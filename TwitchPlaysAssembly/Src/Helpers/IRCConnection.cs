using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
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

	#region UnityLifeCycle
	private void Awake()
	{
		Instance = this;
		_ircConnectionSettings = GetComponent<KMModSettings>();
	}

	private void Start()
	{
		Connect();
	}

	private void Update()
	{
		lock (_messageQueue)
		{
			while (_messageQueue.Count > 0)
			{
				Message message = _messageQueue.Dequeue();
				OnMessageReceived.Invoke(message.UserNickName, message.UserColorCode, message.Text);
			}
		}
	}

	private void OnDisable()
	{
		StopAllCoroutines();
		_onDisableState = State;
		Disconnect();
		_onDisableThread = new Thread(() =>
		{
			while (IRCConnection.Instance.State != IRCConnectionState.Disconnected)
				Thread.Sleep(25);
			IRCConnection.Instance.State = IRCConnectionState.Disabled;
		});
		_onDisableThread.Start();
	}

	private void OnEnable()
	{
		switch (_onDisableState)
		{
			case IRCConnectionState.Retrying:
			case IRCConnectionState.Connecting:
			case IRCConnectionState.Connected:
			case IRCConnectionState.Disabled:
				Connect();
				break;
			default:
				State = IRCConnectionState.Disconnected;
				break;
		}
	}

	private static void AddTextToHoldable(string text, params object[]args)
	{
		IRCConnectionManagerHoldable.IRCTextToDisplay.AddRange(string.Format(text,args).Wrap(60).Split(new[] { "\n" }, StringSplitOptions.None));
	}
	#endregion

	#region Public Methods
	public IEnumerator KeepConnectionAlive()
	{
		AddTextToHoldable("[IRC:Connection] Connecting to IRC");
		Stopwatch stopwatch = new Stopwatch();
		int[] connectionRetryDelay = { 100, 1000, 2000, 5000, 10000, 20000, 30000, 40000, 50000, 60000 };
		while (true)
		{
			int connectionRetryIndex = 0;
			while (State != IRCConnectionState.Connected)
			{
				stopwatch.Start();
				while (stopwatch.ElapsedMilliseconds < connectionRetryDelay[connectionRetryIndex])
				{
					yield return new WaitForSeconds(0.1f);
					if (State == IRCConnectionState.DoNotRetry)
					{
						State = IRCConnectionState.Disconnected;
						AddTextToHoldable("\nCancelled connection retry attempt");
						yield break;
					}
				}
				stopwatch.Reset();

				if (++connectionRetryIndex == connectionRetryDelay.Length) connectionRetryIndex--;
				Thread connectionAttempt = new Thread(ConnectToIRC);
				connectionAttempt.Start();
				while (connectionAttempt.IsAlive) yield return new WaitForSeconds(0.1f);
				switch (State)
				{
					case IRCConnectionState.DoNotRetry:
						State = IRCConnectionState.Disconnected;
						AddTextToHoldable("Connection attempt aborted.");
						yield break;
					case IRCConnectionState.Connected:
						AddTextToHoldable("[IRC:Connection] Successful");
						break;
					default:
						State = IRCConnectionState.Retrying;
						AddTextToHoldable($"[IRC:Connection Failed] - Retrying in {connectionRetryDelay[connectionRetryIndex] / 1000} seconds");
						break;
				}
			}
			while (State == IRCConnectionState.Connected) yield return new WaitForSeconds(0.1f);
			switch (State)
			{
				case IRCConnectionState.DoNotRetry:
					State = IRCConnectionState.Disconnected;
					yield break;
				case IRCConnectionState.Disconnecting:
					UnityEngine.Debug.Log("[IRC:Disconnect] Disconnecting from chat IRC.");
					AddTextToHoldable("[IRC:Disconnect] Disconnecting from chat IRC.");
					yield break;
				default:
					AddTextToHoldable("[IRC:Disconnected] - Retrying to reconnect");
					break;
			}
		}
	}

	private bool IsUsernameValid(string username)
	{
		return !string.IsNullOrEmpty(username) && Regex.IsMatch(username, "^(#)?[a-z0-9][a-z0-9_]{2,24}$");
	}

	private bool IsAuthTokenValid(string authtoken)
	{
		return !string.IsNullOrEmpty(authtoken) && Regex.IsMatch(authtoken, "^oauth:[a-z0-9]{30}$");
	}

	public void Connect()
	{
		if (!gameObject.activeInHierarchy) return;
		if (!File.Exists(_ircConnectionSettings.SettingsPath))
		{
			AddTextToHoldable("The settings file does not exist. Trying to create it now.");
			DebugHelper.LogError("The settings file does not exist. Trying to create it now.");
			try
			{
				File.WriteAllText(_ircConnectionSettings.SettingsPath, JsonConvert.SerializeObject(new TwitchPlaysService.ModSettingsJSON(), Formatting.Indented));
				AddTextToHoldable("Settings file successfully created. Configure it now.");
			}
			catch (Exception ex)
			{
				AddTextToHoldable("Could not create the settings file due to Exception:\n{0}\nLook at output_log.txt for stack trace", ex.Message);
				DebugHelper.LogException(ex, "Settings file did not exist and could not be created:");
			}
			return;
		}
		try
		{
			_settings = JsonConvert.DeserializeObject<TwitchPlaysService.ModSettingsJSON>(File.ReadAllText(_ircConnectionSettings.SettingsPath));
			if (_settings == null)
			{
				AddTextToHoldable("Failed to read connection settings from mod settings.");
				DebugHelper.LogError("Failed to read connection settings from mod settings.");
				return;
			}

			_settings.authToken = _settings.authToken.ToLowerInvariant();
			_settings.channelName = _settings.channelName.Replace("#","").ToLowerInvariant();
			_settings.userName = _settings.userName.Replace("#","").ToLowerInvariant();
			_settings.serverName = _settings.serverName.ToLowerInvariant();

			if (!IsAuthTokenValid(_settings.authToken) || !IsUsernameValid(_settings.channelName) || !IsUsernameValid(_settings.userName) || string.IsNullOrEmpty(_settings.serverName) || _settings.serverPort < 1 || _settings.serverPort > 65535)
			{
				AddTextToHoldable("Your settings file is not configured correctly.\nThe following items need to be configured:\n");
				if (!IsAuthTokenValid(_settings.authToken))
				{
					AddTextToHoldable("AuthToken - Be sure oauth: is included.\n-   Retrieve from https://twitchapps.com/tmi/");
				}
				if (!IsUsernameValid(_settings.userName))
				{
					AddTextToHoldable("userName");
				}
				if (!IsUsernameValid(_settings.channelName))
				{
					AddTextToHoldable("channelName");
				}
				if (string.IsNullOrEmpty(_settings.serverName))
				{
					AddTextToHoldable("serverName - Most likely to be irc.twitch.tv");
				}
				if (_settings.serverPort < 1 || _settings.serverPort > 65535)
				{
					AddTextToHoldable("serverPort - Most likely to be 6667");
				}
				return;
			}
		}
		catch (Exception ex)
		{
			AddTextToHoldable("Failed to read connection settings from mod settings due to an Exception:\n{0}\nLook at output_log.txt for stack trace", ex.Message);
			DebugHelper.LogException(ex, "Failed to read connection settings from mod settings due to an exception:");
			return;
		}

		_keepConnnectionAlive = KeepConnectionAlive();
		StartCoroutine(_keepConnnectionAlive);
	}

	private void ConnectToIRC()
	{
		State = IRCConnectionState.Connecting;
	    try
	    {
		    UnityEngine.Debug.LogFormat("[IRC:Connect] Starting connection to chat IRC {0}:{1}...", _settings.serverName, _settings.serverPort);
		    AddTextToHoldable("[IRC:Connect] Starting connection to chat IRC {0}:{1}...", _settings.serverName, _settings.serverPort);

			TcpClient sock = new TcpClient();
		    sock.Connect(_settings.serverName, _settings.serverPort);
		    if (!sock.Connected)
		    {
			    UnityEngine.Debug.LogErrorFormat("[IRC:Connect] Failed to connect to chat IRC {0}:{1}.", _settings.serverName, _settings.serverPort);
			    AddTextToHoldable("[IRC:Connect] Failed to connect to chat IRC {0}:{1}.", _settings.serverName, _settings.serverPort);
				return;
		    }

		    UnityEngine.Debug.Log("[IRC:Connect] Connection to chat IRC successful.");
		    AddTextToHoldable("[IRC:Connect] Connection to chat IRC successful.");

			NetworkStream networkStream = sock.GetStream();
		    StreamReader inputStream = new StreamReader(networkStream);
		    StreamWriter outputStream = new StreamWriter(networkStream);

		    if (State == IRCConnectionState.DoNotRetry)
		    {
			    networkStream.Close();
			    sock.Close();
			    return;
		    }

		    State = IRCConnectionState.Connected;

		    _inputThread = new Thread(() => InputThreadMethod(inputStream, networkStream));
		    _inputThread.Start();

		    _outputThread = new Thread(() => OutputThreadMethod(outputStream));
		    _outputThread.Start();

		    SendCommand(string.Format("PASS {0}{1}NICK {2}{1}CAP REQ :twitch.tv/tags{1}CAP REQ :twitch.tv/commands", _settings.authToken, Environment.NewLine, _settings.userName));
			AddTextToHoldable("PASS oauth:*****REDACTED******\nNICK {0}\nCAP REQ :twitch.tv/tags\nCAP REQ :twitch.tv/commands", _settings.userName);
	    }
	    catch (SocketException ex)
	    {
			
		    string connectionFailure = string.Format("[IRC:Connect] Failed to connect to chat IRC {0}:{1}. Due to the following Socket Exception: {2} - {3}", _settings.serverName, _settings.serverPort, ex.SocketErrorCode, ex.Message);
		    UnityEngine.Debug.LogErrorFormat(connectionFailure);
		    AddTextToHoldable(connectionFailure);
		    switch (ex.SocketErrorCode)
		    {
				case SocketError.ConnectionRefused:
				case SocketError.AccessDenied:
					State = IRCConnectionState.DoNotRetry;
					break;
				default:
					if(State != IRCConnectionState.DoNotRetry)
						State = IRCConnectionState.Disconnected;
					break;
			}
	    }
	    catch (Exception ex)
	    {
		    State = IRCConnectionState.DoNotRetry;
		    UnityEngine.Debug.LogErrorFormat("[IRC:Connect] Failed to connect to chat IRC {0}:{1}. Due to the following Exception:", _settings.serverName, _settings.serverPort);
		    AddTextToHoldable("[IRC:Connect] Failed to connect to chat IRC {0}:{1}. Due to the following Exception:\n{2}\nSee output_log.txt for Stack trace", _settings.serverName, _settings.serverPort, ex.Message);
			DebugHelper.LogException(ex);
	    }
    }

    public void Disconnect()
    {
	    // ReSharper disable once SwitchStatementMissingSomeCases
	    switch (State)
	    {
			case IRCConnectionState.Connecting:
			case IRCConnectionState.Retrying:
				State = IRCConnectionState.DoNotRetry;
				break;
			case IRCConnectionState.Connected:
				ColorOnDisconnect = TwitchPlaySettings.data.TwitchBotColorOnQuit;
				State = IRCConnectionState.Disconnecting;
				break;
			case IRCConnectionState.Disabled:
				AddTextToHoldable("Twitch plays is currently disabled");
				break;
			default:
				State = IRCConnectionState.Disconnected;
				break;

		}
    }

    public new void SendMessage(string message)
    {
        if (_silenceMode || State == IRCConnectionState.Disconnected) return;
        foreach (string line in message.Wrap(MaxMessageLength).Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries))
        {
            SendCommand(string.Format("PRIVMSG #{0} :{1}", _settings.channelName, line));
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
            SendCommand(string.Format("PRIVMSG #{0} :Silence mode on", _settings.channelName));
        }
        _silenceMode = !_silenceMode;
        if (!_silenceMode)
        {
            SendCommand(string.Format("PRIVMSG #{0} :Silence mode off", _settings.channelName));
        }
    }

	public Color GetUserColor(string userNickName)
	{
		lock(_userColors)
		{
			return _userColors.TryGetValue(userNickName, out Color color) ? color : Color.black;
		}
	}
    #endregion

    #region Private Methods
	private void SetOwnColor()
	{
		if (!ColorUtility.TryParseHtmlString(_currentColor, out Color color)) return;
		lock (_userColors)
		{
			_userColors[_settings.userName] = color;
		}
	}

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


	    if (!ColorUtility.TryParseHtmlString(userColorCode, out Color color)) return;
	    lock (_userColors)
	    {
		    _userColors[userNickName] = color;
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
				    AddTextToHoldable("[IRC:Disconnect] Connection timed out.");
					stopwatch.Reset();
				    State = IRCConnectionState.Disconnected;
				    continue;
			    }

			    if (!networkStream.DataAvailable)
			    {
				    Thread.Sleep(25);
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
		    AddTextToHoldable("[IRC:Disconnect] Connection failed.");
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
		        Thread.Sleep(25);
	        }
	        catch
	        {
		        UnityEngine.Debug.Log("[IRC:Disconnect] Connection failed.");
		        AddTextToHoldable("[IRC:Disconnect] Connection failed.");
				State = IRCConnectionState.Disconnected;
	        }
        }

	    if (State == IRCConnectionState.Disconnecting)
	    {
		    Commands setColor = new Commands(string.Format("PRIVMSG #{0} :.color {1}", _settings.channelName, ColorOnDisconnect));
		    if (setColor.CommandIsColor())
		    {
			    UnityEngine.Debug.LogFormat("[IRC:Disconnect] Color {0} was requested, setting it now.", ColorOnDisconnect);
			    AddTextToHoldable("[IRC:Disconnect] Color {0} was requested, setting it now.", ColorOnDisconnect);
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
		    AddTextToHoldable("[IRC:Disconnect] Disconnected from chat IRC.");
		}
	    lock (_commandQueue)
	    {
		    _commandQueue.Clear();
	    }
	}

    private void SetDelay(string badges, string nickname, string channel)
    {
        if (channel.Equals(_settings.channelName, StringComparison.InvariantCultureIgnoreCase) &&
            nickname.Equals(_settings.userName, StringComparison.InvariantCultureIgnoreCase))
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
	            AddTextToHoldable($"<{groups[2].Value}>: {groups[5].Value}");
				connection.ReceiveMessage(groups[2].Value, groups[1].Value, groups[5].Value);
            }
            else
            {
	            AddTextToHoldable($"<{groups[3].Value}>: {groups[5].Value}");
				connection.ReceiveMessage(groups[3].Value, groups[1].Value, groups[5].Value);
			}
        }),

        new ActionMap(@"badges=([^;]+)?;color=(#[0-9A-F]{6})?;display-name=([^;]+)?;emote-sets=\S+ :\S+ USERSTATE #(.+)", delegate(IRCConnection connection, GroupCollection groups)
        {
            connection._currentColor = string.IsNullOrEmpty(groups[2].Value) ? string.Empty : groups[2].Value;
            connection.SetDelay(groups[1].Value, groups[3].Value, groups[4].Value);
	        connection.SetOwnColor();
		}, false), 

        new ActionMap(@":(\S+)!\S+ PRIVMSG #(\S+) :(.+)", delegate(IRCConnection connection, GroupCollection groups)
        {
	        AddTextToHoldable($"<{groups[1].Value}>: {groups[3].Value}");
			connection.ReceiveMessage(groups[1].Value, null, groups[3].Value);
        }),

        new ActionMap(@"PING (.+)", delegate(IRCConnection connection, GroupCollection groups)
        {
	        AddTextToHoldable($"---PING--- ---PONG--- {groups[1].Value}");
			connection.SendCommand(string.Format("PONG {0}", groups[1].Value));
        }),

        new ActionMap(@"\S* 001.*", delegate(IRCConnection connection, GroupCollection groups)
        {
	        AddTextToHoldable(groups[0].Value);
            connection.SendCommand(string.Format("JOIN #{0}", connection._settings.channelName));
	        UserAccess.AddUser(connection._settings.userName, AccessLevel.Streamer | AccessLevel.SuperUser | AccessLevel.Admin | AccessLevel.Mod);
	        UserAccess.AddUser(connection._settings.channelName.Replace("#",""), AccessLevel.Streamer | AccessLevel.SuperUser | AccessLevel.Admin | AccessLevel.Mod);
	        UserAccess.WriteAccessList();
		}),

		new ActionMap(@"\S* NOTICE \* :Login authentication failed", delegate(IRCConnection connection, GroupCollection groups)
		{
			AddTextToHoldable(groups[0].Value);
			connection.State = IRCConnectionState.DoNotRetry;
		}),

		new ActionMap(@"\S* RECONNECT.*", delegate(IRCConnection connection, GroupCollection groups)
		{
			AddTextToHoldable(groups[0].Value);
			connection.State = IRCConnectionState.Disconnected;
		}),

        new ActionMap(@".+", delegate(IRCConnection connection, GroupCollection groups)
        {
	        AddTextToHoldable(groups[0].Value);
		})  //Log otherwise uncaptured lines.
    };
    #endregion

    #region Public Fields
    public readonly MessageEvent OnMessageReceived = new MessageEvent();
    public string ColorOnDisconnect = null;
	public static IRCConnection Instance { get; private set; }
	public IRCConnectionState State { get; private set; } = IRCConnectionState.Disconnected;
	#endregion

	#region Private Fields
	private KMModSettings _ircConnectionSettings;
	private TwitchPlaysService.ModSettingsJSON _settings;
    private const int MessageDelayUser = 2000;
    private const int MessageDelayMod = 500;
    private const int MaxMessageLength = 480;

    private Thread _inputThread = null;
    private Thread _outputThread = null;
	private Thread _onDisableThread = null;
    private bool _isModerator = false;
    private int _messageDelay = 2000;
    private bool _silenceMode = false;
    
    private string _currentColor = string.Empty;

    private Queue<Message> _messageQueue = new Queue<Message>();
    private Queue<Commands> _commandQueue = new Queue<Commands>();
	private Dictionary<string, Color> _userColors = new Dictionary<string, Color>();
	private IRCConnectionState _onDisableState = IRCConnectionState.Connected;
	private IEnumerator _keepConnnectionAlive;
	#endregion
}

public enum IRCConnectionState
{
	DoNotRetry,
	Disconnected,
	Disconnecting,
	Retrying,
	Connecting,
	Connected,
	Disabled
}
