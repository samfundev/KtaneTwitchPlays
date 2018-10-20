using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TwitchPlaysAssembly.Helpers;
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

	public BombMessageResponder BombMessageResponder;
	public PostGameMessageResponder PostGameMessageResponder;
	public MissionMessageResponder MissionMessageResponder;
	public MiscellaneousMessageResponder MiscellaneousMessageResponder;

	public static TwitchPlaysService Instance;
	public CoroutineQueue CoroutineQueue;

	private KMGameInfo _gameInfo;

	private MessageResponder _activeMessageResponder;

	private HashSet<Mod> _checkedMods;
	private TwitchPlaysProperties _publicProperties;
	private readonly Queue<IEnumerator> _coroutinesToStart = new Queue<IEnumerator>();
	private TwitchPlaysServiceData _data;

	public RectTransform BombHeader => _data.BombHeader;

	private void Awake()
	{
		_data = GetComponent<TwitchPlaysServiceData>();
	}

	private void Start()
	{
		Instance = this;

		transform.Find("Prefabs").gameObject.SetActive(false);
		BombMessageResponder = GetComponentInChildren<BombMessageResponder>(true);
		PostGameMessageResponder = GetComponentInChildren<PostGameMessageResponder>(true);
		MissionMessageResponder = GetComponentInChildren<MissionMessageResponder>(true);
		MiscellaneousMessageResponder = GetComponentInChildren<MiscellaneousMessageResponder>(true);

		BombMessageResponder.TwitchBombHandlePrefab = GetComponentInChildren<TwitchBombHandle>(true);
		BombMessageResponder.TwitchModulePrefab = GetComponentInChildren<TwitchModule>(true);
		BombMessageResponder.ModuleCamerasPrefab = GetComponentInChildren<ModuleCameras>(true);

		BombMessageResponder.Instance = BombMessageResponder;

		GameRoom.InitializeSecondaryCamera();
		_gameInfo = GetComponent<KMGameInfo>();
		_gameInfo.OnStateChange += OnStateChange;

		CoroutineQueue = GetComponent<CoroutineQueue>();

		Leaderboard.Instance.LoadDataFromFile();

		ModuleData.LoadDataFromFile();
		ModuleData.WriteDataToFile();

		TwitchPlaySettings.LoadDataFromFile();

		SetupResponder(BombMessageResponder);
		SetupResponder(PostGameMessageResponder);
		SetupResponder(MissionMessageResponder);
		SetupResponder(MiscellaneousMessageResponder);

		BombMessageResponder.ParentService = this;

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
		if (!transform.gameObject.activeInHierarchy)
			return;

		StartCoroutine(StopEveryCoroutine());

		if (_activeMessageResponder != null)
		{
			_activeMessageResponder.gameObject.SetActive(false);
		}

		_activeMessageResponder = GetActiveResponder(state);

		if (_activeMessageResponder != null)
		{
			_activeMessageResponder.gameObject.SetActive(true);
		}
		TwitchModule.ClaimedList.Clear();
	}

	private IEnumerator StopEveryCoroutine()
	{
		yield return new WaitForSeconds(2.0f);
		_coroutinesToStart.Enqueue(MiscellaneousMessageResponder.FindHoldables());
		CoroutineQueue.StopQueue();
		CoroutineQueue.CancelFutureSubcoroutines();
		CoroutineQueue.StopForcedSolve();
		StopAllCoroutines();
		while (_coroutinesToStart.Count > 0)
			StartCoroutine(_coroutinesToStart.Dequeue());
	}

	private void SetupResponder(MessageResponder responder)
	{
		if (responder != null)
		{
			responder.SetupResponder(CoroutineQueue);
		}
	}

	private MessageResponder GetActiveResponder(KMGameInfo.State state)
	{
		switch (state)
		{
			case KMGameInfo.State.Gameplay:
				DefaultCamera();
				return BombMessageResponder;

			case KMGameInfo.State.Setup:
				DefaultCamera();
				_coroutinesToStart.Enqueue(VanillaRuleModifier.Refresh());
				_coroutinesToStart.Enqueue(MultipleBombs.Refresh());
				_coroutinesToStart.Enqueue(FactoryRoomAPI.Refresh());
				_coroutinesToStart.Enqueue(CreateSolversForAllBombComponents());

				return MissionMessageResponder;

			case KMGameInfo.State.PostGame:
				DefaultCamera();
				return PostGameMessageResponder;

			case KMGameInfo.State.Transitioning:
				ModuleData.LoadDataFromFile();
				TwitchPlaySettings.LoadDataFromFile();
				return null;

			default:
				return null;
		}
	}

	private void DefaultCamera()
	{
		if (GameRoom.SecondaryCamera != null)
		{
			GameRoom.ResetCamera();
			GameRoom.ToggleCamera(true);
		}
	}

	private IEnumerator CreateSolversForAllBombComponents()
	{
		yield return null;
		if (_checkedMods == null) _checkedMods = new HashSet<Mod>();
		if (!(typeof(ModManager).GetField("loadedMods", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(ModManager.Instance) is Dictionary<string, Mod> loadedMods)) yield break;

		Mod[] mods = loadedMods.Values.Where(x => _checkedMods.Add(x)).ToArray();
		KMBombModule[] bombModules = mods.SelectMany(x => x.GetModObjects<KMBombModule>()).ToArray();
		KMNeedyModule[] needyModules = mods.SelectMany(x => x.GetModObjects<KMNeedyModule>()).ToArray();
		DebugHelper.Log($"Found {bombModules.Length} solvable modules and {needyModules.Length} needy modules in {mods.Length} mods");
		DebugHelper.Log($"Solvable Modules: {string.Join(", ", bombModules.Select(x => x.ModuleType).ToArray()).Wrap(80)}");
		DebugHelper.Log($"Needy Modules: {string.Join(", ", needyModules.Select(x => x.ModuleType).ToArray()).Wrap(80)}");

		if (bombModules.Length > 0)
		{
			ComponentSolverFactory.SilentMode = true;
			DebugHelper.Log("Creating a solver for each Solvable module");
			foreach (KMBombModule bombComponent in bombModules)
			{
				ComponentSolver solver = null;
				try
				{
					solver = ComponentSolverFactory.CreateSolver(null, bombComponent.GetComponent<ModBombComponent>());
				}
				catch (Exception e)
				{
					DebugHelper.LogException(e, "Couldn't create a component solver during startup for the following reason:");
				}
				DebugHelper.Log(solver != null
					? $"Found a solver of type \"{solver.GetType().FullName}\" for solvable component \"{bombComponent.ModuleDisplayName}\" ({bombComponent.ModuleType}). This module is {(solver.UnsupportedModule ? "not supported" : "supported")} by Twitch Plays."
					: $"No solver found for solvable component \"{bombComponent.ModuleDisplayName}\". This module is not supported by Twitch Plays.");
				yield return null;
			}
			DebugHelper.Log("Finished creating solvers for each Solvable module");
		}

		if (needyModules.Length > 0)
		{
			ComponentSolverFactory.SilentMode = true;
			DebugHelper.Log("Creating a solver for each Needy module");
			foreach (KMNeedyModule bombComponent in needyModules)
			{
				ComponentSolver solver = null;
				try
				{
					solver = ComponentSolverFactory.CreateSolver(null, bombComponent.GetComponent<ModNeedyComponent>());
				}
				catch (Exception e)
				{
					DebugHelper.LogException(e, "Couldn't create a component solver during startup for the following reason:");
				}
				DebugHelper.Log(solver != null
					? $"Found a solver of type \"{solver.GetType().FullName}\" for needy component \"{bombComponent.ModuleDisplayName}\" ({bombComponent.ModuleType}). This module is {(solver.UnsupportedModule ? "not supported" : "supported")} by Twitch Plays."
					: $"No solver found for needy component \"{bombComponent.ModuleDisplayName}\". This module is not supported by Twitch Plays.");
				yield return null;
			}
			DebugHelper.Log("Finished creating solvers for each Needy module");
		}

		ComponentSolverFactory.SilentMode = false;
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
}
