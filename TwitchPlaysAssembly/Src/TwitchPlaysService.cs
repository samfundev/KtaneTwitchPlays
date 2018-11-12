using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Assets.Scripts.Props;
using UnityEngine;

public class TwitchPlaysService : MonoBehaviour
{
	public class ModSettingsJSON
	{
		// ReSharper disable InconsistentNaming
		public string authToken = "";
		public string userName = "";
		public string channelName = "";
		public string serverName = "irc.twitch.tv";
		public int serverPort = 6697;
		// ReSharper restore InconsistentNaming
	}

	public static TwitchPlaysService Instance;

	public TwitchGame twitchGame = null;
	public CoroutineQueue CoroutineQueue = null;
	public KMGameInfo.State CurrentState;

	private KMGameInfo _gameInfo;
	private TwitchPlaysProperties _publicProperties;
	private readonly Queue<IEnumerator> _coroutinesToStart = new Queue<IEnumerator>();
	private TwitchLeaderboard _leaderboardDisplay;
	private TwitchPlaysServiceData _data;

	public RectTransform BombHeader => _data.BombHeader;
	public TwitchLeaderboard TwitchLeaderboardPrefab => _data.TwitchLeaderboardPrefab;

	private void Awake()
	{
		_data = GetComponent<TwitchPlaysServiceData>();
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

		ModuleData.LoadDataFromFile();
		ModuleData.WriteDataToFile();

		TwitchPlaySettings.LoadDataFromFile();

		IRCConnection.Instance.OnMessageReceived.AddListener(OnMessageReceived);

		twitchGame.ParentService = this;

		GameObject infoObject = new GameObject("TwitchPlays_Info");
		infoObject.transform.parent = gameObject.transform;
		_publicProperties = infoObject.AddComponent<TwitchPlaysProperties>();
		_publicProperties.TwitchPlaysService = this; // Useless variable?
		if (TwitchPlaySettings.data.SkipModManagerInstructionScreen || IRCConnection.Instance.State == IRCConnectionState.Connected)
			ModManagerManualInstructionScreen.HasShownOnce = true;
	}

	private void OnDisable()
	{
		CoroutineQueue.StopQueue();
		CoroutineQueue.CancelFutureSubcoroutines();
		CoroutineQueue.StopForcedSolve();
		StopAllCoroutines();
	}

	private void OnEnable() => OnStateChange(KMGameInfo.State.Setup);

	private void Update()
	{
		if (Input.GetKey(KeyCode.Escape))
		{
			InputInterceptor.EnableInput();
		}

		if (Input.GetKeyDown(DebugSequence[_debugSequenceIndex].ToString()))
		{
			_debugSequenceIndex++;
			if (_debugSequenceIndex != DebugSequence.Length) return;

			TwitchPlaySettings.data.TwitchPlaysDebugEnabled = !TwitchPlaySettings.data.TwitchPlaysDebugEnabled;
			TwitchPlaySettings.WriteDataToFile();

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
	private string _inputCommand;

	private void OnGUI()
	{
		if (!TwitchPlaySettings.data.TwitchPlaysDebugEnabled) return;

		GUILayout.BeginArea(new Rect(50, Screen.height - 75, (Screen.width - 50) * 0.2f, 25));
		GUILayout.BeginHorizontal();
		_inputCommand = GUILayout.TextField(_inputCommand, GUILayout.MinWidth(50));
		if ((GUILayout.Button("Send") || Event.current.keyCode == KeyCode.Return) && _inputCommand.Length != 0)
		{
			if (_inputCommand.Equals(DebugSequence))
			{
				TwitchPlaySettings.data.TwitchPlaysDebugEnabled = !TwitchPlaySettings.data.TwitchPlaysDebugEnabled;
				TwitchPlaySettings.WriteDataToFile();
				_inputCommand = "";
				GUILayout.EndHorizontal();
				GUILayout.EndArea();
				return;
			}
			IRCConnection.SetDebugUsername();
			IRCConnection.SendMessage(_inputCommand);
			IRCConnection.ReceiveMessage(IRCConnection.Instance.UserNickName, IRCConnection.Instance.CurrentColor, _inputCommand);
			_inputCommand = "";
		}
		GUILayout.EndHorizontal();
		GUILayout.EndArea();
	}

	private void OnStateChange(KMGameInfo.State state)
	{
		CurrentState = state;
		if (!transform.gameObject.activeInHierarchy)
			return;

		StartCoroutine(StopEveryCoroutine());

		if (state != KMGameInfo.State.PostGame && _leaderboardDisplay != null)
		{
			DestroyObject(_leaderboardDisplay);
			_leaderboardDisplay = null;
		}

		twitchGame?.gameObject.SetActive(state == KMGameInfo.State.Gameplay);

		OtherModes.RefreshModes(state);

		switch (state)
		{
			case KMGameInfo.State.Gameplay:
				DefaultCamera();
				break;

			case KMGameInfo.State.Setup:
				DefaultCamera();
				_coroutinesToStart.Enqueue(VanillaRuleModifier.Refresh());
				_coroutinesToStart.Enqueue(MultipleBombs.Refresh());
				_coroutinesToStart.Enqueue(FactoryRoomAPI.Refresh());
				_coroutinesToStart.Enqueue(LogInstalledModdedModules());
				break;

			case KMGameInfo.State.PostGame:
				DefaultCamera();
				if (_leaderboardDisplay == null)
					_leaderboardDisplay = Instantiate(TwitchLeaderboardPrefab);
				Leaderboard.Instance.SaveDataToFile();
				break;

			case KMGameInfo.State.Transitioning:
				ModuleData.LoadDataFromFile();
				TwitchPlaySettings.LoadDataFromFile();
				break;
		}
	}

	public Dictionary<string, TwitchHoldable> Holdables = new Dictionary<string, TwitchHoldable>();

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
				Holdables["freeplay"] = new TwitchHoldable(holdable, commandType: typeof(FreeplayCommands));
			else if (holdable.GetComponent<BombBinder>() != null)
				Holdables["binder"] = new TwitchHoldable(holdable, commandType: typeof(MissionBinderCommands));
			else if (holdable.GetComponent<AlarmClock>() != null)
				Holdables["alarm"] = new TwitchHoldable(holdable, commandType: typeof(AlarmClockCommands));
			else if (holdable.GetComponent<IRCConnectionManagerHoldable>() != null)
				Holdables["ircmanager"] = new TwitchHoldable(holdable, commandType: typeof(IRCConnectionManagerCommands));
			else
			{
				var id = holdable.name.ToLowerInvariant().Replace("(clone)", "");
				// Make sure a modded holdable can’t override a built-in
				if (!Holdables.ContainsKey(id))
					Holdables[id] = new TwitchHoldable(holdable, allowModded: true);
			}
		}
	}

	private IEnumerator StopEveryCoroutine()
	{
		yield return new WaitForSeconds(2.0f);
		_coroutinesToStart.Enqueue(FindHoldables());
		CoroutineQueue.StopQueue();
		CoroutineQueue.CancelFutureSubcoroutines();
		CoroutineQueue.StopForcedSolve();
		StopAllCoroutines();
		while (_coroutinesToStart.Count > 0)
			StartCoroutine(_coroutinesToStart.Dequeue());
	}

	private void OnMessageReceived(Message msg)
	{
		var m = Regex.Match(msg.Text, @"^\s*!\s*(\w+)\s+(.+)$");
		if (m.Success)
		{
			TwitchBomb bomb;
			TwitchModule module;

			var prefix = m.Groups[1].Value.Trim();
			var restCommand = m.Groups[2].Value.Trim();

			// Commands for bombs by “!bomb X” referring to the current bomb
			if (CurrentState == KMGameInfo.State.Gameplay && prefix.EqualsIgnoreCase("bomb"))
				InvokeCommand(msg, restCommand, TwitchGame.Instance.Bombs[TwitchGame.Instance._currentBomb == -1 ? 0 : TwitchGame.Instance._currentBomb], typeof(BombCommands));
			// Commands for bombs by bomb name (e.g. “!bomb1 hold”)
			else if (CurrentState == KMGameInfo.State.Gameplay && (bomb = twitchGame.Bombs.FirstOrDefault(b => b.Code.EqualsIgnoreCase(prefix))) != null)
				InvokeCommand(msg, restCommand, bomb, typeof(BombCommands));

			// Commands for modules
			else if (CurrentState == KMGameInfo.State.Gameplay && (module = twitchGame.Modules.FirstOrDefault(md => md.Code.EqualsIgnoreCase(prefix))) != null)
				InvokeCommand(msg, restCommand, module, typeof(ModuleCommands));

			// Commands for holdables (check for these after bombs and modules so modded holdables can’t override them)
			else if (Holdables.TryGetValue(prefix, out var holdable))
			{
				if (holdable.CommandType != null) InvokeCommand(msg, restCommand, holdable, typeof(HoldableCommands), holdable.CommandType);
				else InvokeCommand(msg, restCommand, holdable, typeof(HoldableCommands));
			}
			else
				processGlobalCommand(msg);
		}
		else
			processGlobalCommand(msg);
	}

	public void processGlobalCommand(Message msg)
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
		public CommandAttribute Attr { get; private set; }
		public MethodInfo Method { get; private set; }
		public StaticCommand(CommandAttribute attr, MethodInfo method) { Attr = attr; Method = method; }

		public bool HasAttribute<T>() => Method.GetCustomAttributes(typeof(T), false).Length != 0;
	}

	private static readonly Dictionary<Type, StaticCommand[]> _commands = new Dictionary<Type, StaticCommand[]>();

	private StaticCommand[] GetCommands(Type type)
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

	private void InvokeCommand(Message msg, string cmdStr, params Type[] commandTypes) => InvokeCommand(msg, cmdStr, false, commandTypes);
	private void InvokeCommand<TObj>(Message msg, string cmdStr, TObj extraObject, params Type[] commandTypes)
	{
		Match m = null;
		foreach (var cmd in commandTypes.SelectMany(t => GetCommands(t)).OrderBy(cmd => cmd.Attr.Regex == null))
			if (cmd.Attr.Regex == null || (m = Regex.Match(cmdStr, cmd.Attr.Regex, RegexOptions.IgnoreCase)).Success)
				if (AttemptInvokeCommand(cmd, msg, cmdStr, m, extraObject))
					return;

		IRCConnection.SendMessage(@"@{0}, I don’t recognize that command.", msg.UserNickName, !msg.IsWhisper, msg.UserNickName);
	}

	private bool AttemptInvokeCommand<TObj>(StaticCommand command, Message msg, string cmdStr, Match m, TObj extraObject)
	{
		if (command.HasAttribute<DebuggingOnlyAttribute>() && !TwitchPlaySettings.data.EnableDebuggingCommands)
			return false;
		if (command.HasAttribute<ElevatorOnlyAttribute>() && !(GameRoom.Instance is ElevatorGameRoom))
			return false;
		if (command.HasAttribute<ElevatorDisallowedAttribute>() && GameRoom.Instance is ElevatorGameRoom)
			return false;

		if (!UserAccess.HasAccess(msg.UserNickName, TwitchPlaySettings.data.AnarchyMode ? command.Attr.AccessLevelAnarchy : command.Attr.AccessLevel, orHigher: true))
		{
			IRCConnection.SendMessage(@"@{0}, you need {1} access to use that command{2}.",
				msg.UserNickName,
				UserAccess.LevelToString(TwitchPlaySettings.data.AnarchyMode ? command.Attr.AccessLevelAnarchy : command.Attr.AccessLevel),
				TwitchPlaySettings.data.AnarchyMode ? " in anarchy mode" : "");
			// Return true so that the command counts as processed
			return true;
		}

		if (extraObject is TwitchModule mdl && mdl.Solved && !command.HasAttribute<SolvedAllowedAttribute>() && !TwitchPlaySettings.data.AnarchyMode)
		{
			IRCConnection.SendMessage(TwitchPlaySettings.data.AlreadySolved, mdl.Code, mdl.PlayerName, msg.UserNickName, mdl.BombComponent.GetModuleDisplayName());
			// Return true so that the command counts as processed (otherwise you get the above message multiple times)
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
					IRCConnection.SendMessage(group.Success ? @"@{0}, “{1}” is not a valid number." : @"@{0}, the command could not be parsed.", msg.UserNickName, !msg.IsWhisper, msg.UserNickName, group.Success ? group.Value : null);
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
			else if (parameters[i].ParameterType == typeof(Message))
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
				IRCConnection.SendMessage(@"@{0}, the “{1}” command has an unrecognized parameter “{2}”. This is a bug; please notify the devs.", msg.UserNickName, !msg.IsWhisper, msg.UserNickName, command.Method.Name, parameters[i].Name);
				return true;
			}
		}

		var invokeResult = command.Method.Invoke(null, arguments);
		if (invokeResult is bool invRes)
			return invRes;
		else if (invokeResult is IEnumerator coroutine)
			ProcessCommandCoroutine(coroutine, extraObject);
		else if (invokeResult != null)
			IRCConnection.SendMessage(@"@{0}, the “{1}” command returned something unrecognized. This is a bug; please notify the devs.", msg.UserNickName, !msg.IsWhisper, msg.UserNickName, command.Method.Name);
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

	private void DefaultCamera()
	{
		if (GameRoom.SecondaryCamera != null)
		{
			GameRoom.ResetCamera();
			GameRoom.ToggleCamera(true);
		}
	}

	private IEnumerator LogInstalledModdedModules()
	{
		yield return null;

		if (!(typeof(ModManager).GetField("loadedMods", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(ModManager.Instance) is Dictionary<string, Mod> loadedMods))
			yield break;

		Mod[] mods = loadedMods.Values.ToArray();
		KMBombModule[] bombModules = mods.SelectMany(x => x.GetModObjects<KMBombModule>()).ToArray();
		KMNeedyModule[] needyModules = mods.SelectMany(x => x.GetModObjects<KMNeedyModule>()).ToArray();
		DebugHelper.Log($"Found {bombModules.Length} solvable modules and {needyModules.Length} needy modules in {mods.Length} mods");
		DebugHelper.Log($"Solvable Modules: {string.Join(", ", bombModules.Select(x => x.ModuleType).ToArray()).Wrap(80)}");
		DebugHelper.Log($"Needy Modules: {string.Join(", ", needyModules.Select(x => x.ModuleType).ToArray()).Wrap(80)}");
		ModuleData.WriteDataToFile();
	}

	public void SetHeaderVisbility(bool visible)
	{
		StartCoroutine(AnimateHeaderVisiblity(visible));
	}

	private IEnumerator AnimateHeaderVisiblity(bool visbile)
	{
		var startPosition = BombHeader.anchoredPosition;
		var endPosition = new Vector2(0, visbile ? 0 : -24);
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
}
