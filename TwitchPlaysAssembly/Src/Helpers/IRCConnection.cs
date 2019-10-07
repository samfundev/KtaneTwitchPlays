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
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class IRCMessage
{
	public IRCMessage(string userNickName, string userColorCode, string text, bool isWhisper = false, bool internalMessage = false)
	{
		UserNickName = userNickName;
		UserColorCode = userColorCode;
		Text = text;
		IsWhisper = isWhisper;
		Internal = internalMessage;
	}

	public readonly string UserNickName;
	public readonly string UserColorCode;
	public string Text { get; private set; }
	public readonly bool Internal;
	public readonly bool IsWhisper;

	/// <summary>
	/// Creates a duplicate <see cref="IRCMessage">Message</see> object with the Text changed.
	/// </summary>
	/// <param name="text">The Message's new text.</param>
	/// <returns>Returns a duplicate <see cref="IRCMessage">Message</see> object with the new Text.</returns>
	public IRCMessage Duplicate(string text)
	{
		IRCMessage message = (IRCMessage) MemberwiseClone();
		message.Text = text;
		return message;
	}
}

public class IRCConnection : MonoBehaviour
{
	public TwitchMessage MessagePrefab => _data.MessagePrefab;

	public CanvasGroup HighlightGroup => _data.HighlightGroup;

	public GameObject MessageScrollContents => _data.MessageScrollContents;
	public RectTransform MainWindowTransform => _data.MainWindowTransform;
	public RectTransform HighlightTransform => _data.HighlightTransform;

	public GameObject ConnectionAlert => _data.ConnectionAlert;
	Text alertText;
	Transform alertProgressBar;

	#region Nested Types
	public class MessageEvent : UnityEvent<IRCMessage>
	{
	}

	private class IRCCommand
	{
		public IRCCommand(string command)
		{
			Command = command;
		}

		public string GetColor()
		{
			Match match = Regex.Match(Command, "[./]color ((>?#[0-9A-F]{6})|Blue|BlueViolet|CadetBlue|Chocolate|Coral|DodgerBlue|Firebrick|GoldenRod|Green|HotPink|OrangeRed|Red|SeaGreen|SpringGreen|YellowGreen)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
			if (!match.Success) return null;
			string color = match.Groups[1].Value.ToLowerInvariant();
			return UserColors.TryGetValue(color, out string hexColor) ? hexColor : color;
		}

		public bool CommandIsColor() => GetColor() != null;

		private static readonly Dictionary<string, string> UserColors = new Dictionary<string, string>
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
			if (!match.Success) return false;
			_action(match.Groups);
			if (!LogLine) return true;
			UnityEngine.Debug.LogFormat("[IRC:Read] {0}", input);

			return true;
		}

		// ReSharper disable once MemberCanBePrivate.Local
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
		_data = GetComponent<IRCConnectionData>();

		alertText = ConnectionAlert.transform.Find("Text").GetComponent<Text>();
		alertProgressBar = ConnectionAlert.transform.Find("ProgressBar");

		Connect();
		HighlightGroup.alpha = 0.0f;
	}

	public Dictionary<TwitchMessage, float> ScrollOutStartTime = new Dictionary<TwitchMessage, float>();
	private void Update()
	{
		lock (_receiveQueue)
			while (_receiveQueue.Count > 0)
			{
				IRCMessage message = _receiveQueue.Dequeue();
				if (!message.Internal)
				{
					bool isCommand = message.Text.StartsWith("!");

					TwitchMessage twitchMessage = null;
					CoroutineQueue coroutineQueue = TwitchPlaysService.Instance.CoroutineQueue;
					if (isCommand)
					{
						HighlightGroup.alpha = 1;

						twitchMessage = Instantiate(MessagePrefab, MessageScrollContents.transform, false);
						twitchMessage.SetMessage(string.IsNullOrEmpty(message.UserColorCode)
							? $"<b>{message.UserNickName}</b>: {message.Text}"
							: $"<b><color={message.UserColorCode}>{message.UserNickName}</color></b>: {message.Text}");

						TwitchPlaysService.Instance.CoroutineQueue.AddToQueue(HighlightMessage(twitchMessage));
					}

					coroutineQueue.QueueModified = false;
					try
					{
						OnMessageReceived.Invoke(message);
					}
					catch (Exception exception)
					{
						DebugHelper.LogException(exception, "An exception has occurred while invoking OnMessageRecieved:");
					}

					if (!coroutineQueue.QueueModified && twitchMessage != null) Destroy(twitchMessage.gameObject);
					else if (isCommand)
						TwitchPlaysService.Instance.CoroutineQueue.AddToQueue(HideMessage(twitchMessage));
				}

				InternalMessageReceived(message.UserNickName, message.UserColorCode, message.Text);
			}

		if (ScrollOutStartTime.Count <= 0) return;

		float vertScroll = 0;
		foreach (KeyValuePair<TwitchMessage, float> pair in ScrollOutStartTime.ToArray())
		{
			TwitchMessage twitchMessage = pair.Key;
			if (twitchMessage == null)
			{
				ScrollOutStartTime.Remove(twitchMessage);
				continue;
			}

			float alpha = Mathf.Pow(Mathf.Min((Time.time - pair.Value) / 0.167f, 1), 4);
			if (alpha < 1) vertScroll += alpha * twitchMessage.GetComponent<RectTransform>().rect.height;
			else
			{
				ScrollOutStartTime.Remove(twitchMessage);
				Destroy(twitchMessage.gameObject);
			}
		}

		// ReSharper disable InconsistentlySynchronizedField
		Vector3 localPosition = MessageScrollContents.transform.localPosition;
		MessageScrollContents.transform.localPosition = new Vector3(localPosition.x, vertScroll, localPosition.z);
		// ReSharper restore InconsistentlySynchronizedField
	}

	IEnumerator HighlightMessage(TwitchMessage twitchMessage)
	{
		if (twitchMessage == null) yield break; // twitchMessage could be null if the original message never added any coroutines to the queue.

		StartCoroutine(twitchMessage.DoBackgroundColorChange(twitchMessage.HighlightColor));
	}

	IEnumerator HideMessage(TwitchMessage twitchMessage)
	{
		twitchMessage.RemoveMessage();
		yield break;
	}

	private void FixedUpdate() => HighlightGroup.alpha = Mathf.Max(HighlightGroup.alpha - 0.01f, 0);

	private bool _justDisabled;
	private void OnDisable()
	{
		StopAllCoroutines();
		if (!_justDisabled)
			_onDisableState = _state;
		_justDisabled = true;
		_state = _state == IRCConnectionState.Connected ? IRCConnectionState.Disconnecting : IRCConnectionState.Disconnected;
	}

	private void OnEnable() => StartCoroutine(CheckDisabledState());

	private IEnumerator CheckDisabledState()
	{
		yield return new WaitForSeconds(0.25f);
		if (!gameObject.activeInHierarchy) yield break;
		_justDisabled = false;
		// ReSharper disable once SwitchStatementMissingSomeCases
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
	public static void SetDebugUsername(bool force = false)
	{
		if (!UserAccess.HasAccess(TwitchPlaySettings.data.TwitchPlaysDebugUsername, AccessLevel.Streamer))
		{
			foreach (string username in UserAccess.GetUsers().Where(kvp => kvp.Key.StartsWith("_"))
				.Select(kvp => kvp.Key).ToArray())
				UserAccess.RemoveUser(username, ~AccessLevel.User);
			UserAccess.AddUser(TwitchPlaySettings.data.TwitchPlaysDebugUsername, AccessLevel.Streamer | AccessLevel.SuperUser | AccessLevel.Admin | AccessLevel.Mod);
			UserAccess.WriteAccessList();
		}

		if (Instance == null) return;

		if (!Instance.UserNickName.StartsWith("_") && !force) return;
		Instance.UserNickName = TwitchPlaySettings.data.TwitchPlaysDebugUsername;
		Instance.ChannelName = TwitchPlaySettings.data.TwitchPlaysDebugUsername;
		Instance.CurrentColor = "#" + ColorUtility.ToHtmlStringRGB(TwitchPlaySettings.data.TwitchPlaysDebugUsernameColor);
	}

	bool everConnected = false; // Used to prevent the connection alert from flashing quickly when loading the game and it loads successfully.
	public IEnumerator KeepConnectionAlive()
	{
		if (!gameObject.activeInHierarchy) yield break;

		Text alertText = ConnectionAlert.transform.Find("Text").GetComponent<Text>();
		Transform alertProgressBar = ConnectionAlert.transform.Find("ProgressBar");

		AddTextToHoldable("[IRC:Connect] Connecting to IRC");
		Stopwatch stopwatch = new Stopwatch();
		int[] connectionRetryDelay = { 100, 1000, 2000, 5000, 10000, 20000, 30000, 40000, 50000, 60000 };
		while (true)
		{
			int connectionRetryIndex = 0;

			while (_state != IRCConnectionState.Connected)
			{
				ConnectionAlert.SetActive(everConnected);
				everConnected = true;

				stopwatch.Start();
				while (stopwatch.ElapsedMilliseconds < connectionRetryDelay[connectionRetryIndex])
				{
					alertText.text = $"The bot is currently disconnected. Attempting to connect in {((connectionRetryDelay[connectionRetryIndex] - stopwatch.ElapsedMilliseconds) / 1000f).ToString("N1")}";
					alertProgressBar.localScale = new Vector3(1 - stopwatch.ElapsedMilliseconds / (float) connectionRetryDelay[connectionRetryIndex], 1, 1);

					yield return null;
					if (_state != IRCConnectionState.DoNotRetry) continue;
					_state = IRCConnectionState.Disconnected;
					AddTextToHoldable("\nCancelled connection retry attempt");
					ConnectionAlert.SetActive(false);
					yield break;
				}
				stopwatch.Reset();

				alertText.text = "Connecting...";
				alertProgressBar.localScale = new Vector3(0, 1, 1);

				if (++connectionRetryIndex == connectionRetryDelay.Length) connectionRetryIndex--;

				if (State != IRCConnectionState.Disabled)
				{
					Thread connectionAttempt = new Thread(ConnectToIRC);
					connectionAttempt.Start();
					while (connectionAttempt.IsAlive) yield return new WaitForSeconds(0.1f);
				}
				else
				{
					SetDebugUsername(true);
				}

				// ReSharper disable once SwitchStatementMissingSomeCases
				switch (_state)
				{
					case IRCConnectionState.DoNotRetry:
						_state = IRCConnectionState.Disconnected;
						AddTextToHoldable("[IRC:Connect] Aborted.");

						alertText.text = "Unable to connect, aborting retry attempts.";
						yield return new WaitForSeconds(1);
						ConnectionAlert.SetActive(false);

						yield break;
					case IRCConnectionState.Connected:
						AddTextToHoldable("[IRC:Connect] Successful.");
						SendMessage("Welcome to Twitch Plays: Keep Talking and Nobody Explodes!");
						break;
					default:
						_state = IRCConnectionState.Retrying;
						AddTextToHoldable($"[IRC:Connect] Failed - Retrying in {connectionRetryDelay[connectionRetryIndex] / 1000} seconds.");
						break;
				}
			}
			ConnectionAlert.SetActive(false);

			while (_state == IRCConnectionState.Connected) yield return new WaitForSeconds(0.1f);
			if (TwitchGame.BombActive && TwitchGame.ModuleCameras != null)
				TwitchGame.ModuleCameras.DisableCameraWall();
			// ReSharper disable once SwitchStatementMissingSomeCases
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

	private static bool IsUsernameValid(string username) => !string.IsNullOrEmpty(username) && Regex.IsMatch(username, "^(#)?[a-z0-9][a-z0-9_]{2,24}$");

	private static bool IsAuthTokenValid(string authtoken) => !string.IsNullOrEmpty(authtoken) && Regex.IsMatch(authtoken, "^oauth:[a-z0-9]{30}$");

	public static void Connect()
	{
		if (Instance == null) return;
		if (!Instance.gameObject.activeInHierarchy)
		{
			SetDebugUsername(true);
			return;
		}
		if (!File.Exists(Instance._ircConnectionSettings.SettingsPath))
		{
			AddTextToHoldable("The settings file does not exist. Trying to create it now.");
			SetDebugUsername(true);
			try
			{
				File.WriteAllText(Instance._ircConnectionSettings.SettingsPath, JsonConvert.SerializeObject(new TwitchPlaysService.ModSettingsJSON(), Formatting.Indented));
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
			Instance._settings = JsonConvert.DeserializeObject<TwitchPlaysService.ModSettingsJSON>(File.ReadAllText(Instance._ircConnectionSettings.SettingsPath));
			TwitchPlaysService.ModSettingsJSON settings = Instance._settings;

			if (settings == null)
			{
				SetDebugUsername(true);
				AddTextToHoldable("[IRC:Connect] Failed to read connection settings from mod settings.");
				return;
			}

			Instance.UserNickName = Instance._settings.userName.Replace("#", "");
			Instance.ChannelName = Instance._settings.channelName.Replace("#", "");
			Instance.CurrentColor = new IRCCommand($".color {TwitchPlaySettings.data.TwitchBotColorOnQuit}").GetColor();

			settings.authToken = Instance._settings.authToken.ToLowerInvariant();
			settings.channelName = Instance.ChannelName.ToLowerInvariant();
			settings.userName = Instance.UserNickName.ToLowerInvariant();
			settings.serverName = Instance._settings.serverName.ToLowerInvariant();

			if (!IsAuthTokenValid(settings.authToken) || !IsUsernameValid(settings.channelName) || !IsUsernameValid(settings.userName) || string.IsNullOrEmpty(settings.serverName) || settings.serverPort < 1 || settings.serverPort > 65535)
			{
				SetDebugUsername(true);
				AddTextToHoldable("[IRC:Connect] Your settings file is not configured correctly.\nThe following items need to be configured:\n");
				if (!IsAuthTokenValid(settings.authToken))
					AddTextToHoldable(
						"AuthToken - Be sure oauth: is included.\n-   Retrieve from https://twitchapps.com/tmi/");
				if (!IsUsernameValid(settings.userName))
					AddTextToHoldable("userName");
				if (!IsUsernameValid(settings.channelName))
					AddTextToHoldable("channelName");
				if (string.IsNullOrEmpty(settings.serverName))
					AddTextToHoldable("serverName - Most likely to be irc.twitch.tv");
				if (settings.serverPort < 1 || settings.serverPort > 65535)
					AddTextToHoldable("serverPort - Most likely to be 6697");
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

		Instance._keepConnectionAlive = Instance.KeepConnectionAlive();
		Instance.StartCoroutine(Instance._keepConnectionAlive);
	}

	/// <summary>
	/// A NetworkStream that doesn't block if there is no data to read from the network.
	/// This means the "end of the stream" indicates there is no data to read from the network.
	/// </summary>
	class AsyncNetworkStream : NetworkStream
	{
		public AsyncNetworkStream(TcpClient tcpClient) : base(tcpClient.Client, true)
		{
		}

		public override int Read(byte[] buffer, int offset, int count) => !base.DataAvailable ? 0 : base.Read(buffer, offset, count);
	}

	private void ConnectToIRC()
	{
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

			AsyncNetworkStream networkStream = new AsyncNetworkStream(sock);
			try
			{
				AddTextToHoldable("[IRC:Connect] Attempting to set up SSL connection.");
				SslStream sslStream = new SslStream(networkStream, true, VerifyServerCertificate);
				sslStream.AuthenticateAsClient(_settings.serverName);

				DebugHelper.Log($"SSL encrypted: {sslStream.IsEncrypted}, authenticated: {sslStream.IsAuthenticated}, signed: {sslStream.IsSigned} and mutually authenticated: {sslStream.IsMutuallyAuthenticated}.");

				StreamReader inputStream = new StreamReader(sslStream);
				StreamWriter outputStream = new StreamWriter(sslStream);

				if (_state == IRCConnectionState.DoNotRetry)
				{
					SetDebugUsername(true);
					sslStream.Close();
					networkStream.Close();
					sock.Close();
					return;
				}

				_inputThread = new Thread(() => InputThreadMethod(inputStream));
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
				DebugHelper.LogException(ex, "An Exception has occurred when attempting to connect using SSL, using insecure stream instead:");
				_settings.serverPort = 6667;
				sock = new TcpClient(_settings.serverName, _settings.serverPort);
				networkStream = new AsyncNetworkStream(sock);
				StreamReader inputStream = new StreamReader(networkStream);
				StreamWriter outputStream = new StreamWriter(networkStream);

				if (_state == IRCConnectionState.DoNotRetry)
				{
					SetDebugUsername(true);
					networkStream.Close();
					sock.Close();
					return;
				}

				_inputThread = new Thread(() => InputThreadMethod(inputStream));
				_inputThread.Start();

				_outputThread = new Thread(() => OutputThreadMethod(outputStream));
				_outputThread.Start();
			}

			SendCommand(
				$"PASS {_settings.authToken}{Environment.NewLine}NICK {_settings.userName}{Environment.NewLine}CAP REQ :twitch.tv/tags{Environment.NewLine}CAP REQ :twitch.tv/commands{Environment.NewLine}CAP REQ :twitch.tv/membership");
			AddTextToHoldable("PASS oauth:*****REDACTED******\nNICK {0}\nCAP REQ :twitch.tv/tags\nCAP REQ :twitch.tv/commands\nCAP REQ :twitch.tv/membership", _settings.userName);
			while (_state == IRCConnectionState.Connecting)
				Thread.Sleep(25);
		}
		catch (SocketException ex)
		{
			AddTextToHoldable($"[IRC:Connect] Failed to connect to chat IRC {_settings.serverName}:{_settings.serverPort}. Due to the following Socket Exception: {ex.SocketErrorCode} - {ex.Message}");
			// ReSharper disable once SwitchStatementMissingSomeCases
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

	public static void Disconnect()
	{
		if (Instance == null) return;
		SetDebugUsername(true);
		if (TwitchGame.BombActive && TwitchGame.ModuleCameras != null)
			TwitchGame.ModuleCameras.DisableCameraWall();
		// ReSharper disable once SwitchStatementMissingSomeCases
		switch (Instance._state)
		{
			case IRCConnectionState.Connecting:
			case IRCConnectionState.Retrying:
				Instance._state = IRCConnectionState.DoNotRetry;
				break;
			case IRCConnectionState.Connected:
				Instance.ColorOnDisconnect = TwitchPlaySettings.data.TwitchBotColorOnQuit;
				Instance._state = IRCConnectionState.Disconnecting;
				break;
			case IRCConnectionState.Disabled:
				AddTextToHoldable("[IRC:Connect] Twitch Plays is currently disabled.");
				break;
			default:
				Instance._state = IRCConnectionState.Disconnected;
				break;
		}
	}

	//NOTE: whisper mode is not fully supported, as bots need to be registered with twitch to take advantage of it.
	[StringFormatMethod("message")]
	// ReSharper disable once UnusedMember.Global
	public static void SendWhisper(string userNickName, string message, params object[] args) => SendMessage(message, userNickName, false, args);

	public new static void SendMessage(string message) => SendMessage(message, null, true);

	[StringFormatMethod("message")]
	public static void SendMessageFormat(string message, params object[] args) => SendMessage(message, null, true, args);

	[StringFormatMethod("message")]
	public static void SendMessage(string message, string userNickName, bool sendToChat, params object[] args) => SendMessage(string.Format(message, args), userNickName, sendToChat);

	public static void SendMessage(string message, string userNickName, bool sendToChat)
	{
		if (Instance == null) return;

		sendToChat |= !IsUsernameValid(userNickName) || !TwitchPlaySettings.data.EnableWhispers || userNickName == Instance.UserNickName;
		foreach (string line in message.Wrap(MaxMessageLength).Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
		{
			if (!Instance._silenceMode && Instance._state != IRCConnectionState.Disconnected)
				Instance.SendCommand(sendToChat
					? $"PRIVMSG #{Instance._settings.channelName} :{line}"
					: $"PRIVMSG #{Instance._settings.channelName} :.w {userNickName} {line}");
			if (line.StartsWith(".") || line.StartsWith("/")) continue;
			lock (Instance._receiveQueue)
			{
				Instance._receiveQueue.Enqueue(new IRCMessage(Instance.UserNickName, Instance.CurrentColor, line, !sendToChat, true));
			}
		}
	}

	public static void ToggleSilenceMode()
	{
		if (Instance == null) return;
		if (!Instance._silenceMode)
		{
			Instance.SendCommand($"PRIVMSG #{Instance._settings.channelName} :Silence mode on.");
			lock (Instance._receiveQueue)
				Instance._receiveQueue.Enqueue(new IRCMessage(Instance.UserNickName, Instance.CurrentColor,
					"Silence mode on.", false, true));
		}
		Instance._silenceMode = !Instance._silenceMode;
		if (Instance._silenceMode) return;
		Instance.SendCommand($"PRIVMSG #{Instance._settings.channelName} :Silence mode off.");
		lock (Instance._receiveQueue)
			Instance._receiveQueue.Enqueue(new IRCMessage(Instance.UserNickName, Instance.CurrentColor,
				"Silence mode off.", false, true));
	}

	public static Color GetUserColor(string userNickName)
	{
		if (Instance == null) return Color.black;
		lock (Instance._userColors)
			return Instance._userColors.TryGetValue(userNickName, out Color color) ? color : Color.black;
	}
	#endregion

	#region Private Methods
	private void SetOwnColor()
	{
		if (!ColorUtility.TryParseHtmlString(CurrentColor, out Color color)) return;
		lock (_userColors)
			_userColors[_settings.userName] = color;
	}

	private void SendCommand(string command)
	{
		lock (_sendQueue)
			_sendQueue.Enqueue(new IRCCommand(command));
	}

	public static void ReceiveMessage(string userNickName, string userColorCode, string text, bool isWhisper = false, bool silent = false)
	{
		text = text.Replace("…", "...");
		if (isWhisper && !text.StartsWith("!") && !TwitchPlaySettings.data.WhisperCommandsRequireExclaimationPoint)
			text = $"!{text}";
		ReceiveMessage(new IRCMessage(userNickName, userColorCode, text, isWhisper), silent);
	}

	public static void ReceiveMessage(IRCMessage msg, bool silent = false)
	{
		if (Instance == null) return;
		if (msg.UserColorCode != null && ColorUtility.TryParseHtmlString(msg.UserColorCode, out Color color))
		{
			lock (Instance._userColors)
				Instance._userColors[msg.UserNickName] = color;
		}

		if (!silent) DebugHelper.Log($"[M] {msg.UserNickName} ({msg.UserColorCode}): {msg.Text}");

		Leaderboard.Instance.GetRank(msg.UserNickName, out Leaderboard.LeaderboardEntry entry);
		entry.SetActivity(true);

		if (msg.Text.Equals("!enablecommands", StringComparison.InvariantCultureIgnoreCase) && !CommandsEnabled && UserAccess.HasAccess(msg.UserNickName, AccessLevel.SuperUser, true))
		{
			CommandsEnabled = true;
			SendMessage("Commands enabled.");
			return;
		}
		if (msg.Text.Equals("!enablecommands", StringComparison.InvariantCultureIgnoreCase) && UserAccess.HasAccess(msg.UserNickName, AccessLevel.SuperUser, true))
		{
			SendMessage("Commands are already enabled.");
			return;
		}
		if (!CommandsEnabled && !UserAccess.HasAccess(msg.UserNickName, AccessLevel.SuperUser, true) && (!TwitchPlaySettings.data.AllowSolvingCurrentBombWithCommandsDisabled || !TwitchGame.BombActive)) return;
		if (msg.Text.Equals("!disablecommands", StringComparison.InvariantCultureIgnoreCase) && UserAccess.HasAccess(msg.UserNickName, AccessLevel.SuperUser, true))
		{
			CommandsEnabled = false;
			if (TwitchPlaySettings.data.AllowSolvingCurrentBombWithCommandsDisabled && TwitchGame.BombActive)
				SendMessage("Commands will be disabled once this bomb is completed or exploded.");
			else
				SendMessage("Commands disabled.");
			return;
		}

		if (!msg.IsWhisper || TwitchPlaySettings.data.EnableWhispers)
			lock (Instance._receiveQueue)
				Instance._receiveQueue.Enqueue(msg);
	}

	private int ConnectionTimeout => _state == IRCConnectionState.Connected ? 360000 : 30000;
	private void InputThreadMethod(StreamReader input)
	{
		bool pingTimeoutTest = false; // Keeps track of if we are currently in a ping timeout test.
		Stopwatch stopwatch = new Stopwatch();
		try
		{
			stopwatch.Start();
			while (ThreadAlive)
			{
				if (_state == IRCConnectionState.Connected)
				{
					// If the server hasn't sent any data for 6 minutes then begin a ping timeout test.
					if (stopwatch.ElapsedMilliseconds > 360000 && !pingTimeoutTest)
					{
						pingTimeoutTest = true;
						stopwatch.Reset();
						stopwatch.Start();
						SendCommand("PING");
						MainThreadQueue.Enqueue(() => ConnectionAlert.SetActive(true));
					}
					else if (pingTimeoutTest)
					{
						alertText.text = $"The bot might be disconnected from the server. Timing out in {(10 - stopwatch.ElapsedMilliseconds / 1000f).ToString("N1")}";
						alertProgressBar.localScale = new Vector3(1 - stopwatch.ElapsedMilliseconds / 10000f, 1, 1);

						if (stopwatch.ElapsedMilliseconds > 10000) // Timeout if there hasn't been a response to the ping for 10s.
						{
							AddTextToHoldable("[IRC:Connect] Connection timed out.");
							stopwatch.Reset();
							_state = IRCConnectionState.Disconnected;
							continue;
						}
					}
				}

				if (input.EndOfStream)
				{
					Thread.Sleep(25);
					continue;
				}

				pingTimeoutTest = false;
				MainThreadQueue.Enqueue(() => ConnectionAlert.SetActive(false));
				stopwatch.Reset();
				stopwatch.Start();
				string buffer = input.ReadLine();
				MainThreadQueue.Enqueue(() =>
				{
					foreach (ActionMap action in Actions)
						if (action.TryMatch(buffer))
							break;
				});
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
				IRCCommand command;
				if (stopWatch.ElapsedMilliseconds <= _messageDelay && _state == IRCConnectionState.Connected) continue;
				lock (_sendQueue)
				{
					if (_sendQueue.Count == 0) continue;
					command = _sendQueue.Dequeue();
				}

				if (command.CommandIsColor() &&
					CurrentColor.Equals(command.GetColor(), StringComparison.InvariantCultureIgnoreCase))
					continue;

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
		MainThreadQueue.Enqueue(() => TwitchGame.EnableDisableInput());

		try
		{
			if (_state == IRCConnectionState.Disconnecting)
			{
				IRCCommand setColor = new IRCCommand($"PRIVMSG #{_settings.channelName} :.color {ColorOnDisconnect}");
				if (setColor.CommandIsColor())
				{
					AddTextToHoldable("[IRC:Disconnect] Color {0} was requested, setting it now.", ColorOnDisconnect);
					while (stopWatch.ElapsedMilliseconds < _messageDelay)
						Thread.Sleep(25);
					output.WriteLine(setColor.Command);
					output.Flush();

					stopWatch.Reset();
					stopWatch.Start();
					while (stopWatch.ElapsedMilliseconds < 1200)
						Thread.Sleep(25);
				}
				_state = IRCConnectionState.Disconnected;
				AddTextToHoldable("[IRC:Disconnect] Disconnected from chat IRC.");
			}

			lock (_sendQueue)
				_sendQueue.Clear();
		}
		catch
		{
			_state = IRCConnectionState.Disconnected;
		}
		MainThreadQueue.Enqueue(() =>
		{
			if (!gameObject.activeInHierarchy)
				AddTextToHoldable("[IRC:Disconnect] Twitch Plays disabled.");
		});
	}

	private void SetDelay(string badges, string nickname, string channel) //TODO account for known/verified bot status
	{
		if (!channel.Equals(_settings.channelName, StringComparison.InvariantCultureIgnoreCase) ||
			!nickname.Equals(_settings.userName, StringComparison.InvariantCultureIgnoreCase)) return;
		_messageDelay = MessageDelayUser;
		if (string.IsNullOrEmpty(badges))
			return;

		string[] badgeset = badges.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
		if (!badgeset.Any(badge => badge.StartsWith("broadcaster/") ||
									badge.StartsWith("moderator/") ||
									badge.StartsWith("admin/") ||
									badge.StartsWith("global_mod/") ||
									badge.StartsWith("staff/"))) return;
		_messageDelay = MessageDelayMod;
		_isModerator = true;
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
		new ActionMap(@"color=(#[0-9A-F]{6})?;display-name=([^;]+)?;.+user-id=(\d+).+:(\S+)!\S+ PRIVMSG #\S+ :(.+)", (GroupCollection groups) =>
		{
			// 1: color, 2: display-name, 3: user-id, 4: username, 5: message

			string userNickName = !string.IsNullOrEmpty(groups[2].Value) ? groups[2].Value : groups[4].Value;
			string userColorCode = groups[1].Value;
			string text = groups[5].Value;

			DebugHelper.Log($"[M] {userNickName} ({userColorCode}, {groups[3]}): {text}");
			ReceiveMessage(userNickName, userColorCode, text, silent: true);
		}, false),

		new ActionMap(@"badges=([^;]+)?;color=(#[0-9A-F]{6})?;display-name=([^;]+)?;emote-sets=\S+ :\S+ USERSTATE #(.+)", (GroupCollection groups) =>
		{
			Instance.CurrentColor = string.IsNullOrEmpty(groups[2].Value) ? string.Empty : groups[2].Value;
			Instance.SetDelay(groups[1].Value, groups[3].Value, groups[4].Value);
			Instance.SetOwnColor();
		}, false),

		new ActionMap(@":(\S+)!\S+ PRIVMSG #(\S+) :(.+)", (GroupCollection groups) => ReceiveMessage(groups[1].Value, null, groups[3].Value)),

		new ActionMap(@":(\S+)!\S+ WHISPER (\S+) :(.+)", (GroupCollection groups) => ReceiveMessage(groups[1].Value, null, groups[3].Value, true)),

		new ActionMap(@"PING (.+)", (GroupCollection groups) =>
		{
			AddTextToHoldable($"---PING--- ---PONG--- {groups[1].Value}");
			Instance.SendCommand($"PONG {groups[1].Value}");
		}, false),

		new ActionMap(@"\S* 001.*", (GroupCollection groups) =>
		{
			AddTextToHoldable(groups[0].Value);
			Instance.SendCommand($"JOIN #{Instance._settings.channelName}");
			Instance._state = IRCConnectionState.Connected;
			TwitchGame.EnableDisableInput();
			UserAccess.AddUser(Instance._settings.userName, AccessLevel.Streamer | AccessLevel.SuperUser | AccessLevel.Admin | AccessLevel.Mod);
			UserAccess.AddUser(Instance._settings.channelName.Replace("#", ""), AccessLevel.Streamer | AccessLevel.SuperUser | AccessLevel.Admin | AccessLevel.Mod);
			UserAccess.WriteAccessList();
		}, false),

		new ActionMap(@"\S* NOTICE \* :Login authentication failed", (GroupCollection groups) =>
		{
			AddTextToHoldable(groups[0].Value);
			Instance._state = IRCConnectionState.DoNotRetry;
		}, false),

		new ActionMap(@"\S* RECONNECT.*", (GroupCollection groups) =>
		{
			AddTextToHoldable(groups[0].Value);
			Instance._state = IRCConnectionState.Disconnected;
		}, false),

		new ActionMap(@".+", (GroupCollection groups) => AddTextToHoldable(groups[0].Value), false) //Log otherwise uncaptured lines.
	};
	public static bool CommandsEnabled = true;
	#endregion

	#region Public Fields
	public readonly MessageEvent OnMessageReceived = new MessageEvent();
	public string ColorOnDisconnect;
	public static IRCConnection Instance { get; private set; }
	public IRCConnectionState State => gameObject.activeInHierarchy ? _state : IRCConnectionState.Disabled;
	public string UserNickName { get; private set; }
	public string ChannelName { get; private set; }
	#endregion

	#region Private Fields
	private IRCConnectionData _data;
	private IRCConnectionState _state = IRCConnectionState.Disconnected;
	private bool ThreadAlive => _state == IRCConnectionState.Connecting || _state == IRCConnectionState.Connected;
	private KMModSettings _ircConnectionSettings;
	private TwitchPlaysService.ModSettingsJSON _settings;
	private const int MessageDelayUser = 2000;
	private const int MessageDelayMod = 500;
	private const int MaxMessageLength = 480;

	private Thread _inputThread;
	private Thread _outputThread;
	private bool _isModerator;
	private int _messageDelay = 2000;
	private bool _silenceMode;

	public string CurrentColor { get; private set; } = string.Empty;

	private readonly Queue<IRCMessage> _receiveQueue = new Queue<IRCMessage>();
	private readonly Queue<IRCCommand> _sendQueue = new Queue<IRCCommand>();
	private readonly Dictionary<string, Color> _userColors = new Dictionary<string, Color>();
	private IRCConnectionState _onDisableState = IRCConnectionState.Disconnected;
	private IEnumerator _keepConnectionAlive;
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
