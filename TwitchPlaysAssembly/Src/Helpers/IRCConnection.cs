using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

public class IRCConnection : MonoBehaviour
{
	#region Nested Types
    public class MessageEvent : UnityEvent<string, string, string>
    {
    }

    private class Message
    {
        public Message(string userNickName, string userColorCode, string text)
        {
            UserNickName = userNickName;
            UserColorCode = userColorCode;
            Text = text;
        }

        public readonly string UserNickName;
        public readonly string UserColorCode;
        public readonly string Text;
    }

    private class Commands
    {
        public Commands(string command)
        {
            Command = command;
        }

        public string GetColor()
        {
            Match match = Regex.Match(Command, "[./]color ((>?#[0-9A-F]{6})|Blue|BlueViolet|CadetBlue|Chocolate|Coral|DodgerBlue|Firebrick|GoldenRod|Green|HotPink|OrangeRed|Red|SeaGreen|SpringGreen|YellowGreen)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string color = match.Groups[1].Value.ToLowerInvariant();
                return UserColors.TryGetValue(color, out string hexcolor) ? hexcolor : color;
            }
            return null;
        }

        public bool CommandIsColor()
        {
            return GetColor() != null;
        }

        private static Dictionary<string, string> UserColors = new Dictionary<string, string>
        {
            { "Blue".ToLowerInvariant(),      "#0000FF" },
            {"BlueViolet".ToLowerInvariant(), "#8A2BE2" },
            {"CadetBlue".ToLowerInvariant(),  "#5F9EA0" },
            {"Chocolate".ToLowerInvariant(),  "#D2691E" },
            {"Coral".ToLowerInvariant(),      "#FF7F50" },
            {"DodgerBlue".ToLowerInvariant(), "#1E90FF" },
            {"Firebrick".ToLowerInvariant(),  "#B22222" },
            {"GoldenRod".ToLowerInvariant(),  "#DAA520" },
            {"Green".ToLowerInvariant(),      "#008000" },
            {"HotPink".ToLowerInvariant(),    "#FF69B4" },
            {"OrangeRed".ToLowerInvariant(),  "#FF4500" },
            {"Red".ToLowerInvariant(),        "#FF0000" },
            {"SeaGreen".ToLowerInvariant(),   "#2E8B57" },
            {"SpringGreen".ToLowerInvariant(),"#00FF7F" },
            {"YellowGreen".ToLowerInvariant(),"#9ACD32" }
        };

        public readonly string Command;
    }

    private class ActionMap
    {
        public ActionMap(string regexString, Action<IRCConnection, GroupCollection> action, bool logLine=true)
        {
            _matchingRegex = new Regex(regexString);
            _action = action;
            LogLine = logLine;
        }

        public bool TryMatch(IRCConnection connection, string input)
        {
            Match match = _matchingRegex.Match(input);
            if (match.Success)
            {
                _action(connection, match.Groups);
                if (LogLine)
                {
                    UnityEngine.Debug.LogFormat("[IRC:Read] {0}", input);
                }
                return true;
            }

            return false;
        }

        public readonly bool LogLine;
        private readonly Regex _matchingRegex;
        private readonly Action<IRCConnection, GroupCollection> _action;
    }
    #endregion

    #region Constructor
    public static IRCConnection MakeIRCConnection(string oauth, string nickName, string channelName, string server, int port)
    {
	    GameObject go = new GameObject("IRCConnection");
	    IRCConnection connection = go.AddComponent<IRCConnection>();

	    connection._oauth = oauth;
	    connection._nickName = nickName.ToLower();
	    connection._channelName = channelName;
	    connection._server = server;
	    connection._port = port;
	    Instance = connection;
	    return connection;
    }
	#endregion

	#region UnityLifeCycle
	private void Start()
	{
		StartCoroutine(KeepConnectionAlive());
	}

	private void Update()
	{
		lock (_messageQueue)
		{
			while (_messageQueue.Count > 0)
			{
				Message message = _messageQueue.Dequeue();
				OnMessageReceived.Invoke(message.UserNickName, message.UserColorCode, message.Text);
				IRCConnectionManagerHoldable.ircTextToDisplay.AddRange($"{message.UserNickName}: {message.Text}".Wrap(60).Split(new[] { "\n" }, StringSplitOptions.None));
			}
		}
	}
	#endregion

	#region Public Methods
	public IEnumerator KeepConnectionAlive()
	{
		IRCConnectionManagerHoldable.ircTextToDisplay.Add("[IRC:Connection] Connecting to IRC");
		Stopwatch stopwatch = new Stopwatch();
		int[] connectionRetryDelay = { 100, 1000, 2000, 5000, 10000, 20000, 30000, 40000, 50000, 60000 };
		while (true)
		{
			int connectionRetryIndex = 0;
			while (State != IRCConnectionState.Connected)
			{
				stopwatch.Start();
				while (stopwatch.ElapsedMilliseconds < connectionRetryDelay[connectionRetryIndex]) yield return new WaitForSeconds(0.1f);
				stopwatch.Reset();

				if (++connectionRetryIndex == connectionRetryDelay.Length) connectionRetryIndex--;
				bool result = ConnectToIRC();
				if (!result)
				{
					State = IRCConnectionState.Retrying;
					IRCConnectionManagerHoldable.ircTextToDisplay.Add($"[IRC:Connection Failed] - Retrying in {connectionRetryDelay[connectionRetryIndex] / 1000} seconds");
				}
				else
				{
					IRCConnectionManagerHoldable.ircTextToDisplay.Add("[IRC:Connection] Successful");
				}
			}
			while (State == IRCConnectionState.Connected) yield return new WaitForSeconds(0.1f);
			IRCConnectionManagerHoldable.ircTextToDisplay.Add("[IRC:Disconnected] - Retrying to reconnect");
		}
	}

	public void Connect()
	{
		StartCoroutine(KeepConnectionAlive());
	}

	private bool ConnectToIRC()
	{
		State = IRCConnectionState.Connecting;
	    try
	    {
		    UnityEngine.Debug.LogFormat("[IRC:Connect] Starting connection to chat IRC {0}:{1}...", _server, _port);
		    IRCConnectionManagerHoldable.ircTextToDisplay.Add(string.Format("[IRC:Connect] Starting connection to chat IRC {0}:{1}...", _server, _port));

			TcpClient sock = new TcpClient();
		    sock.Connect(_server, _port);
		    if (!sock.Connected)
		    {
			    UnityEngine.Debug.LogErrorFormat("[IRC:Connect] Failed to connect to chat IRC {0}:{1}.", _server, _port);
			    IRCConnectionManagerHoldable.ircTextToDisplay.Add(string.Format("[IRC:Connect] Failed to connect to chat IRC {0}:{1}.", _server, _port));
				return false;
		    }

		    UnityEngine.Debug.Log("[IRC:Connect] Connection to chat IRC successful.");
		    IRCConnectionManagerHoldable.ircTextToDisplay.Add("[IRC:Connect] Connection to chat IRC successful.");

			NetworkStream networkStream = sock.GetStream();
		    StreamReader inputStream = new StreamReader(networkStream);
		    StreamWriter outputStream = new StreamWriter(networkStream);

		    State = IRCConnectionState.Connected;

		    _inputThread = new Thread(() => InputThreadMethod(inputStream, networkStream));
		    _inputThread.Start();

		    _outputThread = new Thread(() => OutputThreadMethod(outputStream));
		    _outputThread.Start();

		    SendCommand(string.Format("PASS {0}{1}NICK {2}{1}CAP REQ :twitch.tv/tags{1}CAP REQ :twitch.tv/commands", _oauth, Environment.NewLine, _nickName));

		    return true;
	    }
	    catch (SocketException ex)
	    {
		    State = IRCConnectionState.Disconnected;
		    string connectionFailure = string.Format("[IRC:Connect] Failed to connect to chat IRC {0}:{1}. Due to the following Socket Exception: {2} - {3}", _server, _port, ex.SocketErrorCode, ex.Message);
		    UnityEngine.Debug.LogErrorFormat(connectionFailure);
			IRCConnectionManagerHoldable.ircTextToDisplay.AddRange(connectionFailure.Wrap(60).Split(new []{"\n"},StringSplitOptions.None));
		    return false;
	    }
	    catch (Exception ex)
	    {
		    State = IRCConnectionState.Disconnected;
		    UnityEngine.Debug.LogErrorFormat("[IRC:Connect] Failed to connect to chat IRC {0}:{1}. Due to the following Exception:", _server, _port);
			DebugHelper.LogException(ex);
		    return false;
	    }
    }

    public void Disconnect()
    {
	    DebugHelper.Log("[IRC] Stopping All coroutines");
	    StopAllCoroutines();
	    DebugHelper.Log($"[IRC] State = {State.ToString()}");
		if (State != IRCConnectionState.Connected) return;
	    DebugHelper.Log("[IRC] Setting IRC disconnect color");
		ColorOnDisconnect = TwitchPlaySettings.data.TwitchBotColorOnQuit;
	    DebugHelper.Log("[IRC] Setting the Disconnecting state");
		State = IRCConnectionState.Disconnecting;
	    DebugHelper.Log("[IRC] Output final disconnect message to log");
		UnityEngine.Debug.Log("[IRC:Disconnect] Disconnecting from chat IRC.");
	    IRCConnectionManagerHoldable.ircTextToDisplay.Add("[IRC:Disconnect] Disconnecting from chat IRC.");
	}

    public new void SendMessage(string message)
    {
        if (_silenceMode || State == IRCConnectionState.Disconnected) return;
        foreach (string line in message.Wrap(MaxMessageLength).Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries))
        {
            SendCommand(string.Format("PRIVMSG #{0} :{1}", _channelName, line));
        }
    }

    public void SendMessage(string message, params object[] args)
    {
        SendMessage(string.Format(message, args));
    }

    public void ToggleSilenceMode()
    {
        if (!_silenceMode)
        {
            SendCommand(string.Format("PRIVMSG #{0} :Silence mode on", _channelName));
        }
        _silenceMode = !_silenceMode;
        if (!_silenceMode)
        {
            SendCommand(string.Format("PRIVMSG #{0} :Silence mode off", _channelName));
        }
    }
    #endregion

    #region Private Methods
    private void SendCommand(string command)
    {
        lock (_commandQueue)
        {
            _commandQueue.Enqueue(new Commands(command));
        }
    }

    private void ReceiveMessage(string userNickName, string userColorCode, string text)
    {
        lock (_messageQueue)
        {
            _messageQueue.Enqueue(new Message(userNickName, userColorCode, text));
        }
    }

    private void InputThreadMethod(TextReader input, NetworkStream networkStream)
    {
	    Stopwatch stopwatch = new Stopwatch();
	    try
	    {
		    stopwatch.Start();
		    while (State == IRCConnectionState.Connected || State == IRCConnectionState.Disconnecting)
		    {
			    if (stopwatch.ElapsedMilliseconds > 360000)
			    {
				    UnityEngine.Debug.Log("[IRC:Disconnect] Connection timed out.");
				    IRCConnectionManagerHoldable.ircTextToDisplay.Add("[IRC:Disconnect] Connection timed out.");
					stopwatch.Reset();
				    State = IRCConnectionState.Disconnected;
				    continue;
			    }

			    if (!networkStream.DataAvailable)
			    {
				    continue;
			    }

			    stopwatch.Reset();
			    stopwatch.Start();
			    string buffer = input.ReadLine();
			    foreach (ActionMap action in Actions)
			    {
				    if (action.TryMatch(this, buffer))
				    {
					    break;
				    }
			    }
		    }
	    }
	    catch
	    {
		    UnityEngine.Debug.Log("[IRC:Disconnect] Connection failed.");
		    IRCConnectionManagerHoldable.ircTextToDisplay.Add("[IRC:Disconnect] Connection failed.");
			State = IRCConnectionState.Disconnected;
	    }
    }

    private void OutputThreadMethod(TextWriter output)
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();

        while (State == IRCConnectionState.Connected)
        {
	        try
	        {
		        lock (_commandQueue)
		        {
			        if (_commandQueue.Count > 0)
			        {
				        if (stopWatch.ElapsedMilliseconds > _messageDelay)
				        {
					        Commands command = _commandQueue.Dequeue();
					        if (command.CommandIsColor() && _currentColor.Equals(command.GetColor(), StringComparison.InvariantCultureIgnoreCase))
					        {
						        continue;
					        }

					        output.WriteLine(command.Command);
					        output.Flush();

					        stopWatch.Reset();
					        stopWatch.Start();

					        _messageDelay = _isModerator ? MessageDelayMod : MessageDelayUser;
					        _messageDelay += (command.CommandIsColor() && _isModerator) ? 700 : 0;
				        }
			        }
		        }
	        }
	        catch
	        {
		        UnityEngine.Debug.Log("[IRC:Disconnect] Connection failed.");
		        IRCConnectionManagerHoldable.ircTextToDisplay.Add("[IRC:Disconnect] Connection failed.");
				State = IRCConnectionState.Disconnected;
	        }
        }

	    if (State == IRCConnectionState.Disconnecting)
	    {
		    Commands setColor = new Commands(string.Format("PRIVMSG #{0} :.color {1}", _channelName, ColorOnDisconnect));
		    if (setColor.CommandIsColor())
		    {
			    UnityEngine.Debug.LogFormat("[IRC:Disconnect] Color {0} was requested, setting it now.", ColorOnDisconnect);
			    IRCConnectionManagerHoldable.ircTextToDisplay.Add(string.Format("[IRC:Disconnect] Color {0} was requested, setting it now.", ColorOnDisconnect));
				while (stopWatch.ElapsedMilliseconds < _messageDelay)
			    {
			    }
			    output.WriteLine(setColor.Command);
			    output.Flush();

			    stopWatch.Reset();
			    stopWatch.Start();
			    while (stopWatch.ElapsedMilliseconds < 1200)
			    {
			    }
		    }
		    State = IRCConnectionState.Disconnected;
		    UnityEngine.Debug.Log("[IRC:Disconnect] Disconnected from chat IRC.");
		    IRCConnectionManagerHoldable.ircTextToDisplay.Add("[IRC:Disconnect] Disconnected from chat IRC.");
		}
	    lock (_commandQueue)
	    {
		    _commandQueue.Clear();
	    }
    }

    private void SetDelay(string badges, string nickname, string channel)
    {
        if (channel.Equals(_channelName, StringComparison.InvariantCultureIgnoreCase) &&
            nickname.Equals(_nickName, StringComparison.InvariantCultureIgnoreCase))
        {
            _messageDelay = MessageDelayUser;
            if (string.IsNullOrEmpty(badges))
            {
                return;
            }

            string[] badgeset = badges.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
            foreach (string badge in badgeset)
            {
                if (badge.StartsWith("broadcaster/") ||
                    badge.StartsWith("moderator/") ||
                    badge.StartsWith("admin/") ||
                    badge.StartsWith("global_mod/") ||
                    badge.StartsWith("staff/"))
                {
                    _messageDelay = MessageDelayMod;
                    _isModerator = true;
                    return;
                }
            }
        }
    }
    #endregion

    #region Static Fields/Consts
    private static readonly ActionMap[] Actions =
    {
        new ActionMap(@"color=(#[0-9A-F]{6})?;display-name=([^;]+)?;.+:(\S+)!\S+ PRIVMSG #(\S+) :(.+)", delegate(IRCConnection connection, GroupCollection groups)
        {
            if (!string.IsNullOrEmpty(groups[2].Value))
            {
                connection.ReceiveMessage(groups[2].Value, groups[1].Value, groups[5].Value);
            }
            else
            {
                connection.ReceiveMessage(groups[3].Value, groups[1].Value, groups[5].Value);
            }
        }),

        new ActionMap(@"badges=([^;]+)?;color=(#[0-9A-F]{6})?;display-name=([^;]+)?;emote-sets=\S+ :\S+ USERSTATE #(.+)", delegate(IRCConnection connection, GroupCollection groups)
        {
            connection._currentColor = string.IsNullOrEmpty(groups[2].Value) ? string.Empty : groups[2].Value;
            connection.SetDelay(groups[1].Value, groups[3].Value, groups[4].Value);
            
        }, false), 

        new ActionMap(@":(\S+)!\S+ PRIVMSG #(\S+) :(.+)", delegate(IRCConnection connection, GroupCollection groups)
        {
            connection.ReceiveMessage(groups[1].Value, null, groups[3].Value);
        }),

        new ActionMap(@"PING (.+)", delegate(IRCConnection connection, GroupCollection groups)
        {
            connection.SendCommand(string.Format("PONG {0}", groups[1].Value));
        }),

        new ActionMap(@"\S* 001", delegate(IRCConnection connection, GroupCollection groups)
        {
            connection.SendCommand(string.Format("JOIN #{0}", connection._channelName));
        }),

        new ActionMap(@".+", delegate(IRCConnection connection, GroupCollection groups){})  //Log otherwise uncaptured lines.
    };
    #endregion

    #region Public Fields
    public readonly MessageEvent OnMessageReceived = new MessageEvent();
    public string ColorOnDisconnect = null;
	public static IRCConnection Instance { get; private set; }
	public IRCConnectionState State { get; private set; } = IRCConnectionState.Disconnected;
	#endregion

    #region Private Fields
    private string _oauth = null;
    private string _nickName = null;
    private string _channelName = null;
    private string _server = null;
    private int _port = 0;
    private const int MessageDelayUser = 2000;
    private const int MessageDelayMod = 500;
    private const int MaxMessageLength = 480;

    private Thread _inputThread = null;
    private Thread _outputThread = null;
    private bool _isModerator = false;
    private int _messageDelay = 2000;
    private bool _silenceMode = false;
    
    private string _currentColor = string.Empty;

    private Queue<Message> _messageQueue = new Queue<Message>();
    private Queue<Commands> _commandQueue = new Queue<Commands>();
    #endregion
}

public enum IRCConnectionState
{
	Disconnected,
	Disconnecting,
	Retrying,
	Connecting,
	Connected
}
