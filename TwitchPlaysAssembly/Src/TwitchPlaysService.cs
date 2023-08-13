using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Assets.Scripts.Props;
using UnityEngine;
using UnityEngine.UI;

public class TwitchPlaysService : MonoBehaviour
{
	public class ModSettingsJSON
	{
		// ReSharper disable InconsistentNaming
		public string authToken = "";
		public string userName = "";
		public string channelName = "";
		public string serverName = "irc.chat.twitch.tv";
		public int serverPort = 6667;
		// ReSharper restore InconsistentNaming
	}

	public static TwitchPlaysService Instance;

	public TwitchGame twitchGame = null;
	public CoroutineQueue CoroutineQueue = null;
	public KMGameInfo.State CurrentState;
	public Action OnInteractCommand;

	private KMGameInfo _gameInfo;
	private TwitchPlaysProperties _publicProperties;
	private readonly Queue<IEnumerator> _coroutinesToStart = new Queue<IEnumerator>();
	private TwitchLeaderboard _leaderboardDisplay;
	private TwitchPlaysServiceData _data;
	private bool initialLoad;

	public Image HeaderBackground => _data.HeaderBackground;
	public Text HeaderText => _data.HeaderText;
	public Image MessagesBackground => _data.MessagesBackground;

	public RectTransform BombHeader => _data.BombHeader;
	public TwitchLeaderboard TwitchLeaderboardPrefab => _data.TwitchLeaderboardPrefab;

	// Returns a hue component to be used for colors of UI elements
	private static float UiHue => TwitchPlaySettings.data.EnableWhiteList ? .089f : .72f;

	public static string DataFolder
	{
		get
		{
			var path = Path.Combine(Application.persistentDataPath, "TwitchPlays");
			Directory.CreateDirectory(path);

			return path;
		}
	}

	private void Awake()
	{
		_data = GetComponent<TwitchPlaysServiceData>();
		chatSimulator = gameObject.Traverse("UI", "ChatSimulator");
		messageTemplate = chatSimulator.Traverse("ChatHistory", "ChatMessage");
	}

	private void Start()
	{
		Instance = this;

		transform.Find("Prefabs").gameObject.SetActive(false);
		twitchGame = GetComponentInChildren<TwitchGame>(true);

		twitchGame.twitchBombPrefab = GetComponentInChildren<TwitchBomb>(true);
		twitchGame.twitchModulePrefab = GetComponentInChildren<TwitchModule>(true);
		twitchGame.moduleCamerasPrefab = GetComponentInChildren<ModuleCameras>(true);

		TwitchGame.Instance = twitchGame;

		GameRoom.InitializeSecondaryCamera();
		_gameInfo = GetComponent<KMGameInfo>();
		_gameInfo.OnStateChange += OnStateChange;

		CoroutineQueue = GetComponent<CoroutineQueue>();

		Leaderboard.Instance.LoadDataFromFile();
		LeaderboardController.Install();

		ModuleData.LoadDataFromFile();
		ModuleData.WriteDataToFile();

		AuditLog.SetupLog();
		MainThreadQueue.Initialize();

		TwitchPlaySettings.LoadDataFromFile();

		MusicPlayer.LoadMusic();

		IRCConnection.Instance.OnMessageReceived += OnMessageReceived;

		twitchGame.ParentService = this;

		TwitchPlaysAPI.Setup();

		GameObject infoObject = new GameObject("TwitchPlays_Info");
		infoObject.transform.parent = gameObject.transform;
		_publicProperties = infoObject.AddComponent<TwitchPlaysProperties>();
		_publicProperties.TwitchPlaysService = this; // TODO: Useless variable?
		if (TwitchPlaySettings.data.SkipModManagerInstructionScreen || IRCConnection.Instance.State == IRCConnectionState.Connected)
			ModManagerManualInstructionScreen.HasShownOnce = true;

		UpdateUiHue();

		if (TwitchPlaySettings.data.DarkMode)
		{
			HeaderBackground.color = new Color32(0x0E, 0x0E, 0x10, 0xFF);
			HeaderText.color = new Color32(0xEF, 0xEF, 0xEC, 0xFF);
			MessagesBackground.color = new Color32(0x3A, 0x3A, 0x3D, 0xFF);
		}
		else
		{
			HeaderBackground.color = new Color32(0xEE, 0xEE, 0xEE, 0xFF);
			HeaderText.color = new Color32(0x4F, 0x4F, 0x4F, 0xFF);
			MessagesBackground.color = new Color32(0xD8, 0xD8, 0xD8, 0xFF);
		}

		SetupChatSimulator();

		// Delete the steam_appid.txt since it was created for a one-time steam boot.
		// Steam will have initialized before this.
		if (File.Exists("steam_appid.txt"))
		{
			File.Delete("steam_appid.txt");
		}
	}

	public void OnDisable()
	{
		CoroutineQueue.StopQueue();
		CoroutineQueue.CancelFutureSubcoroutines();
		CoroutineQueue.StopForcedSolve();
		StopAllCoroutines();
	}

	private void Update()
	{
		MainThreadQueue.ProcessQueue();

		if (Input.GetKey(KeyCode.Escape))
		{
			InputInterceptor.EnableInput();
			TwitchGame.ModuleCameras?.DisableCameraWall();
		}

		if (Input.GetKeyDown(DebugSequence[_debugSequenceIndex].ToString()))
		{
			_debugSequenceIndex++;
			if (_debugSequenceIndex != DebugSequence.Length) return;

			TwitchPlaySettings.data.TwitchPlaysDebugEnabled = !TwitchPlaySettings.data.TwitchPlaysDebugEnabled;
			TwitchPlaySettings.WriteDataToFile();

			chatSimulator.SetActive(TwitchPlaySettings.data.TwitchPlaysDebugEnabled);

			_debugSequenceIndex = 0;
			UserAccess.AddUser("_TPDEBUG".ToLowerInvariant(), AccessLevel.Streamer | AccessLevel.SuperUser | AccessLevel.Admin | AccessLevel.Mod);
			UserAccess.WriteAccessList();
		}
		else if (Input.anyKeyDown)
		{
			_debugSequenceIndex = 0;
		}
	}

	// Allow users to send commands from in game. Toggle the UI by typing "tpdebug".
	private const string DebugSequence = "tpdebug";
	private int _debugSequenceIndex;
	private readonly Queue<GameObject> chatMessages = new Queue<GameObject>(11);
	private GameObject chatSimulator;
	private GameObject messageTemplate;

	private void SetupChatSimulator()
	{
		chatSimulator.SetActive(TwitchPlaySettings.data.TwitchPlaysDebugEnabled);
		var num = MouseControls.SCREEN_BOUNDARY_PERCENT * Screen.height + 5;
		chatSimulator.transform.position = new Vector3(num, num, 0);

		var messageInput = chatSimulator.Traverse<InputField>("MessageInput");
		var sendButton = chatSimulator.Traverse<Button>("SendButton");

		messageInput.onEndEdit.AddListener(_ =>
		{
			if (Input.GetKey(KeyCode.Return))
			{
				sendButton.onClick.Invoke();
				messageInput.ActivateInputField();
			}
		});

		sendButton.onClick.AddListener(() =>
		{
			var message = messageInput.text;
			if (string.IsNullOrEmpty(message))
				return;

			if (message.Equals(DebugSequence))
			{
				TwitchPlaySettings.data.TwitchPlaysDebugEnabled = !TwitchPlaySettings.data.TwitchPlaysDebugEnabled;
				TwitchPlaySettings.WriteDataToFile();

				chatSimulator.SetActive(TwitchPlaySettings.data.TwitchPlaysDebugEnabled);
			}
			else
			{
				IRCConnection.SetDebugUsername();
				IRCConnection.SendMessage(message);
				IRCConnection.ReceiveMessage(IRCConnection.Instance.UserNickName, IRCConnection.Instance.CurrentColor, message);
			}

			messageInput.text = "";
		});
	}

	public void AddMessage(IRCMessage message)
	{
		if (message.IsWhisper)
			return;

		var chatHistory = chatSimulator.Traverse("ChatHistory");
		var chatMessage = Instantiate(messageTemplate, chatHistory.transform, false);
		chatMessage.GetComponent<Text>().text = $"<color={(!string.IsNullOrEmpty(message.UserColorCode) ? message.UserColorCode : "white")}>{message.UserNickName}</color>: {message.Text.Replace("<", "<<i></i>")}";
		chatMessage.transform.SetSiblingIndex(chatMessages.Count);
		chatMessage.SetActive(true);

		chatMessages.Enqueue(chatMessage);

		if (chatMessages.Count > 10)
			Destroy(chatMessages.Dequeue());
	}

	private void OnStateChange(KMGameInfo.State state)
	{
		CurrentState = state;

		if (!transform.gameObject.activeInHierarchy)
			return;

		Votes.OnStateChange();
		CheckSupport.Cleanup();

		if (state != KMGameInfo.State.PostGame && _leaderboardDisplay != null)
		{
			DestroyObject(_leaderboardDisplay);
			_leaderboardDisplay = null;
		}

		twitchGame?.gameObject.SetActive(state == KMGameInfo.State.Gameplay);

		OtherModes.RefreshModes(state);

		// Automatically check for updates after a round is finished or when entering the setup state but never more than once per hour.
		bool hourPassed = DateTime.Now.Subtract(Updater.LastCheck).TotalHours >= 1;
		if ((state == KMGameInfo.State.PostGame || state == KMGameInfo.State.Setup) && hourPassed && !Updater.UpdateAvailable)
		{
			_coroutinesToStart.Enqueue(Updater.CheckForUpdates().Yield(() =>
			{
				if (Updater.UpdateAvailable)
					IRCConnection.SendMessage("There is a new update to Twitch Plays!");
			}));
		}

		switch (state)
		{
			case KMGameInfo.State.Gameplay:
				DefaultCamera();
				LeaderboardController.DisableLeaderboards();
				break;

			case KMGameInfo.State.Setup:
				DefaultCamera();
				_coroutinesToStart.Enqueue(VanillaRuleModifier.Refresh());
				_coroutinesToStart.Enqueue(MultipleBombs.Refresh());
				_coroutinesToStart.Enqueue(FactoryRoomAPI.Refresh());

				if (!initialLoad)
				{
					initialLoad = true;
					_coroutinesToStart.Enqueue(ComponentSolverFactory.LoadDefaultInformation(true));
					_coroutinesToStart.Enqueue(Repository.LoadData());
					if (TwitchPlaySettings.data.TestModuleCompatibility && !TwitchPlaySettings.data.TwitchPlaysDebugEnabled)
						_coroutinesToStart.Enqueue(CheckSupport.FindSupportedModules());
					if (TwitchPlaySettings.data.EnableAutoProfiles)
						_coroutinesToStart.Enqueue(ProfileHelper.LoadAutoProfiles());
				}

				// Clear out the retry reward if we return to the setup room since the retry button doesn't return to setup.
				// A post game run command would set the retry bonus and then return to the setup room to start the mission, so we don't want to clear that.
				if (TwitchPlaySettings.GetRewardBonus() == 0)
					TwitchPlaySettings.ClearRetryReward();
				break;

			case KMGameInfo.State.PostGame:
				DefaultCamera();
				if (_leaderboardDisplay == null)
					_leaderboardDisplay = Instantiate(TwitchLeaderboardPrefab);
				Leaderboard.Instance.SaveDataToFile();
				_coroutinesToStart.Enqueue(PostGameCommands.AutoContinue());
				break;

			case KMGameInfo.State.Transitioning:
				ModuleData.LoadDataFromFile();
				TwitchPlaySettings.LoadDataFromFile();

				var pageManager = SceneManager.Instance?.SetupState?.Room.GetComponent<SetupRoom>().BombBinder.MissionTableOfContentsPageManager;
				if (pageManager != null)
				{
					var tableOfContentsList = pageManager.GetValue<List<Assets.Scripts.BombBinder.MissionTableOfContents>>("tableOfContentsList");
					if (tableOfContentsList[SetupState.LastBombBinderTOCIndex].ToCID == "toc_tp_search")
					{
						SetupState.LastBombBinderTOCIndex = 0;
						SetupState.LastBombBinderTOCPage = 0;
					}
				}

				break;
		}

		StopEveryCoroutine();
	}

	public Dictionary<string, TwitchHoldable> Holdables = new Dictionary<string, TwitchHoldable>();

	/// <summary>Adds a holdable, ensuring that the ID in <see cref="Holdables"/> is the same as the one in it's <see cref="TwitchHoldable"/>.</summary>
	private void AddHoldable(string id, FloatingHoldable holdable, Type commandType = null, bool allowModded = false)
	{
		Holdables[id] = new TwitchHoldable(holdable, commandType, allowModded, id);
	}

	private IEnumerator FindHoldables()
	{
		Holdables.Clear();
		yield return new WaitForSeconds(0.1f);
		foreach (var holdable in FindObjectsOfType<FloatingHoldable>())
		{
			// Bombs are blacklisted, as they are already handled by TwitchBomb.
			if (holdable.GetComponentInChildren<KMBomb>() != null)
				continue;
			else if (holdable.GetComponent<FreeplayDevice>() != null)
				AddHoldable("freeplay", holdable, commandType: typeof(FreeplayCommands));
			else if (holdable.GetComponent<BombBinder>() != null && CurrentState == KMGameInfo.State.Setup)
				AddHoldable("binder", holdable, commandType: typeof(MissionBinderCommands));
			else if (holdable.GetComponent<AlarmClock>() != null)
				AddHoldable("alarm", holdable, commandType: typeof(AlarmClockCommands));
			else if (holdable.GetComponent<IRCConnectionManagerHoldable>() != null)
				AddHoldable("ircmanager", holdable, commandType: typeof(IRCConnectionManagerCommands));
			else if (holdable.GetComponent("ModSelectorTablet") != null)
				AddHoldable("dmg", holdable, commandType: typeof(DMGCommands));
			else if (holdable.GetComponent<MainMenu>())
				AddHoldable("dossier", holdable, commandType: typeof(DossierCommands));
			else
			{
				var id = holdable.name.ToLowerInvariant().Replace("(clone)", "");
				// Make sure a modded holdable can’t override a built-in
				if (!Holdables.ContainsKey(id))
					AddHoldable(id, holdable, allowModded: true);
			}
		}
	}

	private void StopEveryCoroutine()
	{
		_coroutinesToStart.Enqueue(FindHoldables());
		CoroutineQueue.StopQueue();
		CoroutineQueue.CancelFutureSubcoroutines();
		CoroutineQueue.StopForcedSolve();
		StopAllCoroutines();
		while (_coroutinesToStart.Count > 0)
			StartCoroutine(_coroutinesToStart.Dequeue());
	}

	private void OnMessageReceived(IRCMessage msg)
	{
		var m = Regex.Match(msg.Text, @"^\s*!\s*(\w+)\s+(.+)$");
		if (m.Success)
		{
			TwitchBomb bomb;
			TwitchModule module;

			var prefix = m.Groups[1].Value.Trim();
			var restCommand = m.Groups[2].Value.Trim();

			// It's possible to be in the gameplay state but not have the bombs yet, we'll wait for them so that the user's command doesn't get dropped.
			if (CurrentState == KMGameInfo.State.Gameplay && TwitchGame.Instance.Bombs.Count == 0)
			{
				StartCoroutine(new WaitUntil(() => TwitchGame.Instance.Bombs.Count != 0).Yield(() => OnMessageReceived(msg)));
				return;
			}

			// Commands for bombs by “!bomb X” referring to the current bomb
			if (CurrentState == KMGameInfo.State.Gameplay && prefix.EqualsIgnoreCase("bomb"))
				InvokeCommand(msg, restCommand, TwitchGame.Instance.Bombs[TwitchGame.Instance._currentBomb == -1 ? 0 : TwitchGame.Instance._currentBomb], typeof(BombCommands));
			// Commands for bombs by bomb name (e.g. “!bomb1 hold”)
			else if (CurrentState == KMGameInfo.State.Gameplay && (bomb = twitchGame.Bombs.Find(b => b.Code.EqualsIgnoreCase(prefix))) != null)
				InvokeCommand(msg, restCommand, bomb, typeof(BombCommands));

			// Commands for modules
			else if (CurrentState == KMGameInfo.State.Gameplay && (module = twitchGame.Modules.Find(md => md.Code.EqualsIgnoreCase(prefix))) != null)
				InvokeCommand(msg, restCommand, module, typeof(ModuleCommands));

			// Commands for holdables (check for these after bombs and modules so modded holdables can’t override them)
			else if (Holdables.TryGetValue(prefix, out var holdable))
			{
				if (holdable.CommandType != null) InvokeCommand(msg, restCommand, holdable, typeof(HoldableCommands), holdable.CommandType);
				else InvokeCommand(msg, restCommand, holdable, typeof(HoldableCommands));
			}
			else
				ProcessGlobalCommand(msg);
		}
		else
			ProcessGlobalCommand(msg);
	}

	public void ProcessGlobalCommand(IRCMessage msg)
	{
		var m = Regex.Match(msg.Text, @"^\s*!\s*(.+)$");
		if (!m.Success)
			return;

		var fullCommand = m.Groups[1].Value.Trim();
		switch (CurrentState)
		{
			case KMGameInfo.State.Gameplay:
				InvokeCommand(msg, fullCommand, typeof(GlobalCommands), typeof(GameCommands));
				break;

			case KMGameInfo.State.PostGame:
				InvokeCommand(msg, fullCommand, typeof(GlobalCommands), typeof(PostGameCommands));
				break;

			default:
				InvokeCommand(msg, fullCommand, typeof(GlobalCommands));
				break;
		}
	}

	sealed class StaticCommand
	{
		public CommandAttribute Attr { get; }
		public MethodInfo Method { get; }
		public StaticCommand(CommandAttribute attr, MethodInfo method) { Attr = attr; Method = method; }

		public bool HasAttribute<T>() => Method.GetCustomAttributes(typeof(T), false).Length != 0;
	}

	private static readonly Dictionary<Type, StaticCommand[]> _commands = new Dictionary<Type, StaticCommand[]>();

	private static StaticCommand[] GetCommands(Type type)
	{
		if (_commands.TryGetValue(type, out var cmds))
			return cmds;

		var cmdsList = new List<StaticCommand>();
		foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
		{
			var attrs = method.GetCustomAttributes(typeof(CommandAttribute), false);
			if (attrs == null || attrs.Length == 0)
				continue;
			cmdsList.Add(new StaticCommand((CommandAttribute) attrs[0], method));
		}

		// Commands with a null regex are default/fallback commands. Make sure those are last in the list.
		return _commands[type] = cmdsList.OrderBy(cmd => cmd.Attr.Regex == null).ToArray();
	}

	delegate bool TryParse<T>(string value, out T result);
	enum NumberParseResult { Success, NotOfDesiredType, Error };

	private void InvokeCommand(IRCMessage msg, string cmdStr, params Type[] commandTypes) => InvokeCommand(msg, cmdStr, false, commandTypes);
	private void InvokeCommand<TObj>(IRCMessage msg, string cmdStr, TObj extraObject, params Type[] commandTypes)
	{
		Match m = null;
		foreach (var cmd in commandTypes.SelectMany(t => GetCommands(t)).OrderBy(cmd => cmd.Attr.Regex == null))
			if (cmd.Attr.Regex == null || (m = Regex.Match(cmdStr, cmd.Attr.Regex, RegexOptions.IgnoreCase)).Success)
				if (AttemptInvokeCommand(cmd, msg, cmdStr, m, extraObject))
					return;

		if (TwitchPlaySettings.data.ShowUnrecognizedCommandError)
			IRCConnection.SendMessage("@{0}, I don’t recognize that command.", msg.UserNickName, !msg.IsWhisper, msg.UserNickName);
	}

	private bool AttemptInvokeCommand<TObj>(StaticCommand command, IRCMessage msg, string cmdStr, Match m, TObj extraObject)
	{
		if (command.HasAttribute<DebuggingOnlyAttribute>() && !TwitchPlaySettings.data.EnableDebuggingCommands)
			return false;
		if (command.HasAttribute<ElevatorOnlyAttribute>() && !(GameRoom.Instance is ElevatorGameRoom))
			return false;
		if (command.HasAttribute<ElevatorDisallowedAttribute>() && GameRoom.Instance is ElevatorGameRoom)
			return false;

		if (!UserAccess.HasAccess(msg.UserNickName, TwitchPlaySettings.data.AnarchyMode ? command.Attr.AccessLevelAnarchy : command.Attr.AccessLevel, orHigher: true))
		{
			IRCConnection.SendMessageFormat("@{0}, you need {1} access to use that command{2}.",
				msg.UserNickName,
				UserAccess.LevelToString(TwitchPlaySettings.data.AnarchyMode ? command.Attr.AccessLevelAnarchy : command.Attr.AccessLevel),
				TwitchPlaySettings.data.AnarchyMode ? " in anarchy mode" : "");
			// Return true so that the command counts as processed
			return true;
		}

		if (extraObject is TwitchModule mdl)
		{
			if (mdl.Solved && !command.HasAttribute<SolvedAllowedAttribute>() && !TwitchPlaySettings.data.AnarchyMode)
			{
				IRCConnection.SendMessageFormat(TwitchPlaySettings.data.AlreadySolved, mdl.Code, mdl.PlayerName, msg.UserNickName, mdl.BombComponent.GetModuleDisplayName());
				// Return true so that the command counts as processed (otherwise you get the above message multiple times)
				return true;
			}

			if (mdl.Hidden)
			{
				IRCConnection.SendMessage($"Module {mdl.Code} is currently hidden and cannot be interacted with.");
				return true;
			}
		}

		// Check if commands are allowed to be sent to a holdable as they could be disabled in the settings.
		if (extraObject is TwitchHoldable holdable && !UserAccess.HasAccess(msg.UserNickName, AccessLevel.SuperUser, true) &&
			(
				(!TwitchPlaySettings.data.EnableMissionBinder && holdable.CommandType == typeof(MissionBinderCommands)) ||
				(!TwitchPlaySettings.data.EnableFreeplayBriefcase && holdable.CommandType == typeof(FreeplayCommands))
			)
		)
		{
			IRCConnection.SendMessage("That holdable is currently disabled and cannot be interacted with.");
			return true;
		}

		Leaderboard.Instance.GetRank(msg.UserNickName, out Leaderboard.LeaderboardEntry entry);
		if (entry?.Team == null && extraObject is TwitchModule && OtherModes.VSModeOn)
		{
			if (TwitchPlaySettings.data.AutoSetVSModeTeams)
			{
				if (TwitchPlaySettings.data.VSModePlayerLockout) IRCConnection.SendMessage($@"{msg.UserNickName}, you have not joined a team, and the bomb has already started. Use !join to play the next bomb.");
				else IRCConnection.SendMessage($@"{msg.UserNickName}, you have not joined a team, and cannot solve modules in this mode until you do, please use !join to be assigned a team.");
			}
			else IRCConnection.SendMessage($@"{msg.UserNickName}, you have not joined a team, and cannot solve modules in this mode until you do, please use !join evil or !join good.");
			// Return true so that the command counts as processed (otherwise you get the above message multiple times)
			return true;
		}

		if (!TwitchGame.IsAuthorizedDefuser(msg.UserNickName, msg.IsWhisper))
		{
			return true;
		}

		BanData ban = UserAccess.IsBanned(msg.UserNickName);
		if (ban != null)
		{
			if (double.IsPositiveInfinity(ban.BanExpiry))
			{
				IRCConnection.SendMessage($"Sorry @{msg.UserNickName}, You were restricted from using commands. You can request permission to send commands again by talking to the staff.", msg.UserNickName, !msg.IsWhisper);
			}
			else
			{
				int secondsRemaining = (int) (ban.BanExpiry - DateTime.Now.TotalSeconds());

				int daysRemaining = secondsRemaining / 86400; secondsRemaining %= 86400;
				int hoursRemaining = secondsRemaining / 3600; secondsRemaining %= 3600;
				int minutesRemaining = secondsRemaining / 60; secondsRemaining %= 60;
				string timeRemaining = $"{secondsRemaining} seconds.";
				if (daysRemaining > 0) timeRemaining = $"{daysRemaining} days, {hoursRemaining} hours, {minutesRemaining} minutes, {secondsRemaining} seconds.";
				else if (hoursRemaining > 0) timeRemaining = $"{hoursRemaining} hours, {minutesRemaining} minutes, {secondsRemaining} seconds.";
				else if (minutesRemaining > 0) timeRemaining = $"{minutesRemaining} minutes, {secondsRemaining} seconds.";

				IRCConnection.SendMessage($"Sorry @{msg.UserNickName}, You were temporarily restricted from using commands. You can participate again in {timeRemaining} or request permission by talking to the staff.", msg.UserNickName, !msg.IsWhisper);
			}
			return true;
		}

		var parameters = command.Method.GetParameters();
		var groupAttrs = parameters.Select(p => (GroupAttribute) p.GetCustomAttributes(typeof(GroupAttribute), false).FirstOrDefault()).ToArray();
		var arguments = new object[parameters.Length];
		for (int i = 0; i < parameters.Length; i++)
		{
			// Capturing groups from the regular expression
			if (groupAttrs[i] != null && m != null)
			{
				var group = m.Groups[groupAttrs[i].GroupIndex];
				NumberParseResult result;

				// Helper function to parse numbers (ints, floats, doubles)
				NumberParseResult IsNumber<TNum>(TryParse<TNum> tryParse)
				{
					var isNullable = parameters[i].ParameterType == typeof(Nullable<>).MakeGenericType(typeof(TNum));
					if (parameters[i].ParameterType != typeof(TNum) && !isNullable)
						return NumberParseResult.NotOfDesiredType;

					if (group.Success && tryParse(group.Value, out TNum rslt))
					{
						arguments[i] = rslt;
						return NumberParseResult.Success;
					}
					if (isNullable)
						return NumberParseResult.Success;
					IRCConnection.SendMessage(group.Success ? "@{0}, “{1}” is not a valid number." : "@{0}, the command could not be parsed.", msg.UserNickName, !msg.IsWhisper, msg.UserNickName, group.Success ? group.Value : null);
					return NumberParseResult.Error;
				}

				// Strings
				if (parameters[i].ParameterType == typeof(string))
					arguments[i] = m.Success ? group.Value : null;

				// Booleans — only specifies whether the group matched or not
				else if (parameters[i].ParameterType == typeof(bool))
					arguments[i] = group.Success;

				// Numbers (int, float, double); includes nullables
				else if (
					(result = IsNumber<int>(int.TryParse)) != NumberParseResult.NotOfDesiredType ||
					(result = IsNumber<float>(float.TryParse)) != NumberParseResult.NotOfDesiredType ||
					(result = IsNumber<double>(double.TryParse)) != NumberParseResult.NotOfDesiredType)
				{
					if (result == NumberParseResult.Error)
						return true;
				}
			}

			// Built-in parameter names
			else if (parameters[i].ParameterType == typeof(string) && parameters[i].Name == "user")
				arguments[i] = msg.UserNickName;
			else if (parameters[i].ParameterType == typeof(string) && parameters[i].Name == "cmd")
				arguments[i] = cmdStr;
			else if (parameters[i].ParameterType == typeof(bool) && parameters[i].Name == "isWhisper")
				arguments[i] = msg.IsWhisper;
			else if (parameters[i].ParameterType == typeof(IRCMessage))
				arguments[i] = msg;
			else if (parameters[i].ParameterType == typeof(KMGameInfo))
				arguments[i] = GetComponent<KMGameInfo>();
			else if (parameters[i].ParameterType == typeof(KMGameInfo.State))
				arguments[i] = CurrentState;
			else if (parameters[i].ParameterType == typeof(FloatingHoldable) && extraObject is TwitchHoldable twitchHoldable)
				arguments[i] = twitchHoldable.Holdable;

			// Object we passed in (module, bomb, holdable)
			else if (parameters[i].ParameterType.IsAssignableFrom(typeof(TObj)))
				arguments[i] = extraObject;
			else if (parameters[i].IsOptional)
				arguments[i] = parameters[i].DefaultValue;
			else
			{
				IRCConnection.SendMessage("@{0}, this is a bug; please notify the devs. Error: the “{1}” command has an unrecognized parameter “{2}”. It expects a type of “{3}”, and the extraObject is of type “{4}”.", msg.UserNickName, !msg.IsWhisper, msg.UserNickName, command.Method.Name, parameters[i].Name, parameters[i].ParameterType.Name, extraObject?.GetType().Name);
				return true;
			}
		}

		var invokeResult = command.Method.Invoke(null, arguments);
		if (invokeResult is bool invRes)
			return invRes;
		else if (invokeResult is IEnumerator coroutine)
			ProcessCommandCoroutine(coroutine, extraObject);
		else if (invokeResult != null)
			IRCConnection.SendMessage("@{0}, this is a bug; please notify the devs. Error: the “{1}” command returned something unrecognized.", msg.UserNickName, !msg.IsWhisper, msg.UserNickName, command.Method.Name);

		if ((TwitchPlaySettings.data.AnarchyMode ? command.Attr.AccessLevelAnarchy : command.Attr.AccessLevel) > AccessLevel.Defuser)
			AuditLog.Log(msg.UserNickName, UserAccess.HighestAccessLevel(msg.UserNickName), msg.Text);
		return true;
	}

	public void RunMission(KMMission mission)
	{
		if (CurrentState == KMGameInfo.State.Setup)
		{
			GetComponent<KMGameCommands>().StartMission(mission, "-1");
			OtherModes.RefreshModes(KMGameInfo.State.Transitioning);
		}
	}

	private void ProcessCommandCoroutine(IEnumerator coroutine, object extraObject)
	{
		// Make sure we are holding the correct bomb or holdable
		if (extraObject is TwitchHoldable holdable && holdable.Holdable.HoldState != FloatingHoldable.HoldStateEnum.Held)
			CoroutineQueue.AddToQueue(holdable.Hold());
		else if (extraObject is TwitchBomb bomb && !GameRoom.Instance.IsCurrentBomb(bomb.BombID))
			CoroutineQueue.AddToQueue(bomb.HoldBomb());
		else if (extraObject is TwitchModule module && !GameRoom.Instance.IsCurrentBomb(module.BombID))
			CoroutineQueue.AddToQueue(module.Bomb.HoldBomb());

		CoroutineQueue.AddToQueue(coroutine);
	}

	private static void DefaultCamera()
	{
		if (GameRoom.SecondaryCamera != null)
		{
			GameRoom.ResetCamera();
			GameRoom.ToggleCamera(true);
		}
	}

	public IEnumerator AnimateHeaderVisibility(bool visible)
	{
		var startPosition = BombHeader.anchoredPosition;
		var endPosition = new Vector2(0, visible ? 0 : -24);
		float startTime = Time.time;
		float alpha = 0;

		while (alpha < 1)
		{
			alpha = Mathf.Min((Time.time - startTime) / 0.75f, 1);
			BombHeader.anchoredPosition = Vector2.Lerp(startPosition, endPosition, 1 + Mathf.Pow(alpha - 1, 5));
			yield return null;
		}
	}

	public IEnumerator DropAllHoldables()
	{
		foreach (var holdable in Holdables.Values)
		{
			var drop = holdable.Drop();
			while (drop != null && drop.MoveNext())
				yield return drop.Current;
		}
	}

	public void UpdateUiHue()
	{
		var darkBand = Color.HSVToRGB(UiHue, .6f, .62f);
		BombHeader.GetComponent<Image>().color = darkBand;
		var moduleCameras = TwitchGame.ModuleCameras;
		if (moduleCameras != null)
			moduleCameras.SetNotes();

		foreach (var bomb in TwitchGame.Instance?.Bombs)
			bomb.EdgeworkID.color = darkBand;
	}

	/// <summary>Adds a coroutine that will run when the state changes.</summary>
	public void AddStateCoroutine(IEnumerator coroutine) => _coroutinesToStart.Enqueue(coroutine);
}