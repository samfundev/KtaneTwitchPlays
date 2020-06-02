using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Assets.Scripts.Missions;
using Assets.Scripts.Props;
using UnityEngine;

/// <summary>Encapsulates an ongoing game, with all its bombs, module cameras, HUDs etc.</summary>
public class TwitchGame : MonoBehaviour
{
	public TwitchBomb twitchBombPrefab;
	public TwitchModule twitchModulePrefab;
	public ModuleCameras moduleCamerasPrefab;

	public TwitchPlaysService ParentService;

	public List<TwitchBomb> Bombs = new List<TwitchBomb>();
	public List<TwitchModule> Modules = new List<TwitchModule>();
	public int _currentBomb = -1;
	public readonly Dictionary<int, string> NotesDictionary = new Dictionary<int, string>();
	public Dictionary<string, Dictionary<string, double>> LastClaimedModule = new Dictionary<string, Dictionary<string, double>>();
	public readonly List<CommandQueueItem> CommandQueue = new List<CommandQueueItem>();
	public int callsNeeded = 1;
	public bool VSSetFlag = false;
	public Dictionary<string, string> CallingPlayers = new Dictionary<string, string>();
	public bool callWaiting;
	public string commandToCall = "";
	public CommandQueueItem callSend;
	public SortedDictionary<int, string> VSModePlayers = new SortedDictionary<int, string>();
	public List<string> GoodPlayers = new List<string>();
	public List<string> EvilPlayers = new List<string>();
	public int FindClaimUse = 0;
	public Dictionary<string, int> FindClaimPlayers = new Dictionary<string, int>();
	public bool VoteDetonateAttempted = false;

#pragma warning disable 169
	// ReSharper disable once InconsistentNaming
	private readonly AlarmClock alarmClock;
#pragma warning restore 169

	public static ModuleCameras ModuleCameras;
	public static bool BombActive { get; private set; } = false;
	public static TwitchGame Instance;
	public static bool RetryAllowed = true;

	public static bool EnableDisableInput()
	{
		if (IRCConnection.Instance.State == IRCConnectionState.Connected && !TwitchPlaySettings.data.EnableInteractiveMode && BombActive)
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

	public void SetCurrentBomb()
	{
		if (BombActive)
			_currentBomb = TwitchPlaysService.Instance.CoroutineQueue.CurrentBombID;
	}

	private bool _bombStarted;
	public void OnLightsChange(bool on)
	{
		if (_bombStarted || !on) return;
		_bombStarted = true;

		if (TwitchPlaySettings.data.BombLiveMessageDelay > 0)
		{
			System.Threading.Thread.Sleep(TwitchPlaySettings.data.BombLiveMessageDelay * 1000);
		}

		TwitchPlaysService.Instance.SetHeaderVisbility(true);

		IRCConnection.SendMessage(Bombs.Count == 1
			? TwitchPlaySettings.data.BombLiveMessage
			: TwitchPlaySettings.data.MultiBombLiveMessage);

		StartCoroutine(AutoFillEdgework());
		GameRoom.InitializeGameModes(GameRoom.Instance.InitializeOnLightsOn);
	}

	private void OnEnable()
	{
		Instance = this;
		BombActive = true;
		EnableDisableInput();
		Leaderboard.Instance.ClearSolo();
		LogUploader.Instance.Clear();
		callsNeeded = 1;
		VoteDetonateAttempted = false;
		CallingPlayers.Clear();
		callWaiting = false;
		FindClaimPlayers.Clear();
		MysteryModuleShim.CoveredModules.Clear();
		RetryAllowed = true;

		_bombStarted = false;
		ParentService.GetComponent<KMGameInfo>().OnLightsChange += OnLightsChange;

		StartCoroutine(CheckForBomb());

		FindClaimUse = TwitchPlaySettings.data.FindClaimLimit;
		StartCoroutine(AdjustFindClaimLimit());
		try
		{
			string path = Path.Combine(Application.persistentDataPath, "TwitchPlaysLastClaimed.json");
			LastClaimedModule = SettingsConverter.Deserialize<Dictionary<string, Dictionary<string, double>>>(File.ReadAllText(path));
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "Couldn't read TwitchPlaysLastClaimed.json:");
			LastClaimedModule = new Dictionary<string, Dictionary<string, double>>();
		}
	}

	public string GetBombResult(bool lastBomb = true)
	{
		bool hasDetonated = false;
		bool hasBeenSolved = true;
		float timeStarting = float.MaxValue;
		float timeRemaining = float.MaxValue;
		string timeRemainingFormatted = "";

		foreach (TwitchBomb bomb in Bombs)
		{
			if (bomb == null) continue;

			hasDetonated |= bomb.Bomb.HasDetonated;
			hasBeenSolved &= bomb.IsSolved;
			if (timeRemaining > bomb.CurrentTimer)
			{
				timeStarting = bomb.BombStartingTimer;
				timeRemaining = bomb.CurrentTimer;
			}

			if (!string.IsNullOrEmpty(timeRemainingFormatted))
			{
				timeRemainingFormatted += ", " + bomb.GetFullFormattedTime;
			}
			else
			{
				timeRemainingFormatted = bomb.GetFullFormattedTime;
			}
		}

		string bombMessage = "";
		if (OtherModes.VSModeOn && (hasDetonated || hasBeenSolved))
		{
			OtherModes.Team winner = OtherModes.Team.Good;
			if (OtherModes.GetGoodHealth() == 0)
			{
				winner = OtherModes.Team.Evil;
				bombMessage = TwitchPlaySettings.data.VersusEvilHeader;
			}
			else if (OtherModes.GetEvilHealth() == 0)
			{
				winner = OtherModes.Team.Good;
				bombMessage = TwitchPlaySettings.data.VersusGoodHeader;
			}

			bombMessage += string.Format(TwitchPlaySettings.data.VersusEndMessage,
				winner == OtherModes.Team.Good ? "good" : "evil", timeRemainingFormatted);
			bombMessage += TwitchPlaySettings.GiveBonusPoints();

			if (winner == OtherModes.Team.Good)
				bombMessage += TwitchPlaySettings.data.VersusGoodFooter;
			else if (winner == OtherModes.Team.Evil)
				bombMessage += TwitchPlaySettings.data.VersusEvilFooter;

			if (lastBomb && hasBeenSolved)
				Leaderboard.Instance.Success = true;
		}
		else if (hasDetonated)
		{
			bombMessage = string.Format(TwitchPlaySettings.data.BombExplodedMessage, timeRemainingFormatted);
			Leaderboard.Instance.BombsExploded += Bombs.Count;
			if (!lastBomb)
				return bombMessage;

			Leaderboard.Instance.Success = false;
			TwitchPlaySettings.ClearPlayerLog();
		}
		else if (hasBeenSolved)
		{
			bombMessage = string.Format(TwitchPlaySettings.data.BombDefusedMessage, timeRemainingFormatted);

			Leaderboard.Instance.BombsCleared += Bombs.Count;
			bombMessage += TwitchPlaySettings.GiveBonusPoints();

			if (lastBomb)
			{
				Leaderboard.Instance.Success = true;
			}

			if (Leaderboard.Instance.CurrentSolvers.Count != 1)
				return bombMessage;

			float elapsedTime = timeStarting - timeRemaining;
			string userName = "";
			foreach (string uName in Leaderboard.Instance.CurrentSolvers.Keys)
			{
				userName = uName;
				break;
			}
			if (Leaderboard.Instance.CurrentSolvers[userName] == Leaderboard.RequiredSoloSolves * Bombs.Count && OtherModes.currentMode == TwitchPlaysMode.Normal)
			{
				Leaderboard.Instance.AddSoloClear(userName, elapsedTime, out float previousRecord);
				if (TwitchPlaySettings.data.EnableSoloPlayMode)
				{
					//Still record solo information, should the defuser be the only one to actually defuse a 11 * bomb-count bomb, but display normal leaderboards instead if
					//solo play is disabled.
					TimeSpan elapsedTimeSpan = TimeSpan.FromSeconds(elapsedTime);
					string soloMessage = string.Format(TwitchPlaySettings.data.BombSoloDefusalMessage, Leaderboard.Instance.SoloSolver.UserName, (int) elapsedTimeSpan.TotalMinutes, elapsedTimeSpan.Seconds);
					if (elapsedTime < previousRecord)
					{
						TimeSpan previousTimeSpan = TimeSpan.FromSeconds(previousRecord);
						soloMessage += string.Format(TwitchPlaySettings.data.BombSoloDefusalNewRecordMessage, (int) previousTimeSpan.TotalMinutes, previousTimeSpan.Seconds);
					}
					soloMessage += TwitchPlaySettings.data.BombSoloDefusalFooter;
					ParentService.StartCoroutine(SendDelayedMessage(1.0f, soloMessage));
				}
				else
				{
					Leaderboard.Instance.ClearSolo();
				}
			}
			else
			{
				Leaderboard.Instance.ClearSolo();
			}
		}
		else
		{
			bombMessage = string.Format(TwitchPlaySettings.data.BombAbortedMessage, timeRemainingFormatted);
			Leaderboard.Instance.Success = false;
			TwitchPlaySettings.ClearPlayerLog();
		}
		return bombMessage;
	}

	private void OnDisable()
	{
		GameRoom.ShowCamera();
		BombActive = false;
		EnableDisableInput();
		bool claimsEnabled = TwitchModule.ClaimsEnabled;
		TwitchModule.ClearUnsupportedModules();
		if (!claimsEnabled)
			TwitchModule.ClaimsEnabled = true;
		StopAllCoroutines();
		GoodPlayers.Clear();
		EvilPlayers.Clear();
		VSSetFlag = false;
		Leaderboard.Instance.BombsAttempted++;
		// ReSharper disable once DelegateSubtraction
		ParentService.GetComponent<KMGameInfo>().OnLightsChange -= OnLightsChange;
		TwitchPlaysService.Instance.SetHeaderVisbility(false);

		LogUploader.Instance.GetBombUrl();
		ParentService.StartCoroutine(DelayBombResult());
		if (!claimsEnabled)
			ParentService.StartCoroutine(SendDelayedMessage(1.1f, "Claims have been enabled."));

		if (ModuleCameras != null)
			ModuleCameras.gameObject.SetActive(false);

		// Award users who maintained needy modules.
		Dictionary<string, int> AwardedNeedyPoints = new Dictionary<string, int>();
		foreach (TwitchModule twitchModule in Modules)
		{
			ModuleInformation ModInfo = twitchModule.Solver.ModInfo;
			ScoreMethod scoreMethod = ModInfo.scoreMethod;
			if (scoreMethod == ScoreMethod.Default) continue;

			foreach (var pair in twitchModule.PlayerNeedyStats)
			{
				string playerName = pair.Key;
				var needyStats = pair.Value;

				int points = ((scoreMethod == ScoreMethod.NeedySolves ? needyStats.Solves : needyStats.ActiveTime) * ModInfo.moduleScore * OtherModes.ScoreMultiplier).RoundToInt();
				if (points != 0)
				{
					if (!AwardedNeedyPoints.ContainsKey(playerName))
						AwardedNeedyPoints[playerName] = 0;

					AwardedNeedyPoints[playerName] += points;
					Leaderboard.Instance.AddScore(playerName, points);
				}
			}
		}

		if (AwardedNeedyPoints.Count > 0)
			IRCConnection.SendMessage($"These players have been awarded points for managing a needy: {AwardedNeedyPoints.Select(pair => $"{pair.Key} ({pair.Value})").Join(", ")}");

		GameCommands.unclaimedModules = null;
		DestroyComponentHandles();

		MusicPlayer.StopAllMusic();

		GameRoom.Instance?.OnDisable();

		try
		{
			string path = Path.Combine(Application.persistentDataPath, "TwitchPlaysLastClaimed.json");
			File.WriteAllText(path, SettingsConverter.Serialize(LastClaimedModule));
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "Couldn't write TwitchPlaysLastClaimed.json:");
		}
	}

	// We need to delay the bomb result by one frame so we don't award the solve bonus before the person who solved the last module is added to the Players list.
	public IEnumerator DelayBombResult()
	{
		yield return null;
		ParentService.StartCoroutine(SendDelayedMessage(1.0f, GetBombResult(), SendAnalysisLink));

		foreach (var bomb in Bombs.Where(x => x != null))
			Destroy(bomb.gameObject, 2.0f);
		Bombs.Clear();
	}

	public void DestroyComponentHandles()
	{
		if (Modules == null) return;

		foreach (TwitchModule handle in Modules)
		{
			if (handle != null)
			{
				handle.ClaimQueue.Clear(); // Prevent any claims from going through.
				Destroy(handle.gameObject, 2.0f);
			}
		}
		Modules.Clear();
	}

	public void CallUpdate(bool response)
	{
		var callResponse = CheckIfCall(true, false, "", commandToCall, out _);
		if (callResponse == CallResponse.Success)
			GameCommands.CallQueuedCommand("", false, true, commandToCall);
		else if (callWaiting)
			IRCConnection.SendMessageFormat("Waiting for {0} to be queued.", string.IsNullOrEmpty(commandToCall) ? "the next unnamed queued command" : commandToCall.StartsWith("!") ? "module " + commandToCall : "the command named “" + commandToCall + "”");
		else if (response)
			GameCommands.CallCountCommand();
	}

	public enum CallResponse
	{
		Success,
		AlreadyCalled,
		NotEnoughCalls,
		UncommonCalls,
		DifferentName,
		NotPresent
	}

	public CallResponse CheckIfCall(bool check, bool now, string user, string name, out bool callChanged)
	{
		/*	THIS IS THE ORDERING OF THE CALL CHECKING SYSTEM
		 *	1. Prevent user from being added / add them if necessary*
		 *	2. Check if enough calls were made**
		 *	3. Remove all empty sets and check if calls are all now common. This will also set the correct call to be made if necessary**
		 *	4. Make sure that the correct call exists in the queue. If not, set it to be made when possible
		 * 
		 *  * This section is skipped if "bool check" or "bool now" is true
		 *  ** These sections are skipped if "bool now" is true
		 */

		callChanged = false;
		if (!now)
		{
			//section 1 start
			if (!check)
			{
				if (CallingPlayers.Keys.Contains(user))
				{
					if (name == CallingPlayers[user])
						return CallResponse.AlreadyCalled;

					callChanged = true;
					CallingPlayers.Remove(user);
				}
				CallingPlayers.Add(user, name);
			}

			//section 2 start
			if (callsNeeded > CallingPlayers.Count)
				return CallResponse.NotEnoughCalls;

			//section 3 start
			string[] _calls = CallingPlayers.Values.Where(x => x != "").ToArray();
			if (_calls.Length != 0)
			{
				for (int i = 0; i < _calls.Length; i++)
				{
					if (!_calls[0].EqualsIgnoreCase(_calls[i]))
						return CallResponse.UncommonCalls;
				}
				name = _calls[0];
			}
		}
		commandToCall = name;

		//section 4 start
		CommandQueueItem call = null;
		if (string.IsNullOrEmpty(name)) //call any unnamed command
		{
			call = CommandQueue.Find(item => item.Name == null);
		}
		else if (name.StartsWith("!")) //call a specific module
		{
			name += ' ';
			call = CommandQueue.Find(item => item.Message.Text.StartsWith(name) && item.Name == null);
			if (call == null)
			{
				call = CommandQueue.Find(item => item.Message.Text.StartsWith(name));
				if (call != null)
					return CallResponse.DifferentName;
			}
		}
		else //call a named command
		{
			call = CommandQueue.Find(item => name.EqualsIgnoreCase(item.Name));
		}

		if (call == null)
			return CallResponse.NotPresent;

		callSend = call;
		return CallResponse.Success;
	}

	public void SendCallResponse(string user, string name, CallResponse response, bool callChanged)
	{
		bool unnamed = string.IsNullOrEmpty(name);
		if (response == CallResponse.AlreadyCalled)
		{
			IRCConnection.SendMessageFormat("@{0}, you already called!", user);
			return;
		}
		else if (response == CallResponse.NotEnoughCalls)
		{
			if (callChanged)
				IRCConnection.SendMessageFormat("@{0}, your call has been updated to {1}.", user, unnamed ? "the next queued command" : name);
			GameCommands.CallCountCommand();
			return;
		}
		else if (response == CallResponse.UncommonCalls)
		{
			if (callChanged)
				IRCConnection.SendMessageFormat("@{0}, your call has been updated to {1}. Uncommon calls still present.", user, unnamed ? "the next queued command" : name);
			else
			{
				IRCConnection.SendMessageFormat("Sorry, uncommon calls were made. Please either correct your call(s) or use “!callnow” followed by the correct command to call.");
				GameCommands.ListCalledPlayers();
			}
			return;
		}
		else if (response == CallResponse.DifferentName)
		{
			CommandQueueItem call = CommandQueue.Find(item => item.Message.Text.StartsWith(name));
			IRCConnection.SendMessageFormat("@{0}, module {1} is queued with the name “{2}”, please use “!call {2}” to call it.", user, name, call.Name);
			return;
		}
		else
		{
			unnamed = string.IsNullOrEmpty(commandToCall);
			if (callWaiting)
				IRCConnection.SendMessageFormat("Waiting for {0} to be queued.", unnamed ? "the next unnamed queued command" : commandToCall.StartsWith("!") ? "module " + commandToCall : "the command named “" + commandToCall + "”");
			else
			{
				IRCConnection.SendMessageFormat("No {0} in the queue. Calling {1} when it is queued.", unnamed ? "unnamed commands" : commandToCall.StartsWith("!") ? "command for module " + commandToCall : "command named “" + commandToCall + "”", unnamed ? "the next unnamed queued command" : commandToCall.StartsWith("!") ? "module " + commandToCall : "the command named “" + commandToCall + "”");
				callWaiting = true;
			}
		}
	}
	#region Protected/Private Methods

	private IEnumerator AutoFillEdgework()
	{
		while (BombActive)
		{
			if (TwitchPlaySettings.data.EnableAutomaticEdgework)
				foreach (TwitchBomb bomb in Bombs)
					bomb.FillEdgework();
			yield return new WaitForSeconds(0.1f);
		}
	}

	private IEnumerator CheckForBomb()
	{
		yield return new WaitUntil(() => SceneManager.Instance.GameplayState.Bombs != null && SceneManager.Instance.GameplayState.Bombs.Count > 0);
		yield return null;
		var bombs = SceneManager.Instance.GameplayState.Bombs;

		try
		{
			ModuleCameras = Instantiate(moduleCamerasPrefab);
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "Failed to instantiate the module camera system due to an exception:");
			ModuleCameras = null;
		}

		if (GameRoom.GameRoomTypes.Where((t, i) => t() != null && GameRoom.CreateRooms[i](FindObjectsOfType(t()), out GameRoom.Instance)).Any())
		{
			GameRoom.Instance.InitializeBombs(bombs);
		}
		ModuleCameras?.ChangeBomb(Bombs[0]);

		try
		{
			GameRoom.Instance.InitializeBombNames();
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "An exception has occurred while setting the bomb names");
		}
		StartCoroutine(GameRoom.Instance.ReportBombStatus());
		StartCoroutine(GameRoom.Instance.InterruptLights());

		try
		{
			if (GameRoom.Instance.HoldBomb)
				StartCoroutine(BombCommands.Hold(Bombs[0]));
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "An exception has occurred attempting to hold the bomb.");
		}

		NotesDictionary.Clear();
		CommandQueue.Clear();
		ModuleCameras?.SetNotes();

		if (EnableDisableInput())
		{
			TwitchModule.SolveUnsupportedModules(true);
		}

		// Set up some stuff for the !unclaimed command.
		GameCommands.unclaimedModules = Modules.Where(h => h.CanBeClaimed).Shuffle().ToList();
		GameCommands.unclaimedModuleIndex = 0;

		while (OtherModes.Unexplodable)
		{
			foreach (var bomb in Bombs)
				if (bomb.Bomb.GetTimer() != null && bomb.Bomb.GetTimer().GetRate() > 0)
					bomb.Bomb.GetTimer().SetRateModifier(-bomb.Bomb.GetTimer().GetRate());
			yield return null;
		}

		TwitchPlaysService.Instance.UpdateUiHue();
	}

	internal void InitializeModuleCodes()
	{
		// This method assigns a unique code to each module.

		if (TwitchPlaySettings.data.EnableLetterCodes)
		{
			// Ignore initial “the” in module names
			static string SanitizedName(TwitchModule handle) => Regex.Replace(handle.BombComponent.GetModuleDisplayName(), @"^the\s+", "", RegexOptions.IgnoreCase);

			// First, assign codes “naively”
			var dic1 = new Dictionary<string, List<TwitchModule>>();
			var numeric = 0;
			foreach (var handle in Modules)
			{
				if (handle.BombComponent == null || handle.BombComponent.ComponentType == ComponentTypeEnum.Timer || handle.BombComponent.ComponentType == ComponentTypeEnum.Empty)
					continue;

				string moduleName = SanitizedName(handle);
				if (moduleName != null)
				{
					string code = moduleName.Where(ch => (ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z' && ch != 'O')).Take(2).Join("");
					if (code.Length < 2 && moduleName.Length >= 2)
						code = moduleName.Where(char.IsLetterOrDigit).Take(2).Join("").ToUpperInvariant();
					if (code.Length == 0)
						code = (++numeric).ToString();
					handle.Code = code;
					dic1.AddSafe(code, handle);
				}
				else
				{
					handle.Code = (++numeric).ToString();
					dic1.AddSafe(handle.Code, handle);
				}
			}

			// If this assignment succeeded in generating unique codes, use it
			if (dic1.Values.All(list => list.Count < 2))
				return;

			// See if we can make them all unique by just changing some non-unique ones to different letters in the module name
			var dic2 = dic1.Where(kvp => kvp.Value.Count < 2).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
			foreach (var kvp in dic1)
			{
				if (kvp.Value.Count < 2)
					continue;

				dic2.AddSafe(kvp.Key, kvp.Value[0]);
				for (int i = 1; i < kvp.Value.Count; i++)
				{
					var moduleName = SanitizedName(kvp.Value[i]);
					for (int chIx = 1; chIx < moduleName.Length; chIx++)
					{
						string newCode = (moduleName[0] + "" + moduleName[chIx]).ToUpperInvariant();
						if (moduleName[chIx] == 'O' || !char.IsLetter(moduleName[chIx]) || dic2.ContainsKey(newCode))
							continue;

						kvp.Value[i].Code = newCode;
						dic2.AddSafe(newCode, kvp.Value[i]);
						goto processed;
					}
					dic2.AddSafe(kvp.Key, kvp.Value[i]);
					processed:;
				}
			}

			// If this assignment succeeded in generating unique codes, use it
			if (dic2.Values.All(list => list.Count < 2))
				return;

			var globalNumber = 1;

			// If still no success, gonna have to use numbers
			while (true)
			{
				var tooMany = dic2.FirstOrDefault(kvp => kvp.Value.Count > 1);
				// We did it — all unique
				if (tooMany.Key == null)
					break;

				// Find other non-unique modules with the same first letter
				var all = dic2.Where(kvp => kvp.Key[0] == tooMany.Key[0] && kvp.Value.Count > 1).SelectMany(kvp => kvp.Value.Skip(1)).ToList();
				var number = 1;
				foreach (TwitchModule module in all)
				{
					dic2[module.Code].Remove(module);
					while (dic2.ContainsKey(module.Code[0] + number.ToString()))
						number++;
					if (number < 10)
						module.Code = module.Code[0] + (number++).ToString();
					else
					{
						while (dic2.ContainsKey(globalNumber.ToString()))
							globalNumber++;
						module.Code = (globalNumber++).ToString();
					}
					dic2.AddSafe(module.Code, module);
				}
			}
		}
		else
		{
			int num = 1;
			foreach (var handle in Modules)
				handle.Code = num++.ToString();
		}
	}

	public void SetBomb(Bomb bomb, int id)
	{
		if (Bombs.Count == 0)
			_currentBomb = id == -1 ? -1 : 0;
		var tb = CreateBombHandleForBomb(bomb, id);
		Bombs.Add(tb);
		CreateComponentHandlesForBomb(tb);
	}

	private TwitchBomb CreateBombHandleForBomb(Bomb bomb, int id)
	{
		TwitchBomb twitchBomb = Instantiate(twitchBombPrefab);
		twitchBomb.Bomb = bomb;
		twitchBomb.BombID = id;
		twitchBomb.BombTimeStamp = DateTime.Now;
		twitchBomb.BombStartingTimer = bomb.GetTimer().TimeRemaining;
		return twitchBomb;
	}

	public void CreateComponentHandlesForBomb(TwitchBomb bomb)
	{
		foreach (var component in bomb.Bomb.BombComponents)
		{
			if (component.ComponentType.EqualsAny(ComponentTypeEnum.Empty, ComponentTypeEnum.Timer))
				continue;

			TwitchModule module = Instantiate(twitchModulePrefab, component.transform, false);
			module.Bomb = bomb;
			module.BombComponent = component;
			module.BombID = _currentBomb == -1 ? -1 : Bombs.Count - 1;

			module.transform.SetParent(component.transform.parent, true);
			module.BasePosition = module.transform.localPosition;

			Modules.Add(module);
		}
	}

	public double? GetLastClaimedTime(string moduleID, string userNickName)
	{
		if (LastClaimedModule == null)
			LastClaimedModule = new Dictionary<string, Dictionary<string, double>>();

		if (!LastClaimedModule.TryGetValue(moduleID, out var lastClaimedTimes) || lastClaimedTimes == null)
			lastClaimedTimes = LastClaimedModule[moduleID] = new Dictionary<string, double>();

		return lastClaimedTimes.TryGetValue(userNickName, out var time) ? time : (double?) null;
	}

	public void SetLastClaimedTime(string moduleID, string userNickName, double timestamp)
	{
		// Ensures that the relevant dictionaries exist
		GetLastClaimedTime(moduleID, userNickName);
		LastClaimedModule[moduleID][userNickName] = timestamp;
	}

	private static IEnumerator SendDelayedMessage(float delay, string message, Action callback = null)
	{
		yield return new WaitForSeconds(delay);
		IRCConnection.SendMessage(message);

		callback?.Invoke();
	}

	private IEnumerator AdjustFindClaimLimit()
	{
		if (TwitchPlaySettings.data.FindClaimAddTime < 1) yield break;

		var _time = TwitchPlaySettings.data.FindClaimAddTime * 60;
		while (true)
		{
			yield return new WaitForSeconds(_time);
			FindClaimUse++;
		}
	}

	private void SendAnalysisLink()
	{
		if (LogUploader.Instance.previousUrl != null)
			LogUploader.PostToChat(LogUploader.Instance.previousUrl);
		else
			LogUploader.Instance.postOnComplete = true;
	}

	public static bool IsAuthorizedDefuser(string userNickName, bool isWhisper, bool silent = false)
	{
		if (userNickName.EqualsAny("Bomb Factory", TwitchPlaySettings.data.TwitchPlaysDebugUsername) || Instance.Bombs.Any(x => x.BombName == userNickName))
			return true;

		bool result = !TwitchPlaySettings.data.EnableWhiteList || UserAccess.HasAccess(userNickName, AccessLevel.Defuser, true);
		if (!result && !silent)
			IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.TwitchPlaysDisabled, userNickName), userNickName, !isWhisper);

		return result;
	}

	public void StopCommands()
	{
		CoroutineCanceller.SetCancel();
		TwitchPlaysService.Instance.CoroutineQueue.CancelFutureSubcoroutines();
		SetCurrentBomb();
	}

	private static readonly string[] solveBased = {
		"MemoryV2", "SouvenirModule", "TurnTheKeyAdvanced", "HexiEvilFMN", "simonsStages", "forgetThemAll",
		"tallorderedKeys", "forgetEnigma", "forgetUsNot", "qkForgetPerspective", "organizationModule", "ForgetMeNow"
	};
	private bool removedSolveBasedModules = false;
	public void RemoveSolveBasedModules()
	{
		if (removedSolveBasedModules) return;
		removedSolveBasedModules = true;

		foreach (var module in Modules.Where(x => !x.Solved && solveBased.Contains(x.BombComponent.GetModuleDisplayName())))
		{
			ComponentSolver.HandleForcedSolve(module);

			module.Unsupported = true;
			if (module.Solver != null)
				module.Solver.UnsupportedModule = true;
		}
	}

	#endregion
}
