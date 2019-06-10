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

#pragma warning disable 169
	// ReSharper disable once InconsistentNaming
	private readonly AlarmClock alarmClock;
#pragma warning restore 169

	public static ModuleCameras ModuleCameras;
	public static bool BombActive { get; private set; } = false;
	public static TwitchGame Instance;

	public static bool EnableDisableInput()
	{
		if (IRCConnection.Instance.State == IRCConnectionState.Connected && TwitchPlaySettings.data.EnableTwitchPlaysMode && !TwitchPlaySettings.data.EnableInteractiveMode && BombActive)
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

		_bombStarted = false;
		ParentService.GetComponent<KMGameInfo>().OnLightsChange += OnLightsChange;

		StartCoroutine(CheckForBomb());
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

		string bombMessage;
		if (hasDetonated)
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
			if (!OtherModes.VSModeOn)
				bombMessage = string.Format(TwitchPlaySettings.data.BombDefusedMessage, timeRemainingFormatted);
			else
			{
				OtherModes.Team winner = OtherModes.Team.Good;
				if (OtherModes.GetGoodHealth() == 0)
					winner = OtherModes.Team.Evil;
				else if (OtherModes.GetEvilHealth() == 0)
					winner = OtherModes.Team.Good;
				bombMessage = string.Format(TwitchPlaySettings.data.BombDefusedVsMessage,
					winner == OtherModes.Team.Good ? "good" : "evil", timeRemainingFormatted);
			}

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
			if (Leaderboard.Instance.CurrentSolvers[userName] == (Leaderboard.RequiredSoloSolves * Bombs.Count))
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
				Destroy(handle.gameObject, 2.0f);
		}
		Modules.Clear();
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
				TwitchPlaysService.Instance.CoroutineQueue.AddToQueue(BombCommands.Hold(Bombs[0]));
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "An exception has occurred attempting to hold the bomb.");
		}

		for (int i = 0; i < 4; i++)
		{
			NotesDictionary[i] = (OtherModes.ZenModeOn && i == 3) ? TwitchPlaySettings.data.ZenModeFreeSpace : TwitchPlaySettings.data.NotesSpaceFree;
			ModuleCameras?.SetNotes(i, NotesDictionary[i]);
		}

		if (EnableDisableInput())
		{
			TwitchModule.SolveUnsupportedModules(true);
		}

		while (OtherModes.ZenModeOn)
		{
			foreach (var bomb in Bombs)
				if (bomb.Bomb.GetTimer() != null && bomb.Bomb.GetTimer().GetRate() > 0)
					bomb.Bomb.GetTimer().SetRateModifier(-bomb.Bomb.GetTimer().GetRate());
			yield return null;
		}
	}

	internal void InitializeModuleCodes()
	{
		// This method assigns a unique code to each module.

		if (TwitchPlaySettings.data.EnableLetterCodes)
		{
			// Ignore initial “the” in module names
			string SanitizedName(TwitchModule handle) => Regex.Replace(handle.BombComponent.GetModuleDisplayName(), @"^the\s+", "", RegexOptions.IgnoreCase);

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

		foreach (var handle in Modules.Where(c => c.IsKey))
		{
			var moduleName = handle.BombComponent.GetModuleDisplayName();
			IRCConnection.SendMessage($"Module {handle.Code} {(moduleName.EqualsAny("The Swan", "The Time Keeper") ? "is" : "is a")} {moduleName}");
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
		string[] keyModules =
		{
			"SouvenirModule", "MemoryV2", "TurnTheKey", "TurnTheKeyAdvanced", "theSwan", "HexiEvilFMN", "taxReturns", "timeKeeper", "cookieJars",
			"DividedSquaresModule", "forgetThis", "forgetInfinity"
		};

		foreach (var component in bomb.Bomb.BombComponents)
		{
			var componentType = component.ComponentType;
			bool keyModule = false;
			string moduleName;

			// ReSharper disable once SwitchStatementMissingSomeCases
			switch (componentType)
			{
				case ComponentTypeEnum.Empty:
				case ComponentTypeEnum.Timer:
					continue;

				case ComponentTypeEnum.NeedyCapacitor:
				case ComponentTypeEnum.NeedyKnob:
				case ComponentTypeEnum.NeedyVentGas:
				case ComponentTypeEnum.NeedyMod:
					moduleName = component.GetModuleDisplayName();
					keyModule = true;
					break;

				case ComponentTypeEnum.Mod:
					keyModule = keyModules.Contains(component.GetComponent<KMBombModule>().ModuleType);
					goto default;

				default:
					moduleName = component.GetModuleDisplayName();
					break;
			}

			TwitchModule module = Instantiate(twitchModulePrefab, component.transform, false);
			module.Bomb = bomb;
			module.BombComponent = component;
			module.BombID = _currentBomb == -1 ? -1 : Bombs.Count - 1;
			module.IsKey = keyModule;

			module.transform.SetParent(component.transform.parent, true);
			module.BasePosition = module.transform.localPosition;

			Modules.Add(module);
		}
	}

	private IEnumerator SendDelayedMessage(float delay, string message, Action callback = null)
	{
		yield return new WaitForSeconds(delay);
		IRCConnection.SendMessage(message);

		callback?.Invoke();
	}

	private void SendAnalysisLink()
	{
		if (LogUploader.Instance.previousUrl != null)
			LogUploader.Instance.PostToChat(LogUploader.Instance.previousUrl);
		else
			LogUploader.Instance.postOnComplete = true;
	}

	public static bool IsAuthorizedDefuser(string userNickName, bool isWhisper, bool silent = false)
	{
		if (userNickName.EqualsAny("Bomb Factory", TwitchPlaySettings.data.TwitchPlaysDebugUsername) || TwitchGame.Instance.Bombs.Any(x => x.BombName == userNickName))
			return true;
		BanData ban = UserAccess.IsBanned(userNickName);
		if (ban != null)
		{
			if (silent) return false;

			if (double.IsPositiveInfinity(ban.BanExpiry))
			{
				IRCConnection.SendMessage($"Sorry @{userNickName}, You were banned permanently from Twitch Plays by {ban.BannedBy}{(string.IsNullOrEmpty(ban.BannedReason) ? "." : $", for the following reason: {ban.BannedReason}")}", userNickName, !isWhisper);
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

				IRCConnection.SendMessage($"Sorry @{userNickName}, You were timed out from Twitch Plays by {ban.BannedBy}{(string.IsNullOrEmpty(ban.BannedReason) ? "." : $", For the following reason: {ban.BannedReason}")} You can participate again in {timeRemaining}", userNickName, !isWhisper);
			}
			return false;
		}

		bool result = (TwitchPlaySettings.data.EnableTwitchPlaysMode || UserAccess.HasAccess(userNickName, AccessLevel.Defuser, true));
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

	private static string[] solveBased = new[] { "MemoryV2", "SouvenirModule", "TurnTheKeyAdvanced", "HexiEvilFMN" };
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
