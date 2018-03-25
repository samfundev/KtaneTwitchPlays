using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Prototype.GasVent;
using Assets.Scripts.Components.VennWire;
using Assets.Scripts.Missions;
using TwitchPlaysAssembly.Helpers;
using UnityEngine;

public class TwitchPlaysService : MonoBehaviour
{
    public class ModSettingsJSON
    {
	    public string authToken = "";
	    public string userName = "";
        public string channelName = "";
        public string serverName = "irc.twitch.tv";
        public int serverPort = 6667;
    }

    public BombMessageResponder bombMessageResponder = null;
    public PostGameMessageResponder postGameMessageResponder = null;
    public MissionMessageResponder missionMessageResponder = null;
    public MiscellaneousMessageResponder miscellaneousMessageResponder = null;

    private KMGameInfo _gameInfo = null;
    private CoroutineQueue _coroutineQueue = null;

    private MessageResponder _activeMessageResponder = null;

	private HashSet<Mod> CheckedMods = null;
	private TwitchPlaysProperties _publicProperties;
	private Queue<IEnumerator> _coroutinesToStart = new Queue<IEnumerator>();

	private void Start()
	{
		transform.Find("Prefabs").gameObject.SetActive(false);
	    bombMessageResponder = GetComponentInChildren<BombMessageResponder>(true);
	    postGameMessageResponder = GetComponentInChildren<PostGameMessageResponder>(true);
	    missionMessageResponder = GetComponentInChildren<MissionMessageResponder>(true);
	    miscellaneousMessageResponder = GetComponentInChildren<MiscellaneousMessageResponder>(true);

	    bombMessageResponder.twitchBombHandlePrefab = GetComponentInChildren<TwitchBombHandle>(true);
	    bombMessageResponder.twitchComponentHandlePrefab = GetComponentInChildren<TwitchComponentHandle>(true);
	    bombMessageResponder.moduleCamerasPrefab = GetComponentInChildren<ModuleCameras>(true);

		BombMessageResponder.Instance = bombMessageResponder;

		GameRoom.InitializeSecondaryCamera();
		_gameInfo = GetComponent<KMGameInfo>();
        _gameInfo.OnStateChange += OnStateChange;

        _coroutineQueue = GetComponent<CoroutineQueue>();

		Leaderboard.Instance.LoadDataFromFile();

        ModuleData.LoadDataFromFile();
        ModuleData.WriteDataToFile();

        TwitchPlaySettings.LoadDataFromFile();

        SetupResponder(bombMessageResponder);
        SetupResponder(postGameMessageResponder);
        SetupResponder(missionMessageResponder);
        SetupResponder(miscellaneousMessageResponder);

        bombMessageResponder.parentService = this;

	    GameObject infoObject = new GameObject("TwitchPlays_Info");
	    infoObject.transform.parent = gameObject.transform;
	    _publicProperties = infoObject.AddComponent<TwitchPlaysProperties>();
	    _publicProperties.TwitchPlaysService = this;
		if (TwitchPlaySettings.data.SkipModManagerInstructionScreen || IRCConnection.Instance.State == IRCConnectionState.Connected)
			ModManagerManualInstructionScreen.HasShownOnce = true;
	}

	private void OnDisable()
	{
		_coroutineQueue.StopQueue();
		_coroutineQueue.CancelFutureSubcoroutines();
		_coroutineQueue.StopForcedSolve();
		StopAllCoroutines();
	}

	private void OnEnable()
	{
		OnStateChange(KMGameInfo.State.Setup);
	}
	

	private void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            InputInterceptor.EnableInput();
        }
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
	    TwitchComponentHandle.ClaimedList.Clear();
    }

    private IEnumerator StopEveryCoroutine()
    {
        yield return new WaitForSeconds(2.0f);
	    _coroutinesToStart.Enqueue(MiscellaneousMessageResponder.FindHoldables());
        _coroutineQueue.StopQueue();
        _coroutineQueue.CancelFutureSubcoroutines();
	    _coroutineQueue.StopForcedSolve();
		StopAllCoroutines();
	    while (_coroutinesToStart.Count > 0)
		    StartCoroutine(_coroutinesToStart.Dequeue());

    }

    private void SetupResponder(MessageResponder responder)
    {
        if (responder != null)
        {
            responder.SetupResponder(_coroutineQueue);
        }
    }

    private MessageResponder GetActiveResponder(KMGameInfo.State state)
    {
	    switch (state)
        {
            case KMGameInfo.State.Gameplay:
	            DefaultCamera();
                return bombMessageResponder;

            case KMGameInfo.State.Setup:
	            DefaultCamera();
	            _coroutinesToStart.Enqueue(VanillaRuleModifier.Refresh());
	            _coroutinesToStart.Enqueue(MultipleBombs.Refresh());
	            _coroutinesToStart.Enqueue(FactoryRoomAPI.Refresh());
	            _coroutinesToStart.Enqueue(CreateSolversForAllBombComponents());

                return missionMessageResponder;

            case KMGameInfo.State.PostGame:
	            DefaultCamera();
                return postGameMessageResponder;

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
		if (CheckedMods == null) CheckedMods = new HashSet<Mod>();
		if (!(typeof(ModManager).GetField("loadedMods", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(ModManager.Instance) is Dictionary<string, Mod> loadedMods)) yield break;

		Mod[] mods = loadedMods.Values.Where(x => CheckedMods.Add(x)).ToArray();
		KMBombModule[] bombModules = mods.SelectMany(x => x.GetModObjects<KMBombModule>()).ToArray();
		KMNeedyModule[] needyModules = mods.SelectMany(x => x.GetModObjects<KMNeedyModule>()).ToArray();
		DebugHelper.Log($"Found {bombModules.Length} solvable modules and {needyModules.Length} needy modules in {mods.Length} mods");
		DebugHelper.Log($"Solvable Modules: {string.Join(", ", bombModules.Select(x => x.ModuleType).ToArray()).Wrap(80)}");
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
					solver = ComponentSolverFactory.CreateSolver(null, bombComponent.GetComponent<ModBombComponent>(), ComponentTypeEnum.Mod);
				}
				catch (Exception e)
				{
					DebugHelper.LogException(e, "Couldn't Create a component solver during startup for the following reason:");
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
			newModules = true;
			DebugHelper.Log("Creating a solver for each Needy module");
			foreach (KMNeedyModule bombComponent in needyModules)
			{
				ComponentSolver solver = null;
				try
				{
					solver = ComponentSolverFactory.CreateSolver(null, bombComponent.GetComponent<ModNeedyComponent>(), ComponentTypeEnum.NeedyMod);
				}
				catch (Exception e)
				{
					DebugHelper.LogException(e, "Couldn't Create a component solver during startup for the following reason:");
				}
				DebugHelper.Log(solver != null
					? $"Found a solver of type \"{solver.GetType().FullName}\" for needy component \"{bombComponent.ModuleDisplayName}\" ({bombComponent.ModuleType}). This module is {(solver.UnsupportedModule ? "not supported" : "supported")} by Twitch Plays."
					: $"No solver found for needy component \"{bombComponent.ModuleDisplayName}\". This module is not supported by Twitch Plays.");
				yield return null;
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
