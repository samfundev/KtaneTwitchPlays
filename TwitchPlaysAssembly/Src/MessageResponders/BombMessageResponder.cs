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
				_ircConnection.SendMessage(TwitchPlaySettings.data.BombLiveMessage);
			}
			else
			{
				_ircConnection.SendMessage(TwitchPlaySettings.data.MultiBombLiveMessage);
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
				_currentBomb = GameRoom.Instance.InitializeBombs(bombs);
				break;
			}
		}

		GameRoom.Instance.InitializeBombNames();
		StartCoroutine(GameRoom.Instance.ReportBombStatus());

		if (GameRoom.Instance.HoldBomb)
			_coroutineQueue.AddToQueue(BombHandles[0].OnMessageReceived(BombHandles[0].nameText.text, "red", "bomb hold"), _currentBomb);

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
	}

	public void SetBomb(Bomb bomb, int id)
    {
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
            _ircConnection.SendMessage(TwitchPlaySettings.data.Notes, index+1, _notes[index]);
            return;
        }

        if (text.StartsWith("notes1 ", StringComparison.InvariantCultureIgnoreCase) || text.StartsWith("notes2 ", StringComparison.InvariantCultureIgnoreCase) ||
            text.StartsWith("notes3 ", StringComparison.InvariantCultureIgnoreCase) || text.StartsWith("notes4 ", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!IsAuthorizedDefuser(userNickName)) return;
            int index = "1234".IndexOf(text.Substring(5, 1), StringComparison.Ordinal);
            string notes = text.Substring(7);
            if (notes == "") return;

            _ircConnection.SendMessage(TwitchPlaySettings.data.NotesTaken, index+1 , notes);

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

            _ircConnection.SendMessage(TwitchPlaySettings.data.NotesAppended, index + 1, notes);

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
            _ircConnection.SendMessage(TwitchPlaySettings.data.NoteSlotCleared, index + 1);

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
            List<string> claimed = (from handle in ComponentHandles where handle.PlayerName != null && handle.PlayerName.Equals(userNickName, StringComparison.InvariantCultureIgnoreCase) && !handle.Solved select string.Format(TwitchPlaySettings.data.OwnedModule, handle.idText.text.Replace("!", ""), handle.headerText.text)).ToList();
            if (claimed.Count > 0)
            {
                string message = string.Format(TwitchPlaySettings.data.OwnedModuleList, userNickName, string.Join(", ", claimed.ToArray(), 0, Math.Min(claimed.Count, 5)));
                if (claimed.Count > 5)
                    message += "...";
                _ircConnection.SendMessage(message);
            }
            else
                _ircConnection.SendMessage(TwitchPlaySettings.data.NoOwnedModules, userNickName);
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

        

		if (text.Equals("unclaimed", StringComparison.InvariantCultureIgnoreCase))
		{
			if (!IsAuthorizedDefuser(userNickName)) return;

			IEnumerable<string> unclaimed = ComponentHandles.Where(handle => !handle.claimed && !handle.Solved && GameRoom.Instance.IsCurrentBomb(handle.bombID)).Shuffle().Take(3)
				.Select(handle => string.Format("{0} ({1})", handle.headerText.text, handle.Code)).ToList();
			
			if (unclaimed.Any()) _ircConnection.SendMessage("Unclaimed Modules: {0}", unclaimed.Join(", "));
			else _ircConnection.SendMessage("There are no more unclaimed modules.");

			return;
		}

		if (text.StartsWith("find ", StringComparison.InvariantCultureIgnoreCase) || text.StartsWith("search ", StringComparison.InvariantCultureIgnoreCase))
		{
			if (!IsAuthorizedDefuser(userNickName)) return;

			var split = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			string query = split.Skip(1).Join(" ");
			IEnumerable<string> modules = ComponentHandles.Where(handle => handle.headerText.text.ContainsIgnoreCase(query) && GameRoom.Instance.IsCurrentBomb(handle.bombID))
                .OrderByDescending(handle => handle.headerText.text.EqualsIgnoreCase(query)).ThenBy(handle => handle.Solved).ThenBy(handle => handle.PlayerName != null).Take(3)
				.Select(handle => string.Format("{0} ({1}) - {2}", handle.headerText.text, handle.Code, 
					handle.Solved ? "Solved" : (handle.PlayerName == null ? "Unclaimed" : "Claimed by " + handle.PlayerName)
				)).ToList();

			if (modules.Any()) _ircConnection.SendMessage("Modules: {0}", modules.Join(", "));
			else _ircConnection.SendMessage("Couldn't find any modules containing \"{0}\".", query);

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

	    if (TwitchPlaySettings.data.BombCustomMessages.ContainsKey(text.ToLowerInvariant()))
	    {
		    _ircConnection.SendMessage(TwitchPlaySettings.data.BombCustomMessages[text.ToLowerInvariant()]);
	    }
    }

    private void CreateBombHandleForBomb(MonoBehaviour bomb, int id)
    {
        TwitchBombHandle _bombHandle = Instantiate<TwitchBombHandle>(twitchBombHandlePrefab);
        _bombHandle.bombID = id;
        _bombHandle.ircConnection = _ircConnection;
        _bombHandle.bombCommander = BombCommanders[BombCommanders.Count-1];
        _bombHandle.coroutineQueue = _coroutineQueue;
        _bombHandle.coroutineCanceller = _coroutineCanceller;
        BombHandles.Add(_bombHandle);
        BombCommanders[BombCommanders.Count - 1].twitchBombHandle = _bombHandle;
    }

    public bool CreateComponentHandlesForBomb(Bomb bomb)
    {
        bool foundComponents = false;

        List<BombComponent> bombComponents = bomb.BombComponents;

		var bombCommander = BombCommanders[BombCommanders.Count - 1];
		if (bombComponents.Count > 12 || TwitchPlaySettings.data.ForceMultiDeckerMode)
        {
			bombCommander.multiDecker = true;
        }

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
            handle.ircConnection = _ircConnection;
            handle.bombCommander = bombCommander;
            handle.bombComponent = bombComponent;
            handle.componentType = componentType;
            handle.coroutineQueue = _coroutineQueue;
            handle.coroutineCanceller = _coroutineCanceller;
            handle.leaderboard = leaderboard;
            handle.bombID = _currentBomb == -1 ? -1 : BombCommanders.Count - 1;

            Vector3 idealOffset = handle.transform.TransformDirection(GetIdealPositionForHandle(handle, bombComponents, out handle.direction));
            handle.transform.SetParent(bombComponent.transform.parent, true);
            handle.basePosition = handle.transform.localPosition;
            handle.idealHandlePositionOffset = bombComponent.transform.parent.InverseTransformDirection(idealOffset);

            ComponentHandles.Add(handle);
		}

        return foundComponents;
    }

    private Vector3 GetIdealPositionForHandle(TwitchComponentHandle thisHandle, IList bombComponents, out TwitchComponentHandle.Direction direction)
    {
        Rect handleBasicRect = new Rect(-0.155f, -0.1f, 0.31f, 0.2f);
        Rect bombComponentBasicRect = new Rect(-0.1f, -0.1f, 0.2f, 0.2f);

        float baseUp = (handleBasicRect.height + bombComponentBasicRect.height) * 0.55f;
        float baseRight = (handleBasicRect.width + bombComponentBasicRect.width) * 0.55f;

        Vector2 extentUp = new Vector2(0.0f, baseUp * 0.1f);
        Vector2 extentRight = new Vector2(baseRight * 0.2f, 0.0f);

        Vector2 extentResult = Vector2.zero;

        while (true)
        {
            Rect handleRect = handleBasicRect;
            handleRect.position += extentRight;
            if (!HasOverlap(thisHandle, handleRect, bombComponentBasicRect, bombComponents))
            {
                extentResult = extentRight;
                direction = TwitchComponentHandle.Direction.Left;
                break;
            }

            handleRect = handleBasicRect;
            handleRect.position -= extentRight;
            if (!HasOverlap(thisHandle, handleRect, bombComponentBasicRect, bombComponents))
            {
                extentResult = -extentRight;
                direction = TwitchComponentHandle.Direction.Right;
                break;
            }

            handleRect = handleBasicRect;
            handleRect.position += extentUp;
            if (!HasOverlap(thisHandle, handleRect, bombComponentBasicRect, bombComponents))
            {
                extentResult = extentUp;
                direction = TwitchComponentHandle.Direction.Down;
                break;
            }

            handleRect = handleBasicRect;
            handleRect.position -= extentUp;
            if (!HasOverlap(thisHandle, handleRect, bombComponentBasicRect, bombComponents))
            {
                extentResult = -extentUp;
                direction = TwitchComponentHandle.Direction.Up;
                break;
            }

            extentUp.y += baseUp * 0.1f;
            extentRight.x += baseRight * 0.1f;
        }

        return new Vector3(extentResult.x, 0.0f, extentResult.y);
    }

    private bool HasOverlap(TwitchComponentHandle thisHandle, Rect handleRect, Rect bombComponentBasicRect, IList bombComponents)
    {
        foreach (BombComponent bombComponent in bombComponents)
        {
            Vector3 bombComponentCenter = thisHandle.transform.InverseTransformPoint(bombComponent.transform.position);

            Rect bombComponentRect = bombComponentBasicRect;
            bombComponentRect.position += new Vector2(bombComponentCenter.x, bombComponentCenter.z);

            if (bombComponentRect.Overlaps(handleRect))
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator SendDelayedMessage(float delay, string message, Action callback = null)
    {
        yield return new WaitForSeconds(delay);
        _ircConnection.SendMessage(message);

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
