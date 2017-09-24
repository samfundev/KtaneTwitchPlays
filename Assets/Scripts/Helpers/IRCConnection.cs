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

    private class ActionMap
    {
        public ActionMap(string regexString, Action<IRCConnection, GroupCollection> action)
        {
            _matchingRegex = new Regex(regexString);
            _action = action;
        }

        public bool TryMatch(IRCConnection connection, string input)
        {
            Match match = _matchingRegex.Match(input);
            if (match.Success)
            {
                _action(connection, match.Groups);
                return true;
            }

            return false;
        }

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

        SendCommand(string.Format("PASS {0}{1}NICK {2}{1}CAP REQ :twitch.tv/tags", _oauth, Environment.NewLine, _nickName));

        return true;
    }

    public void Disconnect()
    {
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
        SendCommand(string.Format("PRIVMSG #{0} :{1}", _channelName, message));
    }
    #endregion

    #region Private Methods
    private void SendCommand(string command)
    {
        lock (_commandQueue)
        {
            _commandQueue.Enqueue(command);
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
        while (_keepThreadAlive)
        {
            if (!networkStream.DataAvailable)
            {
                continue;
            }

            string buffer = input.ReadLine();
            UnityEngine.Debug.LogFormat("[IRC:Read] {0}", buffer);

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
                    if (stopWatch.ElapsedMilliseconds > 2000)
                    {
                        output.WriteLine(_commandQueue.Dequeue());
                        output.Flush();

                        stopWatch.Reset();
                        stopWatch.Start();
                    }
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
    };
    #endregion

    #region Public Fields
    public readonly MessageEvent OnMessageReceived = new MessageEvent();
    #endregion

    #region Private Fields
    private readonly string _oauth = null;
    private readonly string _nickName = null;
    private readonly string _channelName = null;
    private readonly string _server = null;
    private readonly int _port = 0;

    private Thread _inputThread = null;
    private Thread _outputThread = null;
    private bool _keepThreadAlive = false;

    private Queue<Message> _messageQueue = new Queue<Message>();
    private Queue<string> _commandQueue = new Queue<string>();
    #endregion
}
