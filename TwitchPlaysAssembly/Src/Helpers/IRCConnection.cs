using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine.Events;

public class IRCConnection
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
    public IRCConnection(string oauth, string nickName, string channelName, string server, int port)
    {
        _oauth = oauth;
        _nickName = nickName.ToLower();
        _channelName = channelName;
        _server = server;
        _port = port;
    }
    #endregion

    #region Public Methods
    public bool Connect()
    {
        UnityEngine.Debug.LogFormat("[IRC:Connect] Starting connection to chat IRC {0}:{1}...", _server, _port);

        TcpClient sock = new TcpClient();
        sock.Connect(_server, _port);
        if (!sock.Connected)
        {
            UnityEngine.Debug.LogErrorFormat("[IRC:Connect] Failed to connect to chat IRC {0}:{1}.", _server, _port);
            return false;
        }

        UnityEngine.Debug.Log("[IRC:Connect] Connection to chat IRC successful.");

        NetworkStream networkStream = sock.GetStream();
        StreamReader inputStream = new StreamReader(networkStream);
        StreamWriter outputStream = new StreamWriter(networkStream);

        _keepThreadAlive = true;

        _inputThread = new Thread(() => InputThreadMethod(inputStream, networkStream));
        _inputThread.Start();

        _outputThread = new Thread(() => OutputThreadMethod(outputStream));
        _outputThread.Start();
		
        SendCommand(string.Format("PASS {0}{1}NICK {2}{1}CAP REQ :twitch.tv/tags{1}CAP REQ :twitch.tv/commands", _oauth, Environment.NewLine, _nickName));

        return true;
    }

    public void Disconnect()
    {
        _isDisconnecting = true;
        _keepThreadAlive = false;
        UnityEngine.Debug.Log("[IRC:Disconnect] Disconnecting from chat IRC.");
    }

    public void Update()
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

    public void SendMessage(string message)
    {
        if (_silenceMode) return;
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
        while (_keepThreadAlive || _isDisconnecting)
        {
            if (!networkStream.DataAvailable)
            {
                continue;
            }

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

    private void OutputThreadMethod(TextWriter output)
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();

        while (_keepThreadAlive)
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

        Commands setColor = new Commands(string.Format("PRIVMSG #{0} :.color {1}", _channelName, ColorOnDisconnect));
        if (setColor.CommandIsColor())
        {
            UnityEngine.Debug.LogFormat("[IRC:Disconnect] Color {0} was requested, setting it now.",ColorOnDisconnect);
            while (stopWatch.ElapsedMilliseconds < _messageDelay){}
            output.WriteLine(setColor.Command);
            output.Flush();

            stopWatch.Reset();
            stopWatch.Start();
            while (stopWatch.ElapsedMilliseconds < 1200){}
        }

        _isDisconnecting = false;
        UnityEngine.Debug.Log("[IRC:Disconnect] Disconnected from chat IRC.");
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
    #endregion

    #region Private Fields
    private readonly string _oauth = null;
    private readonly string _nickName = null;
    private readonly string _channelName = null;
    private readonly string _server = null;
    private readonly int _port = 0;
    private const int MessageDelayUser = 2000;
    private const int MessageDelayMod = 500;
    private const int MaxMessageLength = 480;

    private Thread _inputThread = null;
    private Thread _outputThread = null;
    private bool _keepThreadAlive = false;
    private bool _isDisconnecting = false;
    private bool _isModerator = false;
    private int _messageDelay = 2000;
    private bool _silenceMode = false;
    
    private string _currentColor = string.Empty;

    private Queue<Message> _messageQueue = new Queue<Message>();
    private Queue<Commands> _commandQueue = new Queue<Commands>();
    #endregion
}
