using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Assets.Scripts.Missions;
using Assets.Scripts.Props;
using UnityEngine;

public class BombMessageResponder : MessageResponder
{
    public TwitchBombHandle twitchBombHandlePrefab = null;
    public TwitchComponentHandle twitchComponentHandlePrefab = null;
    public ModuleCameras moduleCamerasPrefab = null;

    public Leaderboard leaderboard = null;
    public TwitchPlaysService parentService = null;

    public List<BombCommander> BombCommanders = new List<BombCommander>();
	public List<TwitchBombHandle> BombHandles = new List<TwitchBombHandle>();
    public List<TwitchComponentHandle> ComponentHandles = new List<TwitchComponentHandle>();
	public List<KMHoldableCommander> HoldableCommanders = new List<KMHoldableCommander>();
    private int _currentBomb = -1;
    private string[] _notes = new string[4];

    private AlarmClock alarmClock;

    public static ModuleCameras moduleCameras = null;

    public static bool BombActive { get; private set; }

	public static BombMessageResponder Instance = null;

    static BombMessageResponder()
    {
        BombActive = false;
    }

    #region Unity Lifecycle

    public static bool EnableDisableInput()
    {
        if (!TwitchPlaysService.DebugMode && TwitchPlaySettings.data.EnableTwitchPlaysMode && !TwitchPlaySettings.data.EnableInteractiveMode && BombActive)
        {
            InputInterceptor.DisableInput();
            return true;
        }
        else
        {
            InputInterceptor.EnableInput();
            return false;
        }
    }

    private void OnEnable()
    {
	    Instance = this;
        BombActive = true;
        EnableDisableInput();
        leaderboard.ClearSolo();
        TwitchPlaysService.logUploader.Clear();

		bool bombStarted = false;
		parentService.GetComponent<KMGameInfo>().OnLightsChange += delegate (bool on)
		{
			if (bombStarted || !on) return;
			bombStarted = true;

			if (TwitchPlaySettings.data.BombLiveMessageDelay > 0)
			{
				System.Threading.Thread.Sleep(TwitchPlaySettings.data.BombLiveMessageDelay * 1000);
			}

			if (BombCommanders.Count == 1)
			{
				IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.BombLiveMessage);
			}
			else
			{
				IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.MultiBombLiveMessage);
			}

            if (TwitchPlaySettings.data.EnableAutomaticEdgework) foreach (var commander in BombCommanders) commander.FillEdgework(commander.twitchBombHandle.bombID != _currentBomb);
			OtherModes.setMultiplier(TwitchPlaySettings.data.TimeModeStartingMultiplier);
		};

        StartCoroutine(CheckForBomb());
    }

	public string GetBombResult(bool lastBomb=true)
	{
		bool HasDetonated = false;
		bool HasBeenSolved = true;
		var timeStarting = float.MaxValue;
		var timeRemaining = float.MaxValue;
		var timeRemainingFormatted = "";

		foreach (var commander in BombCommanders)
		{
			HasDetonated |= commander.Bomb.HasDetonated;
			HasBeenSolved &= commander.IsSolved;
			if (timeRemaining > commander.CurrentTimer)
			{
				timeStarting = commander.bombStartingTimer;
				timeRemaining = commander.CurrentTimer;
			}

			if (!string.IsNullOrEmpty(timeRemainingFormatted))
			{
				timeRemainingFormatted += ", " + commander.GetFullFormattedTime;
			}
			else
			{
				timeRemainingFormatted = commander.GetFullFormattedTime;
			}
		}

		string bombMessage;
		if (HasDetonated)
		{
			bombMessage = string.Format(TwitchPlaySettings.data.BombExplodedMessage, timeRemainingFormatted);
			leaderboard.BombsExploded += BombCommanders.Count;
			if (lastBomb)
			{
				leaderboard.Success = false;
				TwitchPlaySettings.ClearPlayerLog();
			}
		}
		else if (HasBeenSolved)
		{
			bombMessage = string.Format(TwitchPlaySettings.data.BombDefusedMessage, timeRemainingFormatted);
			leaderboard.BombsCleared += BombCommanders.Count;
			bombMessage += TwitchPlaySettings.GiveBonusPoints(leaderboard);

			if (lastBomb)
			{
				leaderboard.Success = true;
			}

			if (leaderboard.CurrentSolvers.Count == 1)
			{
				float previousRecord = 0.0f;
				float elapsedTime = timeStarting - timeRemaining;
				string userName = "";
				foreach (string uName in leaderboard.CurrentSolvers.Keys)
				{
					userName = uName;
					break;
				}
				if (leaderboard.CurrentSolvers[userName] == (Leaderboard.RequiredSoloSolves * BombCommanders.Count))
				{
					leaderboard.AddSoloClear(userName, elapsedTime, out previousRecord);
					if (TwitchPlaySettings.data.EnableSoloPlayMode)
					{
						//Still record solo information, should the defuser be the only one to actually defuse a 11 * bomb-count bomb, but display normal leaderboards instead if
						//solo play is disabled.
						TimeSpan elapsedTimeSpan = TimeSpan.FromSeconds(elapsedTime);
						string soloMessage = string.Format(TwitchPlaySettings.data.BombSoloDefusalMessage, leaderboard.SoloSolver.UserName, (int)elapsedTimeSpan.TotalMinutes, elapsedTimeSpan.Seconds);
						if (elapsedTime < previousRecord)
						{
							TimeSpan previousTimeSpan = TimeSpan.FromSeconds(previousRecord);
							soloMessage += string.Format(TwitchPlaySettings.data.BombSoloDefusalNewRecordMessage, (int)previousTimeSpan.TotalMinutes, previousTimeSpan.Seconds);
						}
						soloMessage += TwitchPlaySettings.data.BombSoloDefusalFooter;
						parentService.StartCoroutine(SendDelayedMessage(1.0f, soloMessage));
					}
					else
					{
						leaderboard.ClearSolo();
					}
				}
				else
				{
					leaderboard.ClearSolo();
				}
			}
		}
		else
		{
			bombMessage = string.Format(TwitchPlaySettings.data.BombAbortedMessage, timeRemainingFormatted);
			leaderboard.Success = false;
			TwitchPlaySettings.ClearPlayerLog();
		}
		return bombMessage;
	}

    private void OnDisable()
    {
	    _hideBombs = false;
        BombActive = false;
        EnableDisableInput();
        TwitchComponentHandle.ClaimedList.Clear();
        TwitchComponentHandle.ClearUnsupportedModules();
        StopAllCoroutines();
        leaderboard.BombsAttempted++;

        TwitchPlaysService.logUploader.Post();
        parentService.StartCoroutine(SendDelayedMessage(1.0f, GetBombResult(), SendAnalysisLink));

		moduleCameras?.gameObject.SetActive(false);

        foreach (TwitchBombHandle handle in BombHandles)
        {
            if (handle != null)
            {
                Destroy(handle.gameObject, 2.0f);
            }
        }
        BombHandles.Clear();
        BombCommanders.Clear();
	    HoldableCommanders.Clear();

	    DestroyComponentHandles();

        MusicPlayer.StopAllMusic();
    }

	public void DestroyComponentHandles()
	{
		if (ComponentHandles == null) return;

		foreach (TwitchComponentHandle handle in ComponentHandles)
		{
			Destroy(handle.gameObject, 2.0f);
		}
		ComponentHandles.Clear();
	}

	#endregion

    #region Protected/Private Methods

	private IEnumerator CheckForBomb()
	{
		TwitchComponentHandle.ResetId();

		
		yield return new WaitUntil(() => (SceneManager.Instance.GameplayState.Bombs != null && SceneManager.Instance.GameplayState.Bombs.Count > 0));
		List<Bomb> bombs = SceneManager.Instance.GameplayState.Bombs;

		for (int i = 0; i < GameRoom.GameRoomTypes.Length; i++)
		{
			if (GameRoom.GameRoomTypes[i]() != null && GameRoom.CreateRooms[i](FindObjectsOfType(GameRoom.GameRoomTypes[i]()), out GameRoom.Instance))
			{
				GameRoom.Instance.InitializeBombs(bombs);
				break;
			}
		}

		try
		{
			GameRoom.Instance.InitializeBombNames();
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "An exception has occured while setting the bomb names");
		}
		StartCoroutine(GameRoom.Instance.ReportBombStatus());

		try
		{
			if (GameRoom.Instance.HoldBomb)
				_coroutineQueue.AddToQueue(BombHandles[0].OnMessageReceived(BombHandles[0].nameText.text, "red", "bomb hold"), _currentBomb);
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "An exception has occured attempting to hold the bomb.");
		}

		alarmClock = FindObjectOfType<AlarmClock>();

		try
		{
			moduleCameras = Instantiate<ModuleCameras>(moduleCamerasPrefab);
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "Failed to Instantiate the module Camera system due to an Exception: ");
			moduleCameras = null;
		}

		for (int i = 0; i < 4; i++)
		{
			_notes[i] = TwitchPlaySettings.data.NotesSpaceFree;
			moduleCameras?.SetNotes(i, TwitchPlaySettings.data.NotesSpaceFree);
		}

		if (EnableDisableInput())
		{
			TwitchComponentHandle.SolveUnsupportedModules(true);
		}

		FloatingHoldable[] holdables = FindObjectsOfType<FloatingHoldable>();
		foreach (FloatingHoldable holdable in holdables)
		{
			//Bombs are blacklisted, as they are already handled by BombCommander.
			if (holdable.name.StartsWith("BasicRectangleBomb")) continue;
			if (holdable.GetComponentInChildren<KMBomb>() != null) continue;
			try
			{
				DebugHelper.Log($"Creating holdable handler for {holdable.name}");
				KMHoldableCommander holdableCommander = new KMHoldableCommander(holdable, IRCConnection.Instance);
				HoldableCommanders.Add(holdableCommander);
			}
			catch (Exception ex)
			{
				DebugHelper.LogException(ex, $"Could not create a handler for holdable {holdable.name} due to an exception:");
			}
		}
	}

	public void SetBomb(Bomb bomb, int id)
	{
		if(BombCommanders.Count == 0)
			_currentBomb = id == -1 ? -1 : 0;
        BombCommanders.Add(new BombCommander(bomb));
        CreateBombHandleForBomb(bomb, id);
        CreateComponentHandlesForBomb(bomb);
    }

    protected override void OnMessageReceived(string userNickName, string userColorCode, string text)
    {
	    if (!text.StartsWith("!") || text.Equals("!")) return;
	    text = text.Substring(1);

		if (text.EqualsAny("notes1","notes2","notes3","notes4"))
        {
            int index = "1234".IndexOf(text.Substring(5, 1), StringComparison.Ordinal);
	        IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.Notes, index+1, _notes[index]);
            return;
        }

        if (text.StartsWith("notes1 ", StringComparison.InvariantCultureIgnoreCase) || text.StartsWith("notes2 ", StringComparison.InvariantCultureIgnoreCase) ||
            text.StartsWith("notes3 ", StringComparison.InvariantCultureIgnoreCase) || text.StartsWith("notes4 ", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!IsAuthorizedDefuser(userNickName)) return;
            int index = "1234".IndexOf(text.Substring(5, 1), StringComparison.Ordinal);
            string notes = text.Substring(7);
            if (notes == "") return;

	        IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.NotesTaken, index+1 , notes);

            _notes[index] = notes;
	        moduleCameras?.SetNotes(index, notes);
            return;
        }

        if (text.StartsWith("appendnotes1 ", StringComparison.InvariantCultureIgnoreCase) || text.StartsWith("appendnotes2 ", StringComparison.InvariantCultureIgnoreCase) ||
            text.StartsWith("appendnotes3 ", StringComparison.InvariantCultureIgnoreCase) || text.StartsWith("appendnotes4 ", StringComparison.InvariantCultureIgnoreCase))
        {
            text = text.Substring(6, 6) + text.Substring(0, 6) + text.Substring(12);
        }

        if (text.StartsWith("notes1append ", StringComparison.InvariantCultureIgnoreCase) || text.StartsWith("notes2append ", StringComparison.InvariantCultureIgnoreCase) ||
            text.StartsWith("notes3append ", StringComparison.InvariantCultureIgnoreCase) || text.StartsWith("notes4append ", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!IsAuthorizedDefuser(userNickName)) return;
            int index = "1234".IndexOf(text.Substring(5, 1), StringComparison.Ordinal);
            string notes = text.Substring(13);
            if (notes == "") return;

	        IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.NotesAppended, index + 1, notes);

            _notes[index] += " " + notes;
	        moduleCameras?.AppendNotes(index, notes);
            return;
        }

        if (text.EqualsAny("clearnotes1", "clearnotes2", "clearnotes3", "clearnotes4"))
        {
            text = text.Substring(5, 6) + text.Substring(0, 5);
        }

        if (text.EqualsAny("notes1clear", "notes2clear", "notes3clear", "notes4clear"))
        {
            if (!IsAuthorizedDefuser(userNickName)) return;
            int index = "1234".IndexOf(text.Substring(5, 1), StringComparison.Ordinal);
            _notes[index] = TwitchPlaySettings.data.NotesSpaceFree;
	        IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.NoteSlotCleared, index + 1);

	        moduleCameras?.SetNotes(index, TwitchPlaySettings.data.NotesSpaceFree);
            return;
        }

        if (text.Equals("snooze", StringComparison.InvariantCultureIgnoreCase))
		{ 
            if (!IsAuthorizedDefuser(userNickName)) return;
            if (TwitchPlaySettings.data.AllowSnoozeOnly)
                alarmClock?.TurnOff();
            else
                alarmClock?.ButtonDown(0);
            return;
        }

        if (text.Equals("stop", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!IsAuthorizedDefuser(userNickName, true)) return;
            _currentBomb = _coroutineQueue.CurrentBombID;
            return;
        }

        if (text.Equals("modules", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!IsAuthorizedDefuser(userNickName)) return;
            moduleCameras?.AttachToModules(ComponentHandles);
            return;
        }

        if (text.StartsWith("claims ", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!IsAuthorizedDefuser(userNickName)) return;
            userNickName = text.Substring(7);
            text = "claims";
            if (userNickName == "")
            {
                return;
            }
        }

        if (text.Equals("claims", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!IsAuthorizedDefuser(userNickName)) return;
            List<string> claimed = (from handle in ComponentHandles where handle.PlayerName != null && handle.PlayerName.Equals(userNickName, StringComparison.InvariantCultureIgnoreCase) && !handle.Solved select string.Format(TwitchPlaySettings.data.OwnedModule, handle.idTextMultiDecker.text, handle.HeaderText)).ToList();
            if (claimed.Count > 0)
            {
                string message = string.Format(TwitchPlaySettings.data.OwnedModuleList, userNickName, string.Join(", ", claimed.ToArray(), 0, Math.Min(claimed.Count, 5)));
                if (claimed.Count > 5)
                    message += "...";
	            IRCConnection.Instance.SendMessage(message);
            }
            else
	            IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.NoOwnedModules, userNickName);
            return;
        }

        if (text.StartsWith("claim ", StringComparison.InvariantCultureIgnoreCase))
        {
            var split = text.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var claim in split.Skip(1))
            {
                TwitchComponentHandle handle = ComponentHandles.FirstOrDefault(x => x.Code.Equals(claim));
                if (handle == null || !GameRoom.Instance.IsCurrentBomb(handle.bombID)) continue;
                handle.OnMessageReceived(userNickName, userColorCode, string.Format("{0} claim", claim));
            }
            return;
        }

        if (text.EqualsAny("unclaim all", "release all","unclaimall","releaseall"))
        {
            string[] moduleIDs = ComponentHandles.Where(x => x.PlayerName != null && x.PlayerName.Equals(userNickName, StringComparison.InvariantCultureIgnoreCase))
                .Select(x => x.Code).ToArray();
            text = string.Format("unclaim {0}", string.Join(" ", moduleIDs));
        }

        if (text.StartsWith("unclaim ", StringComparison.InvariantCultureIgnoreCase) || text.StartsWith("release ", StringComparison.InvariantCultureIgnoreCase))
        {
            var split = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var claim in split.Skip(1))
            {
                TwitchComponentHandle handle = ComponentHandles.FirstOrDefault(x => x.Code.Equals(claim));
                if (handle == null || !GameRoom.Instance.IsCurrentBomb(handle.bombID)) continue;
                handle.OnMessageReceived(userNickName, userColorCode, string.Format("{0} unclaim", claim));
            }
            return;
        }

	    if (text.StartsWith("assign ", StringComparison.InvariantCultureIgnoreCase))
	    {
		    var split = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (!UserAccess.HasAccess(userNickName, AccessLevel.Mod, true) || split.Length < 3) return;
		    foreach (var assign in split.Skip(2))
		    {
			    TwitchComponentHandle handle = ComponentHandles.FirstOrDefault(x => x.Code.Equals(assign));
			    if (handle == null || !GameRoom.Instance.IsCurrentBomb(handle.bombID)) continue;
			    handle.OnMessageReceived(userNickName, userColorCode, string.Format("{0} assign {1}", assign, split[1]));
		    }
		    return;
	    }

		if (text.Equals("unclaimed", StringComparison.InvariantCultureIgnoreCase))
		{
			if (!IsAuthorizedDefuser(userNickName)) return;

			IEnumerable<string> unclaimed = ComponentHandles.Where(handle => !handle.claimed && !handle.Solved && GameRoom.Instance.IsCurrentBomb(handle.bombID)).Shuffle().Take(3)
				.Select(handle => string.Format("{0} ({1})", handle.HeaderText, handle.Code)).ToList();
			
			if (unclaimed.Any()) IRCConnection.Instance.SendMessage("Unclaimed Modules: {0}", unclaimed.Join(", "));
			else IRCConnection.Instance.SendMessage("There are no more unclaimed modules.");

			return;
		}

		if (text.StartsWith("find ", StringComparison.InvariantCultureIgnoreCase) || text.StartsWith("search ", StringComparison.InvariantCultureIgnoreCase))
		{
			if (!IsAuthorizedDefuser(userNickName)) return;

			var split = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			string query = split.Skip(1).Join(" ");
			IEnumerable<string> modules = ComponentHandles.Where(handle => handle.HeaderText.ContainsIgnoreCase(query) && GameRoom.Instance.IsCurrentBomb(handle.bombID))
                .OrderByDescending(handle => handle.HeaderText.EqualsIgnoreCase(query)).ThenBy(handle => handle.Solved).ThenBy(handle => handle.PlayerName != null).Take(3)
				.Select(handle => string.Format("{0} ({1}) - {2}", handle.HeaderText, handle.Code, 
					handle.Solved ? "Solved" : (handle.PlayerName == null ? "Unclaimed" : "Claimed by " + handle.PlayerName)
				)).ToList();

			if (modules.Any()) IRCConnection.Instance.SendMessage("Modules: {0}", modules.Join(", "));
			else IRCConnection.Instance.SendMessage("Couldn't find any modules containing \"{0}\".", query);

			return;
		}

		if (text.Equals("filledgework", StringComparison.InvariantCultureIgnoreCase) && UserAccess.HasAccess(userNickName, AccessLevel.Mod, true))
		{
		    foreach (var commander in BombCommanders) commander.FillEdgework(_currentBomb != commander.twitchBombHandle.bombID);
			return;
		}

        if (text.StartsWith("setmultiplier", StringComparison.InvariantCultureIgnoreCase) && UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true))
        {
			if(float.TryParse(text.Substring(14), out float tempNumber))
				OtherModes.setMultiplier(tempNumber);
        }

        if (text.Equals("solvebomb", StringComparison.InvariantCultureIgnoreCase) && UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true))
		{
			foreach (var handle in ComponentHandles.Where(x => GameRoom.Instance.IsCurrentBomb(x.bombID))) if (!handle.Solved) handle.SolveSilently();
			return;
		}

	    if (text.Equals("enablecamerawall", StringComparison.InvariantCultureIgnoreCase) && UserAccess.HasAccess(userNickName, AccessLevel.Admin, true))
	    {
		    moduleCameras.EnableWallOfCameras();
			StartCoroutine(HideBombs());
	    }

	    if (text.Equals("disablecamerawall", StringComparison.InvariantCultureIgnoreCase) && UserAccess.HasAccess(userNickName, AccessLevel.Admin, true))
	    {
		    moduleCameras.DisableWallOfCameras();
		    _hideBombs = false;
	    }

        GameRoom.Instance.RefreshBombID(ref _currentBomb);

        if (_currentBomb > -1)
        {
            //Check for !bomb messages, and pass them off to the currently held bomb.
            Match match = Regex.Match(text, "^bomb (.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string internalCommand = match.Groups[1].Value;
                text = string.Format("bomb{0} {1}", _currentBomb + 1, internalCommand);
            }

            match = Regex.Match(text, "^edgework$");
            if (match.Success)
            {
                text = string.Format("edgework{0}", _currentBomb + 1);
            }
            else
            {
                match = Regex.Match(text, "^edgework (.+)");
                if (match.Success)
                {
                    string internalCommand = match.Groups[1].Value;
                    text = string.Format("edgework{0} {1}", _currentBomb + 1, internalCommand);
                }
            }
        }

        foreach (TwitchBombHandle handle in BombHandles)
        {
	        if (handle == null) continue;
	        IEnumerator onMessageReceived = handle.OnMessageReceived(userNickName, userColorCode, text);
	        if (onMessageReceived == null)
	        {
		        continue;
	        }

	        if (_currentBomb != handle.bombID)
	        {
		        if (!GameRoom.Instance.IsCurrentBomb(handle.bombID))
			        continue;

		        _coroutineQueue.AddToQueue(BombHandles[_currentBomb].HideMainUIWindow(), handle.bombID);
		        _coroutineQueue.AddToQueue(handle.ShowMainUIWindow(), handle.bombID);
		        _coroutineQueue.AddToQueue(BombCommanders[_currentBomb].LetGoBomb(), handle.bombID);

		        _currentBomb = handle.bombID;
	        }
	        _coroutineQueue.AddToQueue(onMessageReceived, handle.bombID);
        }

        foreach (TwitchComponentHandle componentHandle in ComponentHandles)
        {
            if (!GameRoom.Instance.IsCurrentBomb(componentHandle.bombID)) continue;
            IEnumerator onMessageReceived = componentHandle.OnMessageReceived(userNickName, userColorCode, text);
	        if (onMessageReceived == null) continue;

	        if (_currentBomb != componentHandle.bombID)
	        {
		        _coroutineQueue.AddToQueue(BombHandles[_currentBomb].HideMainUIWindow(), componentHandle.bombID);
		        _coroutineQueue.AddToQueue(BombHandles[componentHandle.bombID].ShowMainUIWindow(), componentHandle.bombID);
		        _coroutineQueue.AddToQueue(BombCommanders[_currentBomb].LetGoBomb(),componentHandle.bombID);
		        _currentBomb = componentHandle.bombID;
	        }
	        _coroutineQueue.AddToQueue(onMessageReceived,componentHandle.bombID);
        }

	    foreach (KMHoldableCommander commander in HoldableCommanders)
	    {
		    string[] split = text.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		    string textAfter = split.Skip(1).Join();
			if (string.IsNullOrEmpty(commander?.ID) || !commander.ID.Equals(split[0])) continue;
		    if (textAfter.EqualsAny("help", "manual"))
		    {
			    commander.Handler.ShowHelp();
			    break;
		    }
		    _coroutineQueue.AddToQueue(BombCommanders[_currentBomb != -1 ? _currentBomb : 0].LetGoBomb(), _currentBomb);
		    _coroutineQueue.AddToQueue(commander.RespondToCommand(userNickName, textAfter, IRCConnection.Instance));
		}

	    if (TwitchPlaySettings.data.BombCustomMessages.ContainsKey(text.ToLowerInvariant()))
	    {
		    IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.BombCustomMessages[text.ToLowerInvariant()]);
	    }
    }

	private bool _hideBombs = false;
	private IEnumerator HideBombs()
	{
		if (_hideBombs) yield break;
		_hideBombs = true;
		while (_hideBombs)
		{
			foreach (BombCommander commander in BombCommanders)
			{
				commander.Bomb.transform.localPosition = new Vector3(0, -1.25f, 0);
			}
			yield return null;
		}
	}

    private void CreateBombHandleForBomb(MonoBehaviour bomb, int id)
    {
        TwitchBombHandle _bombHandle = Instantiate<TwitchBombHandle>(twitchBombHandlePrefab);
        _bombHandle.bombID = id;
        _bombHandle.ircConnection = IRCConnection.Instance;
        _bombHandle.bombCommander = BombCommanders[BombCommanders.Count-1];
        _bombHandle.coroutineQueue = _coroutineQueue;
        BombHandles.Add(_bombHandle);
        BombCommanders[BombCommanders.Count - 1].twitchBombHandle = _bombHandle;
    }

    public bool CreateComponentHandlesForBomb(Bomb bomb)
    {
        bool foundComponents = false;

        List<BombComponent> bombComponents = bomb.BombComponents;

		var bombCommander = BombCommanders[BombCommanders.Count - 1];

        foreach (BombComponent bombComponent in bombComponents)
        {
            ComponentTypeEnum componentType = bombComponent.ComponentType;

            switch (componentType)
            {
                case ComponentTypeEnum.Empty:
                    continue;

                case ComponentTypeEnum.Timer:
                    BombCommanders[BombCommanders.Count - 1].timerComponent = (TimerComponent)bombComponent;
                    continue;

				case ComponentTypeEnum.NeedyCapacitor:
				case ComponentTypeEnum.NeedyKnob:
				case ComponentTypeEnum.NeedyVentGas:
				case ComponentTypeEnum.NeedyMod:
					foundComponents = true;
					break;

                default:
	                bombCommander.bombSolvableModules++;
					foundComponents = true;
                    break;
            }

            TwitchComponentHandle handle = Instantiate<TwitchComponentHandle>(twitchComponentHandlePrefab, bombComponent.transform, false);
            handle.bombCommander = bombCommander;
            handle.bombComponent = bombComponent;
            handle.componentType = componentType;
            handle.coroutineQueue = _coroutineQueue;
            handle.leaderboard = leaderboard;
            handle.bombID = _currentBomb == -1 ? -1 : BombCommanders.Count - 1;

            handle.transform.SetParent(bombComponent.transform.parent, true);
            handle.basePosition = handle.transform.localPosition;

            ComponentHandles.Add(handle);
		}

        return foundComponents;
    }

    private IEnumerator SendDelayedMessage(float delay, string message, Action callback = null)
    {
        yield return new WaitForSeconds(delay);
	    IRCConnection.Instance.SendMessage(message);

	    callback?.Invoke();
    }

    private void SendAnalysisLink()
    {
	    if (TwitchPlaysService.logUploader.PostToChat()) return;
	    Debug.Log("[BombMessageResponder] Analysis URL not found, instructing LogUploader to post when it's ready");
	    TwitchPlaysService.logUploader.postOnComplete = true;
    }
    #endregion
}
