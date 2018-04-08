using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

	public TwitchPlaysService parentService = null;

	public List<BombCommander> BombCommanders = new List<BombCommander>();
	public List<TwitchBombHandle> BombHandles = new List<TwitchBombHandle>();
	public List<TwitchComponentHandle> ComponentHandles = new List<TwitchComponentHandle>();
	private int _currentBomb = -1;
	private Dictionary<int, string> _notesDictionary = new Dictionary<int, string>();

#pragma warning disable 169
	private AlarmClock alarmClock;
#pragma warning restore 169

	public static ModuleCameras moduleCameras = null;

	public static bool BombActive { get; private set; }

	public static BombMessageResponder Instance = null;

	public Dictionary<string, Dictionary<string, double>> LastClaimedModule = new Dictionary<string, Dictionary<string, double>>();

	static BombMessageResponder()
	{
		BombActive = false;
	}

	#region Unity Lifecycle

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
		if (!BombActive) return;
		_currentBomb = _coroutineQueue.CurrentBombID;
	}

	public void DropCurrentBomb()
	{
		if (!BombActive) return;
		_coroutineQueue.AddToQueue(BombCommanders[_currentBomb != -1 ? _currentBomb : 0].LetGoBomb(), _currentBomb);
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

		IRCConnection.Instance.SendMessage(BombCommanders.Count == 1
			? TwitchPlaySettings.data.BombLiveMessage
			: TwitchPlaySettings.data.MultiBombLiveMessage);

		if (TwitchPlaySettings.data.EnableAutomaticEdgework) foreach (var commander in BombCommanders) commander.FillEdgework(commander.twitchBombHandle.bombID != _currentBomb);
		GameRoom.Instance.InitializeGameModes(GameRoom.Instance.InitializeOnLightsOn);
	}

	private void OnEnable()
	{
		Instance = this;
		BombActive = true;
		EnableDisableInput();
		Leaderboard.Instance.ClearSolo();
		LogUploader.Instance.Clear();

		_bombStarted = false;
		parentService.GetComponent<KMGameInfo>().OnLightsChange += OnLightsChange;

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
			Leaderboard.Instance.BombsExploded += BombCommanders.Count;
			if (lastBomb)
			{
				Leaderboard.Instance.Success = false;
				TwitchPlaySettings.ClearPlayerLog();
			}
		}
		else if (HasBeenSolved)
		{
			bombMessage = string.Format(TwitchPlaySettings.data.BombDefusedMessage, timeRemainingFormatted);
			Leaderboard.Instance.BombsCleared += BombCommanders.Count;
			bombMessage += TwitchPlaySettings.GiveBonusPoints();

			if (lastBomb)
			{
				Leaderboard.Instance.Success = true;
			}

			if (Leaderboard.Instance.CurrentSolvers.Count == 1)
			{
				float previousRecord = 0.0f;
				float elapsedTime = timeStarting - timeRemaining;
				string userName = "";
				foreach (string uName in Leaderboard.Instance.CurrentSolvers.Keys)
				{
					userName = uName;
					break;
				}
				if (Leaderboard.Instance.CurrentSolvers[userName] == (Leaderboard.RequiredSoloSolves * BombCommanders.Count))
				{
					Leaderboard.Instance.AddSoloClear(userName, elapsedTime, out previousRecord);
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
						parentService.StartCoroutine(SendDelayedMessage(1.0f, soloMessage));
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
		_hideBombs = false;
		BombActive = false;
		EnableDisableInput();
		TwitchComponentHandle.ClaimedList.Clear();
		TwitchComponentHandle.ClearUnsupportedModules();
		StopAllCoroutines();
		Leaderboard.Instance.BombsAttempted++;
		parentService.GetComponent<KMGameInfo>().OnLightsChange -= OnLightsChange;

		LogUploader.Instance.Post();
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

		GameRoom.Instance?.OnDisable();
		OtherModes.RefreshModes();

		try
		{
			string path = Path.Combine(Application.persistentDataPath, "TwitchPlaysLastClaimed.json");
			File.WriteAllText(path, SettingsConverter.Serialize(LastClaimedModule));
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "Couldn't Write TwitchPlaysLastClaimed.json:");
		}
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

		InitializeModuleCodes();

		foreach (var handle in ComponentHandles.Where(c => c.IsKey))
		{
			var moduleName = handle.bombComponent.GetModuleDisplayName();
			if (moduleName == "The Swan")
				IRCConnection.Instance.SendMessage($"Module {handle.Code} is {moduleName}");
			else
				IRCConnection.Instance.SendMessage($"Module {handle.Code} is a {moduleName}");
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

		try
		{
			moduleCameras = Instantiate<ModuleCameras>(moduleCamerasPrefab);
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "Failed to Instantiate the module camera system due to an exception:");
			moduleCameras = null;
		}
		moduleCameras?.ChangeBomb(BombCommanders[0]);

		for (int i = 0; i < 4; i++)
		{
			_notesDictionary[i] = (OtherModes.ZenModeOn && i == 3) ? TwitchPlaySettings.data.ZenModeFreeSpace : TwitchPlaySettings.data.NotesSpaceFree;
			moduleCameras?.SetNotes(i, _notesDictionary[i]);
		}

		if (EnableDisableInput())
		{
			TwitchComponentHandle.SolveUnsupportedModules(true);
		}

		while (OtherModes.ZenModeOn)
		{
			foreach (BombCommander bomb in BombCommanders)
			{
				if (bomb.timerComponent == null || bomb.timerComponent.GetRate() < 0) continue;
				bomb.timerComponent.SetRateModifier(-bomb.timerComponent.GetRate());
			}
			yield return null;
		}
	}

	private void InitializeModuleCodes()
	{
		// This message assigns a unique code to each module.

		// Ignore initial “the” in module names
		string sanitizedName(TwitchComponentHandle handle) => Regex.Replace(handle.bombComponent.GetModuleDisplayName(), @"^the\s+", "", RegexOptions.IgnoreCase);

		// First, assign codes “naively”
		var dic1 = new Dictionary<string, List<TwitchComponentHandle>>();
		var numeric = 0;
		foreach (var handle in ComponentHandles)
		{
			if (handle.componentType == ComponentTypeEnum.Timer || handle.componentType == ComponentTypeEnum.Empty || handle.bombComponent == null)
				continue;

			var name = sanitizedName(handle);
			var code = name.Where(ch => (ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z' && ch != 'O')).Take(2).Join("");
			if (code.Length < 2 && name.Length >= 2)
				code = name.Where(ch => char.IsLetterOrDigit(ch)).Take(2).Join("").ToUpperInvariant();
			if (code.Length == 0)
				code = (++numeric).ToString();
			handle.Code = code;
			dic1.AddSafe(code, handle);
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
				var name = sanitizedName(kvp.Value[i]);
				for (int chIx = 1; chIx < name.Length; chIx++)
				{
					var newCode = (name[0] + "" + name[chIx]).ToUpperInvariant();
					if (name[chIx] != 'O' && char.IsLetter(name[chIx]) && !dic2.ContainsKey(newCode))
					{
						kvp.Value[i].Code = newCode;
						dic2.AddSafe(newCode, kvp.Value[i]);
						goto processed;
					}
				}
				dic2.AddSafe(kvp.Key, kvp.Value[i]);
				processed:;
			}
		}

		// If this assignment succeeded in generating unique codes, use it
		if (dic2.Values.All(list => list.Count < 2))
			return;

		var globalNumber = 1;

		// If still no success, gonna hafta use numbers
		while (true)
		{
			var tooMany = dic2.FirstOrDefault(kvp => kvp.Value.Count > 1);
			// We did it — all unique
			if (tooMany.Key == null)
				break;

			// Find other non-unique modules with the same first letter
			var all = dic2.Where(kvp => kvp.Key[0] == tooMany.Key[0] && kvp.Value.Count > 1).SelectMany(kvp => kvp.Value.Skip(1)).ToList();
			var number = 1;
			for (int i = 0; i < all.Count; i++)
			{
				dic2[all[i].Code].Remove(all[i]);
				while (dic2.ContainsKey(all[i].Code[0] + number.ToString()))
					number++;
				if (number < 10)
					all[i].Code = all[i].Code[0] + (number++).ToString();
				else
				{
					while (dic2.ContainsKey(globalNumber.ToString()))
						globalNumber++;
					all[i].Code = (globalNumber++).ToString();
				}
				dic2.AddSafe(all[i].Code, all[i]);
			}
		}
	}

	public void SetBomb(Bomb bomb, int id)
	{
		if (BombCommanders.Count == 0)
			_currentBomb = id == -1 ? -1 : 0;
		BombCommanders.Add(new BombCommander(bomb));
		CreateBombHandleForBomb(bomb, id);
		CreateComponentHandlesForBomb(bomb);
	}

	public void OnMessageReceived(string userNickName, string text)
	{
		OnMessageReceived(userNickName, null, text);
	}

	protected override void OnMessageReceived(string userNickName, string userColorCode, string text)
	{
		Match match;
		int index;
		if (!text.StartsWith("!") || text.Equals("!")) return;
		text = text.Substring(1);

		if (IsAuthorizedDefuser(userNickName))
		{
			if (text.RegexMatch(out match, "^notes(-?[0-9]+) (.+)$") && int.TryParse(match.Groups[1].Value, out index))
			{
				string notes = match.Groups[2].Value;

				IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.NotesTaken, index--, notes);

				_notesDictionary[index] = notes;
				moduleCameras?.SetNotes(index, notes);
				return;
			}

			if (text.RegexMatch(out match, "^notes(-?[0-9]+)append (.+)", "^appendnotes(-?[0-9]+) (.+)") && int.TryParse(match.Groups[1].Value, out index))
			{
				string notes = match.Groups[2].Value;

				IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.NotesAppended, index--, notes);
				if (!_notesDictionary.ContainsKey(index))
					_notesDictionary[index] = TwitchPlaySettings.data.NotesSpaceFree;

				_notesDictionary[index] += " " + notes;
				moduleCameras?.AppendNotes(index, notes);
				return;
			}

			if (text.RegexMatch(out match, "^clearnotes(-?[0-9]+)$", "^notes(-?[0-9]+)clear$") && int.TryParse(match.Groups[1].Value, out index))
			{
				IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.NoteSlotCleared, index--);

				_notesDictionary[index] = (OtherModes.ZenModeOn && index == 3) ? TwitchPlaySettings.data.ZenModeFreeSpace : TwitchPlaySettings.data.NotesSpaceFree;
				moduleCameras?.SetNotes(index, _notesDictionary[index]);
				return;
			}

			if (text.Equals("snooze", StringComparison.InvariantCultureIgnoreCase))
			{
				if (GameRoom.Instance is ElevatorGameRoom) return;  //There is no alarm clock in the elevator room.
				DropCurrentBomb();
				_coroutineQueue.AddToQueue(AlarmClockHoldableHandler.Instance.RespondToCommand(userNickName, "alarmclock snooze"));
				return;
			}

			if (text.Equals("modules", StringComparison.InvariantCultureIgnoreCase))
			{
				moduleCameras?.AttachToModules(ComponentHandles);
				return;
			}

			if (text.RegexMatch(out match, "^claims (.+)"))
			{
				OnMessageReceived(match.Groups[1].Value, userColorCode, "!claims");
				return;
			}

			if (text.Equals("claims", StringComparison.InvariantCultureIgnoreCase))
			{
				List<string> claimed = (from handle in ComponentHandles where handle.PlayerName != null && handle.PlayerName.Equals(userNickName, StringComparison.InvariantCultureIgnoreCase) && !handle.Solved select string.Format(TwitchPlaySettings.data.OwnedModule, handle.IDTextMultiDecker.text, handle.HeaderText)).ToList();
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


			if (text.RegexMatch("^(?:claim ?|view ?|all ?){2,3}$"))
			{
				if (text.Contains("claim") && text.Contains("all"))
				{
					foreach (var handle in ComponentHandles)
					{
						if (handle == null || !GameRoom.Instance.IsCurrentBomb(handle.bombID)) continue;
						handle.AddToClaimQueue(userNickName, text.Contains("view"));
					}
					return;
				}
			}

			if (text.StartsWith("claim ", StringComparison.InvariantCultureIgnoreCase))
			{
				var split = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (var claim in split.Skip(1))
				{
					TwitchComponentHandle handle = ComponentHandles.FirstOrDefault(x => x.Code.Equals(claim));
					if (handle == null || !GameRoom.Instance.IsCurrentBomb(handle.bombID)) continue;
					handle.AddToClaimQueue(userNickName);
				}
				return;
			}

			if (text.RegexMatch("^(unclaim|release) ?all$"))
			{
				foreach (TwitchComponentHandle handle in ComponentHandles)
				{
					handle.RemoveFromClaimQueue(userNickName);
				}
				string[] moduleIDs = ComponentHandles.Where(x => !x.Solved && x.PlayerName != null && x.PlayerName.Equals(userNickName, StringComparison.InvariantCultureIgnoreCase))
					.Select(x => x.Code).ToArray();
				text = string.Format("unclaim {0}", string.Join(" ", moduleIDs));
			}

			if (text.RegexMatch(out match, "^(?:unclaim|release) (.+)"))
			{
				var split = match.Groups[1].Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (var claim in split)
				{
					TwitchComponentHandle handle = ComponentHandles.FirstOrDefault(x => x.Code.Equals(claim));
					if (handle == null || !GameRoom.Instance.IsCurrentBomb(handle.bombID)) continue;
					handle.OnMessageReceived(userNickName, userColorCode, string.Format("{0} unclaim", claim));
				}
				return;
			}

			if (text.Equals("unclaimed", StringComparison.InvariantCultureIgnoreCase))
			{
				IEnumerable<string> unclaimed = ComponentHandles.Where(handle => !handle.Claimed && !handle.Solved && GameRoom.Instance.IsCurrentBomb(handle.bombID)).Shuffle().Take(3)
					.Select(handle => string.Format($"{handle.HeaderText} ({handle.Code})")).ToList();

				if (unclaimed.Any()) IRCConnection.Instance.SendMessage("Unclaimed Modules: {0}", unclaimed.Join(", "));
				else IRCConnection.Instance.SendMessage("There are no more unclaimed modules.");

				return;
			}

			if (text.Equals("unsolved", StringComparison.InvariantCultureIgnoreCase))
			{
				IEnumerable<string> unsolved = ComponentHandles.Where(handle => !handle.Solved && GameRoom.Instance.IsCurrentBomb(handle.bombID)).Shuffle().Take(3)
					.Select(handle => string.Format("{0} ({1}) - {2}", handle.HeaderText, handle.Code,
					handle.PlayerName == null ? "Unclaimed" : "Claimed by " + handle.PlayerName)).ToList();
				if (unsolved.Any()) IRCConnection.Instance.SendMessage("Unsolved Modules: {0}", unsolved.Join(", "));
				else IRCConnection.Instance.SendMessage("There are no unsolved modules, something went wrong as this message should never be displayed."); //this should never happen

				return;
			}

			if (text.RegexMatch(out match, "^(?:find|search) (.+)"))
			{
				string[] queries = match.Groups[1].Value.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);

				foreach (string query in queries)
				{
					string trimmed = query.Trim();
					IEnumerable<string> modules = ComponentHandles.Where(handle => handle.HeaderText.ContainsIgnoreCase(trimmed) && GameRoom.Instance.IsCurrentBomb(handle.bombID))
						.OrderByDescending(handle => handle.HeaderText.EqualsIgnoreCase(trimmed)).ThenBy(handle => handle.Solved).ThenBy(handle => handle.PlayerName != null).Take(3)
						.Select(handle => string.Format("{0} ({1}) - {2}", handle.HeaderText, handle.Code,
							handle.Solved ? "Solved" : (handle.PlayerName == null ? "Unclaimed" : "Claimed by " + handle.PlayerName)
						)).ToList();

					if (modules.Any()) IRCConnection.Instance.SendMessage("Modules: {0}", modules.Join(", "));
					else IRCConnection.Instance.SendMessage($"Couldn't find any modules containing \"{trimmed}\".");
				}

				return;
			}

			if (text.RegexMatch(out match, "^(?:findplayer|playerfind|searchplayer|playersearch) (.+)"))
			{
				string[] queries = match.Groups[1].Value.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
				foreach (string query in queries)
				{
					string trimmed = query.Trim();
					IEnumerable<TwitchComponentHandle> modules = ComponentHandles.Where(handle => handle.HeaderText.ContainsIgnoreCase(trimmed) && GameRoom.Instance.IsCurrentBomb(handle.bombID))
						.OrderByDescending(handle => handle.HeaderText.EqualsIgnoreCase(trimmed)).ThenBy(handle => handle.Solved).ToList();
					IEnumerable<string> playerModules = modules.Where(handle => handle.PlayerName != null).OrderByDescending(handle => handle.HeaderText.EqualsIgnoreCase(trimmed))
						.Select(handle => string.Format($"{handle.HeaderText} ({handle.Code}) - Claimed by {handle.PlayerName}")).ToList();
					if (modules.Any())
					{
						if (playerModules.Any()) IRCConnection.Instance.SendMessage("Modules: {0}", playerModules.Join(", "));
						else IRCConnection.Instance.SendMessage("None of the specified modules are claimed/have been solved.");
					}
					else IRCConnection.Instance.SendMessage($"Could not find any modules containing \"{trimmed}\".");
				}
			}

			if (text.RegexMatch(out match, "^(?:findsolved|solvedfind|searchsolved|solvedsearch) (.+)"))
			{
				string[] queries = match.Groups[1].Value.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
				foreach (string query in queries)
				{
					string trimmed = query.Trim();
					IEnumerable<BombCommander> commanders = BombCommanders.Where(handle => handle.SolvedModules.Keys.ToArray().Any(x => x.ContainsIgnoreCase(trimmed))).ToList();
					IEnumerable<TwitchComponentHandle> modules = commanders.SelectMany(x => x.SolvedModules.Where(y => y.Key.ContainsIgnoreCase(trimmed)))
						.OrderByDescending(x => x.Key.EqualsIgnoreCase(trimmed)).SelectMany(x => x.Value).ToList();
					IEnumerable<string> playerModules = modules.Where(handle => handle.PlayerName != null)
						.Select(handle => string.Format($"{handle.HeaderText} ({handle.Code}) - Claimed by {handle.PlayerName}", handle.HeaderText, handle.Code, "Claimed by " + handle.PlayerName)).ToList();
					if (commanders.Any())
					{
						if (playerModules.Any()) IRCConnection.Instance.SendMessage("Modules: {0}", playerModules.Join(", "));
						else IRCConnection.Instance.SendMessage("None of the specified modules have been solved.");
					}
					else IRCConnection.Instance.SendMessage($"Could not find any modules containing \"{trimmed}\".");
				}
			}

			if (text.RegexMatch(out match, "^(claim(?:any|van|dmod)(?:view)?|viewclaim(?:any|van|mod))"))
			{
				var vanilla = match.Groups[1].Value.Contains("van");
				var modded = match.Groups[1].Value.Contains("mod");
				var view = match.Groups[1].Value.Contains("view");
				var avoid = new[] { "Souvenir", "Forget Me Not", "Turn The Key", "Turn The Keys", "The Swan", "Forget Everything" };

				var unclaimed = ComponentHandles
					.Where(handle => (vanilla ? !handle.IsMod : modded ? handle.IsMod : true) && !handle.Claimed && !handle.Solved && !avoid.Contains(handle.HeaderText) && GameRoom.Instance.IsCurrentBomb(handle.bombID))
					.Shuffle().FirstOrDefault();

				if (unclaimed != null)
					text = unclaimed.Code + (view ? " claimview" : " claim");
				else
					IRCConnection.Instance.SendMessage(string.Format("There are no more unclaimed{0} modules.", vanilla ? " vanilla" : modded ? " modded" : null));
			}

			if (text.RegexMatch(out match, "^((?:(?:find|search)|claim|view|all){2,4}) (.+)"))
			{
				bool validFind = match.Groups[1].Value.Contains("find") || match.Groups[1].Value.Contains("search");
				bool validClaim = match.Groups[1].Value.Contains("claim");
				if (!validFind || !validClaim) return;

				bool validView = match.Groups[1].Value.Contains("view");
				bool validAll = match.Groups[1].Value.Contains("all");

				string[] queries = match.Groups[2].Value.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);

				foreach (string query in queries)
				{
					string trimmed = query.Trim();
					IEnumerable<string> modules = ComponentHandles.Where(handle => handle.HeaderText.ContainsIgnoreCase(trimmed) && GameRoom.Instance.IsCurrentBomb(handle.bombID) && !handle.Solved && !handle.Claimed)
						.OrderByDescending(handle => handle.HeaderText.EqualsIgnoreCase(trimmed)).Select(handle => $"{handle.Code}").ToList();
					if (modules.Any())
					{
						if (!validAll) modules = modules.Take(1);
						foreach (string module in modules)
						{
							TwitchComponentHandle handle = ComponentHandles.FirstOrDefault(x => x.Code.Equals(module));
							if (handle == null || !GameRoom.Instance.IsCurrentBomb(handle.bombID)) continue;
							handle.AddToClaimQueue(userNickName, validView);
						}
					}
					else IRCConnection.Instance.SendMessage($"Couldn't find any modules containing \"{trimmed}\".");
				}
				return;
			}

			if (text.Equals("newbomb", StringComparison.InvariantCultureIgnoreCase) && OtherModes.ZenModeOn)
			{
				TwitchPlaySettings.SetRewardBonus(0);
				foreach (var handle in ComponentHandles.Where(x => GameRoom.Instance.IsCurrentBomb(x.bombID)))
				{
					if (!handle.Solved) handle.SolveSilently();
				}
				return;
			}
		}

		if (text.RegexMatch(out match, "^notes(-?[0-9]+)$") && int.TryParse(match.Groups[1].Value, out index))
		{
			if (!_notesDictionary.ContainsKey(index - 1))
				_notesDictionary[index - 1] = TwitchPlaySettings.data.NotesSpaceFree;
			IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.Notes, index, _notesDictionary[index - 1]);
			return;
		}

		switch (UserAccess.HighestAccessLevel(userNickName))
		{
			case AccessLevel.Streamer:
			case AccessLevel.SuperUser:
				if (text.RegexMatch(out match, @"^setmultiplier ([0-9]+(?:\.[0-9]+)*)$"))
				{
					OtherModes.SetMultiplier(float.Parse(match.Groups[1].Value));
					return;
				}

				if (text.Equals("solvebomb", StringComparison.InvariantCultureIgnoreCase))
				{
					foreach (var handle in ComponentHandles.Where(x => GameRoom.Instance.IsCurrentBomb(x.bombID)))
					{
						if (!handle.Solved) handle.SolveSilently();
					}
					return;
				}
				goto case AccessLevel.Admin;
			case AccessLevel.Admin:
				if (text.Equals("enablecamerawall", StringComparison.InvariantCultureIgnoreCase) || text.Equals("enablemodulewall", StringComparison.InvariantCultureIgnoreCase))
				{
					moduleCameras.EnableWallOfCameras();
					StartCoroutine(HideBombs());
				}

				if (text.Equals("disablecamerawall", StringComparison.InvariantCultureIgnoreCase) || text.Equals("disablemodulewall", StringComparison.InvariantCultureIgnoreCase))
				{
					moduleCameras.DisableWallOfCameras();
					_hideBombs = false;
				}
				goto case AccessLevel.Mod;
			case AccessLevel.Mod:
				if (text.RegexMatch(out match, @"^assign (\S+) (.+)"))
				{
					var split = match.Groups[2].Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (var assign in split)
					{
						TwitchComponentHandle handle = ComponentHandles.FirstOrDefault(x => x.Code.Equals(assign));
						if (handle == null || !GameRoom.Instance.IsCurrentBomb(handle.bombID)) continue;
						handle.OnMessageReceived(userNickName, userColorCode, string.Format("{0} assign {1}", assign, match.Groups[1].Value));
					}
					return;
				}

				if (text.RegexMatch("^bot ?unclaim( ?all)?$"))
				{
					userNickName = IRCConnection.Instance.UserNickName;
					foreach (TwitchComponentHandle handle in ComponentHandles)
					{
						handle.RemoveFromClaimQueue(userNickName);
					}
					string[] moduleIDs = ComponentHandles.Where(x => !x.Solved && x.PlayerName != null && x.PlayerName.Equals(userNickName, StringComparison.InvariantCultureIgnoreCase))
						.Select(x => x.Code).ToArray();
					foreach (var claim in moduleIDs)
					{
						TwitchComponentHandle handle = ComponentHandles.FirstOrDefault(x => x.Code.Equals(claim));
						if (handle == null || !GameRoom.Instance.IsCurrentBomb(handle.bombID)) continue;
						handle.OnMessageReceived(userNickName, userColorCode, string.Format($"{claim} unclaim"));
					}
					return;
				}

				if (text.Equals("filledgework", StringComparison.InvariantCultureIgnoreCase))
				{
					foreach (var commander in BombCommanders) commander.FillEdgework(_currentBomb != commander.twitchBombHandle.bombID);
					return;
				}
				break;
		}

		GameRoom.Instance.RefreshBombID(ref _currentBomb);

		if (_currentBomb > -1)
		{
			//Check for !bomb messages, and pass them off to the currently held bomb.
			if (text.RegexMatch(out match, "^bomb (.+)"))
			{
				string internalCommand = match.Groups[1].Value;
				text = string.Format("bomb{0} {1}", _currentBomb + 1, internalCommand);
			}

			if (text.RegexMatch(out match, "^edgework$"))
			{
				text = string.Format("edgework{0}", _currentBomb + 1);
			}
			else
			{
				if (text.RegexMatch(out match, "^edgework (.+)"))
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
				_coroutineQueue.AddToQueue(BombCommanders[_currentBomb].LetGoBomb(), componentHandle.bombID);
				_currentBomb = componentHandle.bombID;
			}
			_coroutineQueue.AddToQueue(onMessageReceived, componentHandle.bombID);
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
		Dictionary<Bomb, Vector3> originalBombPositions = new Dictionary<Bomb, Vector3>();
		foreach (BombCommander commander in BombCommanders)
		{
			//Store the original positions of the bombs.
			originalBombPositions[commander.Bomb] = commander.Bomb.transform.localPosition;
		}
		while (_hideBombs)
		{
			foreach (BombCommander commander in BombCommanders)
			{
				//Required every frame for every bomb with floating holdables attached.
				commander.Bomb.transform.localPosition = new Vector3(0, -1.25f, 0);
			}
			yield return null;
		}
		foreach (BombCommander commander in BombCommanders)
		{
			//Required for bombs with no floating holdables attached.
			if (!originalBombPositions.TryGetValue(commander.Bomb, out Vector3 value)) continue;
			commander.Bomb.transform.localPosition = value;
		}
	}

	private void CreateBombHandleForBomb(MonoBehaviour bomb, int id)
	{
		TwitchBombHandle _bombHandle = Instantiate<TwitchBombHandle>(twitchBombHandlePrefab);
		_bombHandle.bombID = id;
		_bombHandle.bombCommander = BombCommanders[BombCommanders.Count - 1];
		_bombHandle.coroutineQueue = _coroutineQueue;
		BombHandles.Add(_bombHandle);
		BombCommanders[BombCommanders.Count - 1].twitchBombHandle = _bombHandle;
	}

	public bool CreateComponentHandlesForBomb(Bomb bomb)
	{
		string[] keyModules =
		{
			"SouvenirModule", "MemoryV2", "TurnTheKey", "TurnTheKeyAdvanced", "theSwan", "HexiEvilFMN"
		};
		bool foundComponents = false;

		List<BombComponent> bombComponents = bomb.BombComponents.Shuffle().ToList();

		var bombCommander = BombCommanders[BombCommanders.Count - 1];

		foreach (BombComponent bombComponent in bombComponents)
		{
			ComponentTypeEnum componentType = bombComponent.ComponentType;
			bool keyModule = false;
			string moduleName = "";

			switch (componentType)
			{
				case ComponentTypeEnum.Empty:
					continue;

				case ComponentTypeEnum.Timer:
					BombCommanders[BombCommanders.Count - 1].timerComponent = (TimerComponent) bombComponent;
					continue;

				case ComponentTypeEnum.NeedyCapacitor:
				case ComponentTypeEnum.NeedyKnob:
				case ComponentTypeEnum.NeedyVentGas:
				case ComponentTypeEnum.NeedyMod:
					moduleName = bombComponent.GetModuleDisplayName();
					keyModule = true;
					foundComponents = true;
					break;

				case ComponentTypeEnum.Mod:
					KMBombModule module = bombComponent.GetComponent<KMBombModule>();
					keyModule = keyModules.Contains(module.ModuleType);
					goto default;

				default:
					moduleName = bombComponent.GetModuleDisplayName();
					bombCommander.bombSolvableModules++;
					foundComponents = true;
					break;
			}

			if (!bombCommander.SolvedModules.ContainsKey(moduleName))
				bombCommander.SolvedModules[moduleName] = new List<TwitchComponentHandle>();

			TwitchComponentHandle handle = Instantiate<TwitchComponentHandle>(twitchComponentHandlePrefab, bombComponent.transform, false);
			handle.bombCommander = bombCommander;
			handle.bombComponent = bombComponent;
			handle.componentType = componentType;
			handle.coroutineQueue = _coroutineQueue;
			handle.bombID = _currentBomb == -1 ? -1 : BombCommanders.Count - 1;
			handle.IsKey = keyModule;

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
		if (LogUploader.Instance.PostToChat()) return;
		Debug.Log("[BombMessageResponder] Analysis URL not found, instructing LogUploader to post when it's ready");
		LogUploader.Instance.postOnComplete = true;
	}
	#endregion
}
