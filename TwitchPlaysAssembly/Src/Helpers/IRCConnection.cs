using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

public class IRCConnection : MonoBehaviour
{
	#region Nested Types
	public class MessageEvent : UnityEvent<string, string, string, bool>
	{
	}

	private class Message
	{
		public Message(string userNickName, string userColorCode, string text, bool internalMessage = false, bool isWhisper = false)
		{
			UserNickName = userNickName;
			UserColorCode = userColorCode;
			Text = text;
			IsWhisper = isWhisper;
			Internal = internalMessage;
		}

		public readonly string UserNickName;
		public readonly string UserColorCode;
		public readonly string Text;
		public readonly bool Internal;
		public readonly bool IsWhisper;
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
			{"Blue".ToLowerInvariant(),       "#0000FF" },
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
		public ActionMap(string regexString, Action<GroupCollection> action, bool logLine = true)
		{
			_matchingRegex = new Regex(regexString);
			_action = action;
			LogLine = logLine;
		}

		public bool TryMatch(string input)
		{
			Match match = _matchingRegex.Match(input);
			if (match.Success)
			{
				_action(match.Groups);
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
		private readonly Action<GroupCollection> _action;
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
				if (!message.Internal)
					OnMessageReceived.Invoke(message.UserNickName, message.UserColorCode, message.Text, message.IsWhisper);
				InternalMessageReceived(message.UserNickName, message.UserColorCode, message.Text);
			}
		}
	}

	private bool _justDisabled = false;
	private void OnDisable()
	{
		StopAllCoroutines();
		if (!_justDisabled)
			_onDisableState = _state;
		_justDisabled = true;
		_state = (_state == IRCConnectionState.Connected) ? IRCConnectionState.Disconnecting : IRCConnectionState.Disconnected;
	}

	private void OnEnable()
	{
		StartCoroutine(CheckDisabledState());
	}

	private IEnumerator CheckDisabledState()
	{
		yield return new WaitForSeconds(0.25f);
		if (!gameObject.activeInHierarchy) yield break;
		_justDisabled = false;
		switch (_onDisableState)
		{
			case IRCConnectionState.Retrying:
			case IRCConnectionState.Connecting:
			case IRCConnectionState.Connected:
			case IRCConnectionState.Disabled:
				Connect();
				break;
			default:
				_state = IRCConnectionState.Disconnected;
				break;
		}
	}

	private static void AddTextToHoldable(Exception ex, string text)
	{
		UnityEngine.Debug.Log(text);
		UnityEngine.Debug.LogException(ex);
		IRCConnectionManagerHoldable.IRCTextToDisplay.AddRange($"{text}\n{ex.Message}\nSee output_log.txt for stack trace".Wrap(60).Split(new[] { "\n" }, StringSplitOptions.None));
	}

	private static void AddTextToHoldable(string text, params object[] args)
	{
		UnityEngine.Debug.LogFormat(text, args);
		IRCConnectionManagerHoldable.IRCTextToDisplay.AddRange(string.Format(text, args).Wrap(60).Split(new[] { "\n" }, StringSplitOptions.None));
	}
	#endregion

	#region Public Methods
	public void SetDebugUsername(bool force = false)
	{
		if (!UserAccess.HasAccess(TwitchPlaySettings.data.TwitchPlaysDebugUsername, AccessLevel.Streamer))
		{
			foreach (string username in UserAccess.GetUsers().Where(kvp => kvp.Key.StartsWith("_")).Select(kvp => kvp.Key).ToArray())
			{
				UserAccess.RemoveUser(username, ~AccessLevel.User);
			}
			UserAccess.AddUser(TwitchPlaySettings.data.TwitchPlaysDebugUsername, AccessLevel.Streamer | AccessLevel.SuperUser | AccessLevel.Admin | AccessLevel.Mod);
			UserAccess.WriteAccessList();
		}

		if (UserNickName.StartsWith("_") || force)
		{
			UserNickName = TwitchPlaySettings.data.TwitchPlaysDebugUsername;
			ChannelName = TwitchPlaySettings.data.TwitchPlaysDebugUsername;
			CurrentColor = "#" + ColorUtility.ToHtmlStringRGB(TwitchPlaySettings.data.TwitchPlaysDebugUsernameColor);
		}
	}

	public IEnumerator KeepConnectionAlive()
	{
		if (!gameObject.activeInHierarchy) yield break;
		AddTextToHoldable("[IRC:Connect] Connecting to IRC");
		Stopwatch stopwatch = new Stopwatch();
		int[] connectionRetryDelay = { 100, 1000, 2000, 5000, 10000, 20000, 30000, 40000, 50000, 60000 };
		while (true)
		{
			int connectionRetryIndex = 0;

			while (_state != IRCConnectionState.Connected)
			{
				stopwatch.Start();
				while (stopwatch.ElapsedMilliseconds < connectionRetryDelay[connectionRetryIndex])
				{
					yield return new WaitForSeconds(0.1f);
					if (_state == IRCConnectionState.DoNotRetry)
					{
						_state = IRCConnectionState.Disconnected;
						AddTextToHoldable("\nCancelled connection retry attempt");
						yield break;
					}
				}
				stopwatch.Reset();

				if (++connectionRetryIndex == connectionRetryDelay.Length) connectionRetryIndex--;
				Thread connectionAttempt = new Thread(ConnectToIRC);
				connectionAttempt.Start();
				while (connectionAttempt.IsAlive) yield return new WaitForSeconds(0.1f);
				switch (_state)
				{
					case IRCConnectionState.DoNotRetry:
						_state = IRCConnectionState.Disconnected;
						AddTextToHoldable("[IRC:Connect] Aborted.");
						yield break;
					case IRCConnectionState.Connected:
						AddTextToHoldable("[IRC:Connect] Successful.");
						break;
					default:
						_state = IRCConnectionState.Retrying;
						AddTextToHoldable($"[IRC:Connect] Failed - Retrying in {connectionRetryDelay[connectionRetryIndex] / 1000} seconds.");
						break;
				}
			}
			while (_state == IRCConnectionState.Connected) yield return new WaitForSeconds(0.1f);
			if (BombMessageResponder.BombActive) BombMessageResponder.Instance.OnMessageReceived("Bomb Factory", "!disablecamerawall");
			switch (_state)
			{
				case IRCConnectionState.DoNotRetry:
					_state = IRCConnectionState.Disconnected;
					yield break;
				case IRCConnectionState.Disconnecting:
					AddTextToHoldable("[IRC:Disconnect] Disconnecting from chat IRC.");
					yield break;
				default:
					AddTextToHoldable("[IRC:Connect] Trying to reconnect.");
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
		if (!gameObject.activeInHierarchy)
		{
			SetDebugUsername(true);
			return;
		}
		if (!File.Exists(_ircConnectionSettings.SettingsPath))
		{
			AddTextToHoldable("The settings file does not exist. Trying to create it now.");
			SetDebugUsername(true);
			try
			{
				File.WriteAllText(_ircConnectionSettings.SettingsPath, JsonConvert.SerializeObject(new TwitchPlaysService.ModSettingsJSON(), Formatting.Indented));
				AddTextToHoldable("Settings file successfully created. Configure it now. Open up the Mod manager holdable, and select Open mod settins folder.");
			}
			catch (Exception ex)
			{
				AddTextToHoldable(ex, "Settings file did not exist and could not be created:");
			}
			return;
		}
		try
		{
			_settings = JsonConvert.DeserializeObject<TwitchPlaysService.ModSettingsJSON>(File.ReadAllText(_ircConnectionSettings.SettingsPath));
			if (_settings == null)
			{
				SetDebugUsername(true);
				AddTextToHoldable("[IRC:Connect] Failed to read connection settings from mod settings.");
				return;
			}

			UserNickName = _settings.userName.Replace("#", "");
			ChannelName = _settings.channelName.Replace("#", "");
			CurrentColor = new Commands($".color {TwitchPlaySettings.data.TwitchBotColorOnQuit}").GetColor();

			_settings.authToken = _settings.authToken.ToLowerInvariant();
			_settings.channelName = ChannelName.ToLowerInvariant();
			_settings.userName = UserNickName.ToLowerInvariant();
			_settings.serverName = _settings.serverName.ToLowerInvariant();

			if (!IsAuthTokenValid(_settings.authToken) || !IsUsernameValid(_settings.channelName) || !IsUsernameValid(_settings.userName) || string.IsNullOrEmpty(_settings.serverName) || _settings.serverPort < 1 || _settings.serverPort > 65535)
			{
				SetDebugUsername(true);
				AddTextToHoldable("[IRC:Connect] Your settings file is not configured correctly.\nThe following items need to be configured:\n");
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
					AddTextToHoldable("serverPort - Most likely to be 6697");
				}
				AddTextToHoldable("\nOpen up the Mod manager holdable, and select \"open mod settings folder\".");
				return;
			}
		}
		catch (Exception ex)
		{
			SetDebugUsername(true);
			AddTextToHoldable(ex, "[IRC:Connect] Failed to read connection settings from mod settings due to an exception:");
			return;
		}

		_keepConnnectionAlive = KeepConnectionAlive();
		StartCoroutine(_keepConnnectionAlive);
	}

	private void ConnectToIRC()
	{
		if (State == IRCConnectionState.Disabled)
		{
			SetDebugUsername(true);
			return;
		}
		_state = IRCConnectionState.Connecting;
		try
		{
			AddTextToHoldable("[IRC:Connect] Starting connection to chat IRC {0}:{1}...", _settings.serverName, _settings.serverPort);

			TcpClient sock = new TcpClient();
			sock.Connect(_settings.serverName, _settings.serverPort);
			if (!sock.Connected)
			{
				SetDebugUsername(true);
				AddTextToHoldable("[IRC:Connect] Failed to connect to chat IRC {0}:{1}.", _settings.serverName, _settings.serverPort);
				return;
			}

			AddTextToHoldable("[IRC:Connect] Connection to chat IRC successful.");

			NetworkStream networkStream = sock.GetStream();
			try
			{
				AddTextToHoldable("[IRC:Connect] Attempting to set up SSL connection.");
				SslStream sslStream = new SslStream(networkStream, true, new RemoteCertificateValidationCallback(VerifyServerCertificate));
				sslStream.AuthenticateAsClient(_settings.serverName);

				DebugHelper.Log($"SSL encrypted: {sslStream.IsEncrypted}.");
				DebugHelper.Log($"SSL authenticated: {sslStream.IsAuthenticated}.");
				DebugHelper.Log($"SSL signed: {sslStream.IsSigned}.");
				DebugHelper.Log($"SSL mutually authenticated: {sslStream.IsMutuallyAuthenticated}.");

				StreamReader inputStream = new StreamReader(sslStream);
				StreamWriter outputStream = new StreamWriter(sslStream);

				if (_state == IRCConnectionState.DoNotRetry)
				{
					SetDebugUsername(true);
					sslStream.Close();
					sock.Close();
					return;
				}

				_inputThread = new Thread(() => InputThreadMethod(inputStream, networkStream));
				_inputThread.Start();

				_outputThread = new Thread(() => OutputThreadMethod(outputStream));
				_outputThread.Start();

				AddTextToHoldable("[IRC:Connect] SSL setup completed with no errors.");
			}
			catch (Exception ex)
			{
				AddTextToHoldable("[IRC:Connect] SSL connection failed, defaulting to insecure connection.");
				if (_settings.serverPort == 6667)
					AddTextToHoldable("[IRC:Connect] The configured port does not use SSL, please change it to 6697 if you wish to use SSL.");
				DebugHelper.LogException(ex, "An Exception has occured when attempting to connect using SSL, using insecure stream instead:");
				_settings.serverPort = 6667;
				sock = new TcpClient(_settings.serverName, _settings.serverPort);
				networkStream = sock.GetStream();
				StreamReader inputStream = new StreamReader(networkStream);
				StreamWriter outputStream = new StreamWriter(networkStream);

				if (_state == IRCConnectionState.DoNotRetry)
				{
					SetDebugUsername(true);
					networkStream.Close();
					sock.Close();
					return;
				}

				_inputThread = new Thread(() => InputThreadMethod(inputStream, networkStream));
				_inputThread.Start();

				_outputThread = new Thread(() => OutputThreadMethod(outputStream));
				_outputThread.Start();
			}

			SendCommand(string.Format("PASS {0}{1}NICK {2}{1}CAP REQ :twitch.tv/tags{1}CAP REQ :twitch.tv/commands{1}CAP REQ :twitch.tv/membership", _settings.authToken, Environment.NewLine, _settings.userName));
			AddTextToHoldable("PASS oauth:*****REDACTED******\nNICK {0}\nCAP REQ :twitch.tv/tags\nCAP REQ :twitch.tv/commands\nCAP REQ :twitch.tv/membership", _settings.userName);
			while (_state == IRCConnectionState.Connecting)
			{
				Thread.Sleep(25);
			}
		}
		catch (SocketException ex)
		{
			AddTextToHoldable($"[IRC:Connect] Failed to connect to chat IRC {_settings.serverName}:{_settings.serverPort}. Due to the following Socket Exception: {ex.SocketErrorCode} - {ex.Message}");
			switch (ex.SocketErrorCode)
			{
				case SocketError.ConnectionRefused:
				case SocketError.AccessDenied:
					_state = IRCConnectionState.DoNotRetry;
					break;
				default:
					if (_state != IRCConnectionState.DoNotRetry)
						_state = IRCConnectionState.Disconnected;
					break;
			}
		}
		catch (Exception ex)
		{
			_state = IRCConnectionState.DoNotRetry;
			AddTextToHoldable(ex, $"[IRC:Connect] Failed to connect to chat IRC {_settings.serverName}:{_settings.serverPort}. Due to the following Exception:");
		}
	}

	private bool VerifyServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
	{
		X509Certificate2 cert2 = new X509Certificate2(certificate);
		if (cert2.Subject.Contains(_settings.serverName.Substring(4)) && DateTime.UtcNow <= cert2.NotAfter && DateTime.UtcNow >= cert2.NotBefore)
		{
			DebugHelper.Log("SSL certificate valid.");
			return true;
		}

		if (!cert2.Subject.Contains(_settings.serverName.Substring(4)))
			DebugHelper.Log("SSL certificate not issued to the server we are connected to.");
		else if (DateTime.UtcNow > cert2.NotAfter)
			DebugHelper.Log("SSL certificate expired.");
		else if (DateTime.UtcNow < cert2.NotBefore)
			DebugHelper.Log("SSL certificate issued for the future.");

		return false;
	}

	public void Disconnect()
	{
		SetDebugUsername(true);
		if (BombMessageResponder.BombActive) BombMessageResponder.Instance.OnMessageReceived("Bomb Factory", "!disablecamerawall");
		// ReSharper disable once SwitchStatementMissingSomeCases
		switch (_state)
		{
			case IRCConnectionState.Connecting:
			case IRCConnectionState.Retrying:
				_state = IRCConnectionState.DoNotRetry;
				break;
			case IRCConnectionState.Connected:
				ColorOnDisconnect = TwitchPlaySettings.data.TwitchBotColorOnQuit;
				_state = IRCConnectionState.Disconnecting;
				break;
			case IRCConnectionState.Disabled:
				AddTextToHoldable("[IRC:Connect] Twitch Plays is currently disabled.");
				break;
			default:
				_state = IRCConnectionState.Disconnected;
				break;

		}
	}

	[StringFormatMethod("message")]
	public void SendChatMessage(string message)
	{
		foreach (string line in message.Wrap(MaxMessageLength).Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
		{
			if (!_silenceMode && _state != IRCConnectionState.Disconnected)
				SendCommand(string.Format("PRIVMSG #{0} :{1}", _settings.channelName, line));
			if (line.StartsWith(".") || line.StartsWith("/")) continue;
			lock (_messageQueue)
			{
				_messageQueue.Enqueue(new Message(UserNickName, CurrentColor, line, true, false));
			}
		}
	}

	[StringFormatMethod("message")]
	public void SendChatMessage(string message, params object[] args)
	{
		SendMessage(string.Format(message, args));
	}

	[StringFormatMethod("message")]
	public void SendMessage(string message, params object[] args)
	{
		SendMessage(string.Format(message, args));
	}

	[StringFormatMethod("message")]
	public new void SendMessage(string message, object arg)
	{
		SendMessage(string.Format(message, arg));
	}

	[StringFormatMethod("message")]
	public new void SendMessage(string message)
	{
		SendMessage(message, null, true);
	}

	[StringFormatMethod("message")]
	public void SendMessage(string message, string userNickName, bool sendToChat)
	{
		if (sendToChat || string.IsNullOrEmpty(userNickName) || !TwitchPlaySettings.data.EnableWhispers || userNickName == UserNickName) //can't send whispers to yourself
		{
			SendChatMessage(message);
		}
		else
		{
			SendWhisper(userNickName, message);
		}
	}

	[StringFormatMethod("message")]
	public void SendMessage(string message, string userNickName, bool sendToChat, params object[] args)
	{
		SendMessage(string.Format(message, args), userNickName, sendToChat);
	}

	//NOTE: whisper mode is not fully supported, as bots need to be registered with twitch to take advantage of it.
	[StringFormatMethod("message")]
	public void SendWhisper(string userNickName, string message)
	{
		foreach (string line in message.Wrap(MaxMessageLength).Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
		{
			if (!_silenceMode && _state != IRCConnectionState.Disconnected)
				SendCommand(string.Format("PRIVMSG #{0} :.w {1} {2}", _settings.channelName, userNickName, line));
			if (line.StartsWith(".") || line.StartsWith("/")) continue;
			lock (_messageQueue)
			{
				_messageQueue.Enqueue(new Message(UserNickName, CurrentColor, line, true, true));
			}
		}
	}

	[StringFormatMethod("message")]
	public void SendWhisper(string userNickName, string message, params object[] args)
	{
		SendWhisper(userNickName, string.Format(message, args));
	}


	public void ToggleSilenceMode()
	{
		if (!_silenceMode)
		{
			SendCommand(string.Format("PRIVMSG #{0} :Silence mode on.", _settings.channelName));
			lock (_messageQueue)
			{
				_messageQueue.Enqueue(new Message(UserNickName, CurrentColor, "Silence mode on.", true, false));
			}
		}
		_silenceMode = !_silenceMode;
		if (!_silenceMode)
		{
			SendCommand(string.Format("PRIVMSG #{0} :Silence mode off.", _settings.channelName));
			lock (_messageQueue)
			{
				_messageQueue.Enqueue(new Message(UserNickName, CurrentColor, "Silence mode off.", true, false));
			}
		}
	}

	public Color GetUserColor(string userNickName)
	{
		lock (_userColors)
		{
			return _userColors.TryGetValue(userNickName, out Color color) ? color : Color.black;
		}
	}
	#endregion

	#region Private Methods
	private void SetOwnColor()
	{
		if (!ColorUtility.TryParseHtmlString(CurrentColor, out Color color)) return;
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

	private void ReceiveMessage(string userNickName, string userColorCode, string text, bool isWhisper = false)
	{
		if (ColorUtility.TryParseHtmlString(userColorCode, out Color color))
		{
			lock (_userColors)
			{
				_userColors[userNickName] = color;
			}
		}

		if (text.Equals("!enablecommands", StringComparison.InvariantCultureIgnoreCase) && !CommandsEnabled && UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true))
		{
			CommandsEnabled = true;
			Instance.SendMessage("Commands enabled.");
			return;
		}
		if (!CommandsEnabled && !UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true) && (!TwitchPlaySettings.data.AllowSolvingCurrentBombWithCommandsDisabled || !BombMessageResponder.BombActive)) return;
		if (text.Equals("!disablecommands", StringComparison.InvariantCultureIgnoreCase) && UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true))
		{
			CommandsEnabled = false;
			if (TwitchPlaySettings.data.AllowSolvingCurrentBombWithCommandsDisabled && BombMessageResponder.BombActive)
				Instance.SendMessage("Commands will be disabled once this bomb is completed or exploded.");
			else
				Instance.SendMessage("Commands disabled.");
			return;
		}

		if (!isWhisper || TwitchPlaySettings.data.EnableWhispers)
		{
			lock (_messageQueue)
			{
				_messageQueue.Enqueue(new Message(userNickName, userColorCode, text, false, isWhisper));
			}
		}
	}

	private int _connectionTimeout => _state == IRCConnectionState.Connected ? 360000 : 30000;
	private void InputThreadMethod(TextReader input, NetworkStream networkStream)
	{
		Stopwatch stopwatch = new Stopwatch();
		try
		{
			stopwatch.Start();
			while (ThreadAlive)
			{
				if (stopwatch.ElapsedMilliseconds > _connectionTimeout)
				{
					AddTextToHoldable("[IRC:Connect] Connection timed out.");
					stopwatch.Reset();
					_state = IRCConnectionState.Disconnected;
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
					if (action.TryMatch(buffer))
					{
						break;
					}
				}
			}
		}
		catch
		{
			AddTextToHoldable("[IRC:Disconnect] Connection failed.");
			_state = IRCConnectionState.Disconnected;
		}
	}

	private void OutputThreadMethod(TextWriter output)
	{
		Stopwatch stopWatch = new Stopwatch();
		stopWatch.Start();
		_messageDelay = 0;

		while (ThreadAlive)
		{
			try
			{
				Thread.Sleep(25);
				Commands command;
				if (stopWatch.ElapsedMilliseconds <= _messageDelay && _state == IRCConnectionState.Connected) continue;
				lock (_commandQueue)
				{
					if (_commandQueue.Count == 0) continue;
					command = _commandQueue.Dequeue();
				}

				if (command.CommandIsColor() && CurrentColor.Equals(command.GetColor(), StringComparison.InvariantCultureIgnoreCase))
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
			catch
			{
				AddTextToHoldable("[IRC:Disconnect] Connection failed.");
				_state = IRCConnectionState.Disconnected;
			}
		}
		BombMessageResponder.EnableDisableInput();

		try
		{
			if (_state == IRCConnectionState.Disconnecting)
			{
				Commands setColor = new Commands(string.Format("PRIVMSG #{0} :.color {1}", _settings.channelName, ColorOnDisconnect));
				if (setColor.CommandIsColor())
				{
					AddTextToHoldable("[IRC:Disconnect] Color {0} was requested, setting it now.", ColorOnDisconnect);
					while (stopWatch.ElapsedMilliseconds < _messageDelay)
					{
						Thread.Sleep(25);
					}
					output.WriteLine(setColor.Command);
					output.Flush();

					stopWatch.Reset();
					stopWatch.Start();
					while (stopWatch.ElapsedMilliseconds < 1200)
					{
						Thread.Sleep(25);
					}
				}
				_state = IRCConnectionState.Disconnected;
				AddTextToHoldable("[IRC:Disconnect] Disconnected from chat IRC.");
			}
			lock (_commandQueue)
			{
				_commandQueue.Clear();
			}
		}
		catch
		{
			_state = IRCConnectionState.Disconnected;
		}
		if (!gameObject.activeInHierarchy)
			AddTextToHoldable("[IRC:Disconnect] Twitch Plays disabled.");
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

			string[] badgeset = badges.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
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

	protected void InternalMessageReceived(string userNickName, string userColorCode, string text)
	{
		const string actionStart = "\x01" + "ACTION ";
		const string actionEnd = "\x01";
		bool messageIsAction = text.StartsWith(actionStart) && text.EndsWith(actionEnd);
		if (messageIsAction)
		{
			text = text.Substring(actionStart.Length);
			text = text.Remove(text.Length - actionEnd.Length);
		}
		while (text.RegexMatch(out Match match, "(<size=[0-9]+>|<\\/size>)"))
			text = text.Replace(match.Groups[1].Value, "");
		if (text.Trim().Length == 0) return;

		string nickname = messageIsAction ? $"* {userNickName} " : $"{userNickName}: ";
		string messageText = nickname.Replace(" ", "-") + text;
		messageText = messageText.Wrap(64).Substring(nickname.Length);
		if (ColorUtility.TryParseHtmlString(userColorCode, out _))
		{
			nickname = $"<color={userColorCode}FF>{nickname}";
			if (messageIsAction)
				messageText += "</color>";
			else
				nickname += "</color>";
		}
		IRCConnectionManagerHoldable.IRCTextToDisplay.AddRange($"{nickname}{messageText}".Split(new[] { "\n" }, StringSplitOptions.None));
	}
	#endregion

	#region Static Fields/Consts
	private static readonly ActionMap[] Actions =
	{
		new ActionMap(@"color=(#[0-9A-F]{6})?;display-name=([^;]+)?;.+:(\S+)!\S+ PRIVMSG #(\S+) :(.+)", delegate(GroupCollection groups)
		{
			if (!string.IsNullOrEmpty(groups[2].Value))
			{
				Instance.ReceiveMessage(groups[2].Value, groups[1].Value, groups[5].Value);
			}
			else
			{
				Instance.ReceiveMessage(groups[3].Value, groups[1].Value, groups[5].Value);
			}
		}),

		new ActionMap(@"badges=([^;]+)?;color=(#[0-9A-F]{6})?;display-name=([^;]+)?;emote-sets=\S+ :\S+ USERSTATE #(.+)", delegate(GroupCollection groups)
		{
			Instance.CurrentColor = string.IsNullOrEmpty(groups[2].Value) ? string.Empty : groups[2].Value;
			Instance.SetDelay(groups[1].Value, groups[3].Value, groups[4].Value);
			Instance.SetOwnColor();
		}, false),

		new ActionMap(@":(\S+)!\S+ PRIVMSG #(\S+) :(.+)", delegate(GroupCollection groups)
		{
			Instance.ReceiveMessage(groups[1].Value, null, groups[3].Value);
		}),

		new ActionMap(@":(\S+)!\S+ WHISPER (\S+) :(.+)", delegate(GroupCollection groups)
		{
			Instance.ReceiveMessage(groups[1].Value, null, groups[3].Value, true);
		}),

		new ActionMap(@"PING (.+)", delegate(GroupCollection groups)
		{
			AddTextToHoldable($"---PING--- ---PONG--- {groups[1].Value}");
			Instance.SendCommand(string.Format("PONG {0}", groups[1].Value));
		}, false),

		new ActionMap(@"\S* 001.*", delegate(GroupCollection groups)
		{
			AddTextToHoldable(groups[0].Value);
			Instance.SendCommand(string.Format("JOIN #{0}", Instance._settings.channelName));
			Instance._state = IRCConnectionState.Connected;
			BombMessageResponder.EnableDisableInput();
			UserAccess.AddUser(Instance._settings.userName, AccessLevel.Streamer | AccessLevel.SuperUser | AccessLevel.Admin | AccessLevel.Mod);
			UserAccess.AddUser(Instance._settings.channelName.Replace("#", ""), AccessLevel.Streamer | AccessLevel.SuperUser | AccessLevel.Admin | AccessLevel.Mod);
			UserAccess.WriteAccessList();
		}, false),

		new ActionMap(@"\S* NOTICE \* :Login authentication failed", delegate(GroupCollection groups)
		{
			AddTextToHoldable(groups[0].Value);
			Instance._state = IRCConnectionState.DoNotRetry;
		}, false),

		new ActionMap(@"\S* RECONNECT.*", delegate(GroupCollection groups)
		{
			AddTextToHoldable(groups[0].Value);
			Instance._state = IRCConnectionState.Disconnected;
		}, false),

		new ActionMap(@".+", delegate(GroupCollection groups)
		{
			AddTextToHoldable(groups[0].Value);
		}, false) //Log otherwise uncaptured lines.
	};
	public static bool CommandsEnabled = true;
	#endregion

	#region Public Fields
	public readonly MessageEvent OnMessageReceived = new MessageEvent();
	public string ColorOnDisconnect = null;
	public static IRCConnection Instance { get; private set; }
	public IRCConnectionState State => gameObject.activeInHierarchy ? _state : IRCConnectionState.Disabled;
	public string UserNickName { get; private set; } = null;
	public string ChannelName { get; private set; } = null;
	#endregion

	#region Private Fields
	private IRCConnectionState _state = IRCConnectionState.Disconnected;
	private bool ThreadAlive => _state == IRCConnectionState.Connecting || _state == IRCConnectionState.Connected;
	private KMModSettings _ircConnectionSettings;
	private TwitchPlaysService.ModSettingsJSON _settings;
	private const int MessageDelayUser = 2000;
	private const int MessageDelayMod = 500;
	private const int MaxMessageLength = 480;

	private Thread _inputThread = null;
	private Thread _outputThread = null;
	private bool _isModerator = false;
	private int _messageDelay = 2000;
	private bool _silenceMode = false;

	public string CurrentColor { get; private set; } = string.Empty;

	private Queue<Message> _messageQueue = new Queue<Message>();
	private Queue<Commands> _commandQueue = new Queue<Commands>();
	private Dictionary<string, Color> _userColors = new Dictionary<string, Color>();
	private IRCConnectionState _onDisableState = IRCConnectionState.Disconnected;
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
