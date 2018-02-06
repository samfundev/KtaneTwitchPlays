using System;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Missions;
using UnityEngine;

public class TwitchPlaysService : MonoBehaviour
{
    public class ModSettingsJSON
    {
        public string authToken;
        public string userName;
        public string channelName;
        public string serverName;
        public int serverPort;
        public bool debug = false;
        public bool shortUrls = false;
    }

    public BombMessageResponder bombMessageResponder = null;
    public PostGameMessageResponder postGameMessageResponder = null;
    public MissionMessageResponder missionMessageResponder = null;
    public MiscellaneousMessageResponder miscellaneousMessageResponder = null;

    private KMGameInfo _gameInfo = null;
    private KMModSettings _modSettings = null;
    private IRCConnection _ircConnection = null;
    private CoroutineQueue _coroutineQueue = null;
    private CoroutineCanceller _coroutineCanceller = null;

    private MessageResponder _activeMessageResponder = null;
    private Leaderboard _leaderboard = null;

    public static bool DebugMode = false;
    public static LogUploader logUploader = null;
    public static UrlHelper urlHelper = null;

	private HashSet<Mod> CheckedMods = null;
	private TwitchPlaysProperties _publicProperties;
	private Queue<IEnumerator> _coroutinesToStart = new Queue<IEnumerator>();
	

	private void Start()
    {
	    bombMessageResponder = GetComponentInChildren<BombMessageResponder>(true);
	    postGameMessageResponder = GetComponentInChildren<PostGameMessageResponder>(true);
	    missionMessageResponder = GetComponentInChildren<MissionMessageResponder>(true);
	    miscellaneousMessageResponder = GetComponentInChildren<MiscellaneousMessageResponder>(true);

	    bombMessageResponder.twitchBombHandlePrefab = GetComponentInChildren<TwitchBombHandle>(true);
	    bombMessageResponder.twitchComponentHandlePrefab = GetComponentInChildren<TwitchComponentHandle>(true);
	    bombMessageResponder.moduleCamerasPrefab = GetComponentInChildren<ModuleCameras>(true);

        _gameInfo = GetComponent<KMGameInfo>();
        _gameInfo.OnStateChange += OnStateChange;

        _modSettings = GetComponent<KMModSettings>();

        ModSettingsJSON settings = JsonConvert.DeserializeObject<ModSettingsJSON>(_modSettings.Settings);
        if (settings == null)
        {
            DebugHelper.LogError("Failed to read connection settings from mod settings.");
            return;
        }

        DebugMode = (settings.debug == true);
		
        _ircConnection = IRCConnection.MakeIRCConnection(_modSettings);
	    if (_ircConnection != null)
		    _ircConnection.transform.SetParent(transform);

        _coroutineCanceller = new CoroutineCanceller();

        _coroutineQueue = GetComponent<CoroutineQueue>();
        _coroutineQueue.coroutineCanceller = _coroutineCanceller;

        logUploader = GetComponent<LogUploader>();
        logUploader.ircConnection = _ircConnection;

        urlHelper = GetComponent<UrlHelper>();
        urlHelper.ChangeMode(settings.shortUrls == true);

        _leaderboard = new Leaderboard();
        _leaderboard.LoadDataFromFile();

        ModuleData.LoadDataFromFile();
        ModuleData.WriteDataToFile();

        TwitchPlaySettings.LoadDataFromFile();

        UserAccess.AddUser(settings.userName, AccessLevel.Streamer | AccessLevel.SuperUser | AccessLevel.Admin | AccessLevel.Mod);
        UserAccess.AddUser(settings.channelName.Replace("#",""), AccessLevel.Streamer | AccessLevel.SuperUser | AccessLevel.Admin | AccessLevel.Mod);
        UserAccess.WriteAccessList();

        SetupResponder(bombMessageResponder);
        SetupResponder(postGameMessageResponder);
        SetupResponder(missionMessageResponder);
        SetupResponder(miscellaneousMessageResponder);

        bombMessageResponder.leaderboard = _leaderboard;
        postGameMessageResponder.leaderboard = _leaderboard;
        miscellaneousMessageResponder.leaderboard = _leaderboard;

        bombMessageResponder.parentService = this;

	    GameObject infoObject = new GameObject("TwitchPlays_Info");
	    infoObject.transform.parent = gameObject.transform;
	    _publicProperties = infoObject.AddComponent<TwitchPlaysProperties>();
	    _publicProperties.TwitchPlaysService = this;
	}

	

	private void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            InputInterceptor.EnableInput();
        }
    }

    private void OnDestroy()
    {
        if (_ircConnection != null)
        {
            _ircConnection.ColorOnDisconnect = TwitchPlaySettings.data.TwitchBotColorOnQuit;
            _ircConnection.Disconnect();
        }
    }

    private void OnStateChange(KMGameInfo.State state)
    {
        if (_ircConnection == null)
        {
            return;
        }

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
    }

    private IEnumerator StopEveryCoroutine()
    {
        yield return new WaitForSeconds(2.0f);

        _coroutineQueue.StopQueue();
        _coroutineQueue.CancelFutureSubcoroutines();
        StopAllCoroutines();
	    while (_coroutinesToStart.Count > 0)
		    StartCoroutine(_coroutinesToStart.Dequeue());

    }

    private void SetupResponder(MessageResponder responder)
    {
        if (responder != null)
        {
            responder.SetupResponder(_ircConnection, _coroutineQueue, _coroutineCanceller);
        }
    }

    private MessageResponder GetActiveResponder(KMGameInfo.State state)
    {
        switch (state)
        {
            case KMGameInfo.State.Gameplay:
                return bombMessageResponder;

            case KMGameInfo.State.Setup:
	            _coroutinesToStart.Enqueue(VanillaRuleModifier.Refresh());
	            _coroutinesToStart.Enqueue(MultipleBombs.Refresh());
	            _coroutinesToStart.Enqueue(CreateSolversForAllBombComponents());

                return missionMessageResponder;

            case KMGameInfo.State.PostGame:
                return postGameMessageResponder;

            case KMGameInfo.State.Transitioning:
                ModuleData.LoadDataFromFile();
                TwitchPlaySettings.LoadDataFromFile();
                return null;

            default:
                return null;
        }
    }

	private IEnumerator CreateSolversForAllBombComponents()
	{
		yield return null;
		if (CheckedMods == null) CheckedMods = new HashSet<Mod>();
		if (!(typeof(ModManager).GetField("loadedMods", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(ModManager.Instance) is Dictionary<string, Mod> loadedMods)) yield break;

		Mod[] mods = loadedMods.Values.Where(x => CheckedMods.Add(x)).ToArray();
		KMBombModule[] bombModules = mods.SelectMany(x => x.GetModObjects<KMBombModule>()).ToArray();
		KMNeedyModule[] needyModules = mods.SelectMany(x => x.GetModObjects<KMNeedyModule>()).ToArray();
		DebugHelper.Log($"Found {bombModules.Length} solvable modules and {needyModules.Length} needy modules in {mods.Length} mods");
		DebugHelper.Log($"Solvable Modules: {string.Join(", ",bombModules.Select(x => x.ModuleType).ToArray()).Wrap(80)}");
		DebugHelper.Log($"Needy Modules: {string.Join(", ", needyModules.Select(x => x.ModuleType).ToArray()).Wrap(80)}");

		bool newModules = false;
		if (bombModules.Length > 0)
		{
			ComponentSolverFactory.SilentMode = true;
			newModules = true;
			DebugHelper.Log("Creating a solver for each Solvable module");
			foreach (KMBombModule bombComponent in bombModules)
			{
				ComponentSolver solver = null;
				try
				{
					solver = ComponentSolverFactory.CreateSolver(null, bombComponent.GetComponent<ModBombComponent>(), ComponentTypeEnum.Mod, _ircConnection, _coroutineCanceller);
				}
				catch (Exception e)
				{
					DebugHelper.LogException(e, "Couldn't Create a component solver during startup for the following reason:");
				}
				DebugHelper.Log(solver != null
					? $"Found a solver of type \"{solver.GetType().FullName}\" for solvable component \"{bombComponent.ModuleDisplayName}\" ({bombComponent.ModuleType}). This module is {(solver.UnsupportedModule ? "not supported" : "supported")} by Twitch Plays."
					: $"No solver found for solvable component \"{bombComponent.ModuleDisplayName}\". This module is not supported by Twitch Plays.");
			}
			DebugHelper.Log("Finished creating solvers for each Solvable module");
		}

		if (needyModules.Length > 0)
		{
			ComponentSolverFactory.SilentMode = true;
			newModules = true;
			DebugHelper.Log("Creating a solver for each Needy module");
			foreach (KMNeedyModule bombComponent in needyModules)
			{
				ComponentSolver solver = null;
				try
				{
					solver = ComponentSolverFactory.CreateSolver(null, bombComponent.GetComponent<ModNeedyComponent>(), ComponentTypeEnum.NeedyMod, _ircConnection, _coroutineCanceller);
				}
				catch (Exception e)
				{
					DebugHelper.LogException(e, "Couldn't Create a component solver during startup for the following reason:");
				}
				DebugHelper.Log(solver != null
					? $"Found a solver of type \"{solver.GetType().FullName}\" for needy component \"{bombComponent.ModuleDisplayName}\" ({bombComponent.ModuleType}). This module is {(solver.UnsupportedModule ? "not supported" : "supported")} by Twitch Plays."
					: $"No solver found for needy component \"{bombComponent.ModuleDisplayName}\". This module is not supported by Twitch Plays.");
			}
			DebugHelper.Log("Finished creating solvers for each Needy module");
		}

		ComponentSolverFactory.SilentMode = false;
		if (newModules)
		{
			ModuleData.DataHasChanged = true;
			ModuleData.WriteDataToFile();
		}
	}
}
