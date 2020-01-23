using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Assets.Scripts.Props;

static class GameCommands
{
	#region Commands during the game
	[Command(@"cancel")]
	public static void Cancel() => CoroutineCanceller.SetCancel();

	[Command(@"stop")]
	public static void Stop() => TwitchGame.Instance.StopCommands();

	[Command(@"notes(-?\d+)")]
	public static void ShowNotes([Group(1)] int index, string user, bool isWhisper) =>
		IRCConnection.SendMessage(TwitchPlaySettings.data.Notes, user, !isWhisper, index, TwitchGame.Instance.NotesDictionary.TryGetValue(index - 1, out var note) ? note : TwitchPlaySettings.data.NotesSpaceFree);

	[Command(@"notes(-?\d+) +(.+)")]
	public static void SetNotes([Group(1)] int index, [Group(2)] string notes, string user, bool isWhisper)
	{
		IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.NotesTaken, index, notes), user, !isWhisper);
		index--;
		TwitchGame.Instance.NotesDictionary[index] = notes;
		TwitchGame.ModuleCameras?.SetNotes();
	}

	[Command(@"notes(-?\d+)append +(.+)")]
	public static void SetNotesAppend([Group(1)] int index, [Group(2)] string notes, string user, bool isWhisper)
	{
		IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.NotesAppended, index, notes), user, !isWhisper);
		index--;
		if (TwitchGame.Instance.NotesDictionary.ContainsKey(index))
			TwitchGame.Instance.NotesDictionary[index] += " " + notes;
		else
			TwitchGame.Instance.NotesDictionary[index] = notes;
		TwitchGame.ModuleCameras?.SetNotes();
	}

	[Command(@"(?:notes(-?\d+)clear|clearnotes(-?\d+))")]
	public static void SetNotesClear([Group(1)] int index, string user, bool isWhisper)
	{
		IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.NoteSlotCleared, index), user, !isWhisper);
		index--;
		if (TwitchGame.Instance.NotesDictionary.ContainsKey(index))
			TwitchGame.Instance.NotesDictionary.Remove(index);
		TwitchGame.ModuleCameras?.SetNotes();
	}

	[Command(@"snooze")]
	public static IEnumerator Snooze()
	{
		if (GameRoom.Instance is ElevatorGameRoom)
			yield break;
		if (!TwitchPlaysService.Instance.Holdables.TryGetValue("alarm", out var alarmClock))
			yield break;

		var e = alarmClock.Hold();
		while (e.MoveNext())
			yield return e.Current;

		e = AlarmClockCommands.Snooze(alarmClock.Holdable.GetComponent<AlarmClock>());
		while (e.MoveNext())
			yield return e.Current;
	}

	[Command(@"claims +(.+)")]
	public static void ShowClaimsOfAnotherPlayer([Group(1)] string targetUser, string user, bool isWhisper)
	{
		if (TwitchPlaySettings.data.AnarchyMode)
			IRCConnection.SendMessage($"Sorry {user}, claiming modules is not available in anarchy mode.", user, !isWhisper);
		else if (isWhisper && TwitchPlaySettings.data.EnableWhispers)
			IRCConnection.SendMessage("Checking other people's claims in whispers is not supported.", user, false);
		else
			ShowClaimsOfUser(targetUser, targetUser, isWhisper, TwitchPlaySettings.data.OwnedModuleListOther, TwitchPlaySettings.data.NoOwnedModulesOther);
	}

	[Command(@"claims")]
	public static void ShowClaims(string user, bool isWhisper)
	{
		if (TwitchPlaySettings.data.AnarchyMode)
			IRCConnection.SendMessage($"Sorry {user}, claiming modules is not available in anarchy mode.", user, !isWhisper);
		else
			ShowClaimsOfUser(user, user, isWhisper, TwitchPlaySettings.data.OwnedModuleList, TwitchPlaySettings.data.NoOwnedModules);
	}

	[Command(@"((?:claim *|view *|pin *)+)(?: +(.+)| *(all))")]
	public static void ClaimViewPin(string user, bool isWhisper, [Group(1)] string command, [Group(2)] string claimWhat, [Group(3)] bool all)
	{
		var strings = all ? null : claimWhat.SplitFull(' ', ',', ';');
		var modules =
			all ? TwitchGame.Instance.Modules.Where(m => !m.Solved).ToArray() :
			strings.Length == 0 ? null :
			TwitchGame.Instance.Modules.Where(md => strings.Any(str => str.EqualsIgnoreCase(md.Code))).ToArray();

		if (modules == null || modules.Length == 0)
		{
			IRCConnection.SendMessage($"@{user}, no such module.", user, !isWhisper);
			return;
		}
		ClaimViewPin(user, isWhisper, modules, command.Contains("claim"), command.Contains("view"), command.Contains("pin"));
	}

	[Command(@"(?:claim *(any|van|mod) *(view)?|(view) *claim *(any|van|mod))")]
	public static void ClaimAny([Group(1)] string claimWhat1, [Group(4)] string claimWhat2, [Group(2)] bool view1, [Group(3)] bool view2, string user, bool isWhisper)
	{
		var claimWhat = claimWhat1 ?? claimWhat2;

		var vanilla = claimWhat.EqualsIgnoreCase("van");
		var modded = claimWhat.EqualsIgnoreCase("mod");
		var view = view1 || view2;
		var avoid = new[] { "Forget Everything", "Forget Me Not", "Souvenir", "The Swan", "The Time Keeper", "Turn The Key", "Turn The Keys" };

		var unclaimed = TwitchGame.Instance.Modules
			.Where(module => (vanilla ? !module.IsMod : !modded || module.IsMod) && !module.Claimed && !module.Solved && !avoid.Contains(module.HeaderText) && GameRoom.Instance.IsCurrentBomb(module.BombID))
			.Shuffle()
			.FirstOrDefault();

		if (unclaimed != null)
			ClaimViewPin(user, isWhisper, new[] { unclaimed }, claim: true, view: view);
		else
			IRCConnection.SendMessage($"There are no more unclaimed{(vanilla ? " vanilla" : modded ? " modded" : null)} modules.");
	}

	[Command(@"(?:unclaim|release) *(?:all|(q(?:ueued?)?))")]
	public static void UnclaimAll(string user, [Group(1)] bool queuedOnly)
	{
		foreach (var module in TwitchGame.Instance.Modules)
		{
			module.RemoveFromClaimQueue(user);
			// Only unclaim the player’s own modules. Avoid releasing other people’s modules if the user is a moderator.
			if (!module.Solved && !queuedOnly && module.PlayerName == user)
				module.SetUnclaimed();
		}
	}

	[Command(@"(?:unclaim|release) +(.+)")]
	public static void UnclaimSpecific([Group(1)] string unclaimWhat, string user, bool isWhisper)
	{
		var strings = unclaimWhat.SplitFull(' ', ',', ';');
		var modules = strings.Length == 0 ? null : TwitchGame.Instance.Modules.Where(md => !md.Solved && md.PlayerName == user && strings.Any(str => str.EqualsIgnoreCase(md.Code))).ToArray();
		if (modules == null || modules.Length == 0)
		{
			IRCConnection.SendMessage($"@{user}, no such module.", user, !isWhisper);
			return;
		}

		foreach (var module in modules)
			module.SetUnclaimed();
	}

	public static List<TwitchModule> unclaimedModules;
	public static int unclaimedModuleIndex;
	[Command(@"unclaimed")]
	public static void ListUnclaimed(string user, bool isWhisper)
	{
		// TwitchGame sets up the unclaimed list at the beginning of each round, so it would be hard to hit this but just in case someone does we can't do anything until they're setup.
		if (unclaimedModules == null)
		{
			return;
		}

		void checkAndWrap()
		{
			// We've reached the end, wrap back to the beginning.
			if (unclaimedModuleIndex >= unclaimedModules.Count)
			{
				// Add back any modules that may have been released.	
				unclaimedModules = TwitchGame.Instance.Modules.Where(h => h.CanBeClaimed && !h.Claimed && !h.Solved)
					.Shuffle().ToList();
				unclaimedModuleIndex = 0;
			}
		}

		// The for loop below won't run if all the modules got claimed, but we still need to add back in any modules that were released.
		if (unclaimedModules.Count == 0)
			checkAndWrap();

		List<string> unclaimed = new List<string>();
		for (int i = 0; i < 3 && i < unclaimedModules.Count; i++) // In case there are less than 3 modules, we have to lower the amount we return so we don't show repeats.
		{
			// See if there is a valid module at the current index and increment for the next go around.
			TwitchModule handle = unclaimedModules[unclaimedModuleIndex];
			if (!handle.CanBeClaimed || handle.Claimed || handle.Solved)
			{
				unclaimedModules.RemoveAt(unclaimedModuleIndex);
				i--;

				// Check if there aren't any unclaimed modules left.
				if (unclaimedModules.Count == 0)
				{
					break;
				}

				// Wrap in case we removed the last module.
				checkAndWrap();
				continue;
			}

			// At this point we have a valid module, so we should increase the index and wrap.
			unclaimedModuleIndex++;
			checkAndWrap();

			string moduleString = string.Format($"{handle.HeaderText} ({handle.Code})");
			// If we hit a duplicate because we were at the end of the list and we wrapped so we'll skip over it and get another item.
			if (unclaimed.Contains(moduleString))
			{
				i--;

				continue;
			}

			unclaimed.Add(moduleString);
		}

		// If we didn't find any unclaimed, there aren't any left.
		if (unclaimed.Count == 0)
		{
			IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.NoUnclaimed, user), user, !isWhisper);
			return;
		}

		IRCConnection.SendMessage($"Unclaimed Modules: {unclaimed.Join(", ")}");
	}

	[Command(@"unsolved")]
	public static void ListUnsolved(string user, bool isWhisper)
	{
		if (TwitchGame.Instance.Bombs.All(b => b.IsSolved))
		{
			// If the command is issued while the winning fanfare is playing.
			IRCConnection.SendMessage("All bombs already solved!", user, !isWhisper);
			return;
		}

		IEnumerable<string> unsolved = TwitchGame.Instance.Modules
			.Where(module => !module.Solved && GameRoom.Instance.IsCurrentBomb(module.BombID))
			.Shuffle().Take(3)
			.Select(module => $"{module.HeaderText} ({module.Code}) - {(module.PlayerName == null ? "Unclaimed" : "Claimed by " + module.PlayerName)}")
			.ToList();
		if (unsolved.Any())
			IRCConnection.SendMessage($"Unsolved Modules: {unsolved.Join(", ")}", user, !isWhisper);
		else
		{
			IRCConnection.SendMessage("There are no unsolved modules, something went wrong as this message should never be displayed.", user, !isWhisper);
			IRCConnection.SendMessage("Please file a bug at https://github.com/samfun123/KtaneTwitchPlays", user, !isWhisper); //this should never happen
		}
	}

	[Command(@"(?:find|search)((?: *claim| *view)*) +(.+)")]
	public static void FindClaimView([Group(1)] string commands, [Group(2)] string queries, string user, bool isWhisper)
	{
		var modules = FindModules(queries.SplitFull(',', ';').Select(q => q.Trim()).Distinct().ToArray()).ToList();
		if (!modules.Any())
		{
			IRCConnection.SendMessage("No such modules.", user, !isWhisper);
			return;
		}

		var claim = commands.ContainsIgnoreCase("claim");
		var view = commands.ContainsIgnoreCase("view");
		if (claim || view)
			ClaimViewPin(user, isWhisper, modules, claim: claim, view: view);
		else
			// Neither claim nor view: just “find”, so output top 3 search results
			IRCConnection.SendMessage("{0}, modules are: {1}", user, !isWhisper, user,
				modules.Take(3).Select(handle => string.Format("{0} ({1}) - {2}", handle.HeaderText, handle.Code, handle.Solved ? "solved" : handle.PlayerName == null ? "unclaimed" : "claimed by " + handle.PlayerName)).Join(", "));
	}

	[Command(@"(?:find *player|player *find|search *player|player *search) +(.+)", AccessLevel.User, /* Disabled in Anarchy mode */ AccessLevel.Streamer)]
	public static void FindPlayer([Group(1)] string queries, string user, bool isWhisper)
	{
		List<string> modules = FindModules(queries.SplitFull(',', ';').Select(q => q.Trim()).Distinct().ToArray(), m => m.PlayerName != null)
			.Select(module => $"{module.HeaderText} ({module.Code}) - claimed by {module.PlayerName}")
			.ToList();
		IRCConnection.SendMessage(modules.Any() ? $"Modules: {modules.Join(", ")}" : "No such claimed/solved modules.", user, !isWhisper);
	}

	[Command(@"(?:find *solved|solved *find|search *solved|solved *search) +(.+)", AccessLevel.User, /* Disabled in Anarchy mode */ AccessLevel.Streamer)]
	public static void FindSolved([Group(1)] string queries, string user, bool isWhisper)
	{
		List<string> modules = FindModules(queries.SplitFull(',', ';').Select(q => q.Trim()).Distinct().ToArray(), m => m.Solved)
			.Select(module => $"{module.HeaderText} ({module.Code}) - claimed by {module.PlayerName}")
			.ToList();
		IRCConnection.SendMessage(modules.Any() ? $"Modules: {modules.Join(", ")}" : "No such solved modules.", user, !isWhisper);
	}

	[Command(@"newbomb")]
	public static void NewBomb(string user, bool isWhisper)
	{
		if (!OtherModes.ZenModeOn)
		{
			IRCConnection.SendMessage($"{user}, the newbomb command is only allowed in Zen mode.", user, !isWhisper);
			return;
		}
		if (isWhisper)
		{
			IRCConnection.SendMessage($"{user}, the newbomb command is not allowed in whispers.", user, !isWhisper);
			return;
		}

		Leaderboard.Instance.GetRank(user, out var entry);
		if (entry == null || entry.SolveScore < TwitchPlaySettings.data.MinScoreForNewbomb && !UserAccess.HasAccess(user, AccessLevel.Defuser, true))
			IRCConnection.SendMessage($"{user}, you don’t have enough points to use the newbomb command.");
		else
		{
			TwitchPlaySettings.AddRewardBonus(-TwitchPlaySettings.GetRewardBonus());

			foreach (var bomb in TwitchGame.Instance.Bombs.Where(x => GameRoom.Instance.IsCurrentBomb(x.BombID)))
				bomb.StartCoroutine(bomb.KeepAlive());

			foreach (var module in TwitchGame.Instance.Modules.Where(x => GameRoom.Instance.IsCurrentBomb(x.BombID)))
				if (!module.Solved)
					module.SolveSilently();
		}
	}

	[Command(@"filledgework")]
	public static void FillEdgework(string user, bool isWhisper)
	{
		if (!UserAccess.HasAccess(user, AccessLevel.Mod, true) && !TwitchPlaySettings.data.EnableFilledgeworkForEveryone && !TwitchPlaySettings.data.AnarchyMode)
			return;

		foreach (var bomb in TwitchGame.Instance.Bombs)
		{
			var str = bomb.FillEdgework();
			if (bomb.BombID == TwitchGame.Instance._currentBomb)
				IRCConnection.SendMessage(TwitchPlaySettings.data.BombEdgework, user, !isWhisper, str);
		}
	}

	[Command(@"edgework((?: right| left| back| r| l| b)?)"), ElevatorOnly]
	public static IEnumerator EdgeworkElevator([Group(1)] string edge, string user, bool isWhisper) => Edgework(edge, user, isWhisper);
	[Command(@"edgework((?: 45|-45)|(?: top right| right top| right bottom| bottom right| bottom left| left bottom| left top| top left| left| top| right| bottom| tr| rt| tl| lt| br| rb| bl| lb| t| r| b| l))?"), ElevatorDisallowed]
	public static IEnumerator Edgework([Group(1)] string edge, string user, bool isWhisper)
	{
		if (TwitchPlaySettings.data.EnableEdgeworkCommand || TwitchPlaySettings.data.AnarchyMode)
			return TwitchGame.Instance.Bombs[TwitchGame.Instance._currentBomb == -1 ? 0 : TwitchGame.Instance._currentBomb].ShowEdgework(edge);
		else
		{
			string edgework = TwitchGame.Instance.Bombs.Count == 1 ?
				TwitchGame.Instance.Bombs[0].EdgeworkText.text :
				TwitchGame.Instance.Bombs.Select(bomb => $"{bomb.BombID} = {bomb.EdgeworkText.text}").Join(" //// ");

			IRCConnection.SendMessage(TwitchPlaySettings.data.BombEdgework, user, !isWhisper, edgework);
			return null;
		}
	}

	[Command(@"enablecamerawall")]
	public static void EnableCameraWall(string user)
	{
		if (TwitchPlaySettings.data.EnableAutomaticCameraWall && !UserAccess.HasAccess(user, AccessLevel.Mod, true))
			IRCConnection.SendMessage("The camera wall is being controlled automatically and cannot be enabled.");
		else
			TwitchGame.ModuleCameras.EnableCameraWall();
	}

	[Command(@"disablecamerawall")]
	public static void DisableCameraWall(string user)
	{
		if (TwitchPlaySettings.data.EnableAutomaticCameraWall && !UserAccess.HasAccess(user, AccessLevel.Mod, true))
			IRCConnection.SendMessage("The camera wall is being controlled automatically and cannot be disabled.");
		else
			TwitchGame.ModuleCameras.DisableCameraWall();
	}

	[Command(@"q(?:ueue)? +(?!\s*!)([^!]+) +(!.+)")]
	public static void EnqueueNamedCommand(IRCMessage msg, [Group(1)] string name, [Group(2)] string command)
	{
		if (name.Trim().EqualsIgnoreCase("all"))
		{
			IRCConnection.SendMessage(@"@{0}, you can’t use “all” as a name for queued commands.", msg.UserNickName, !msg.IsWhisper, msg.UserNickName);
			return;
		}
		TwitchGame.Instance.CommandQueue.Add(new CommandQueueItem(msg.Duplicate(command), name.Trim()));
		TwitchGame.ModuleCameras?.SetNotes();
		IRCConnection.SendMessage("@{0}, command queued.", msg.UserNickName, !msg.IsWhisper, msg.UserNickName);
	}

	[Command(@"q(?:ueue)? +(!.+)")]
	public static void EnqueueUnnamedCommand(IRCMessage msg, [Group(1)] string command)
	{
		TwitchGame.Instance.CommandQueue.Add(new CommandQueueItem(msg.Duplicate(command)));
		TwitchGame.ModuleCameras?.SetNotes();
		IRCConnection.SendMessage("@{0}, command queued.", msg.UserNickName, !msg.IsWhisper, msg.UserNickName);
	}

	[Command(@"(?:(un)|(del)|(show|list))q(?:ueue)?(?: *(all)| +(.+))?")]
	public static void UnqueueCommand(string user, bool isWhisper, [Group(1)] bool un, [Group(2)] bool del, [Group(3)] bool show, [Group(4)] bool all, [Group(5)] string command)
	{
		if (del && !UserAccess.HasAccess(user, AccessLevel.Mod, true))
		{
			IRCConnection.SendMessage($"{user}, you don’t have moderator access.", user, !isWhisper);
			return;
		}
		if ((del || un) && !all && (command == null || command.Trim() == ""))
		{
			IRCConnection.SendMessage($"{user}, specify a command name or use “!{(del ? "del" : "un")}qall”.", user, !isWhisper);
			return;
		}
		var matchingItems = all && un
			? TwitchGame.Instance.CommandQueue.Where(item => item.Message.UserNickName == user).ToArray()
			: all || (show && command.Trim() == "")
				? TwitchGame.Instance.CommandQueue.Where(item => all || item.Message.UserNickName == user).ToArray()
				: command.StartsWith("!")
					? TwitchGame.Instance.CommandQueue.Where(item => (all || del || item.Message.UserNickName == user) && item.Message.Text.StartsWith(command + " ")).ToArray()
					: command.Trim().Count() > 0
						? TwitchGame.Instance.CommandQueue.Where(item => all || del || item.Message.UserNickName == user && item.Name != null && item.Name == command.Trim()).ToArray()
						: TwitchGame.Instance.CommandQueue.Where(item => (all || del || item.Message.UserNickName == user) && item.Message.UserNickName.EqualsIgnoreCase(command)).ToArray();
		if (matchingItems.Length == 0)
		{
			IRCConnection.SendMessage(@"@{0}, no matching queued commands.", user, !isWhisper, user);
			return;
		}
		IRCConnection.SendMessage(@"@{0}, {1}: {2}", user, !isWhisper, user, show ? "queue contains" : "removing", matchingItems.Select(item => item.Message.Text + (item.Name != null ? $" ({item.Name})" : null)).Join("; "));
		if (!show)
		{
			TwitchGame.Instance.CommandQueue.RemoveAll(item => matchingItems.Contains(item));
			TwitchGame.ModuleCameras?.SetNotes();
		}
	}

	[Command(@"call( *now)?(?! *all| *set| *count)( +.+)?")]
	public static void CallQueuedCommand(string user, bool isWhisper, [Group(1)] bool now, [Group(2)] string name)
	{
		name = name?.Trim();

		if (TwitchGame.Instance.CallingPlayers.Contains(user))
		{
			IRCConnection.SendMessageFormat("@{0}, you already called!", user);
			return;
		}

		var _callsNeeded = TwitchGame.Instance.callsNeeded;
		var _callsTotal = TwitchGame.Instance.callsTotal;

		// Only call if there are enough calls.
		if (!(_callsTotal + 1 >= _callsNeeded) && !now)
		{
			TwitchGame.Instance.callsTotal++;
			IRCConnection.SendMessageFormat("{0} out of {1} calls needed.", _callsTotal + 1, _callsNeeded);
			TwitchGame.Instance.CallingPlayers.Add(user);
			return;
		}

		CommandQueueItem call = null;
		if (string.IsNullOrEmpty(name))
		{
			// Call the first unnamed item in the queue.
			call = TwitchGame.Instance.CommandQueue.Find(item => item.Name == null);

			if (call == null)
			{
				IRCConnection.SendMessage($"@{user}, no unnamed commands in the queue.", user, !isWhisper);
				return;
			}
		}
		else if (name.StartsWith("!"))
		{
			name += ' ';

			// Call an unnamed item in the queue for a specific module.
			call = TwitchGame.Instance.CommandQueue.Find(item => item.Message.Text.StartsWith(name) && item.Name == null);

			if (call == null)
			{
				// If a named command exists, and no unnamed commands exist, then show the name of that command (but don't call it).
				call = TwitchGame.Instance.CommandQueue.Find(item => item.Message.Text.StartsWith(name));

				if (call != null)
					IRCConnection.SendMessage($"@{user}, module {name} is queued with the name “{call.Name}”, please use “!call {call.Name}” to call it.", user, !isWhisper);
				else
					IRCConnection.SendMessage($"@{user}, no commands for module {name} in the queue.", user, !isWhisper);

				return;
			}
		}
		else
		{
			// Call a named item in the queue.
			call = TwitchGame.Instance.CommandQueue.FirstOrDefault(item => name.EqualsIgnoreCase(item.Name));

			if (call == null)
			{
				IRCConnection.SendMessage($"@{user}, no commands named “{name}” in the queue.", user, !isWhisper);
				return;
			}
		}

		TwitchGame.Instance.callsTotal = 0;
		TwitchGame.Instance.CallingPlayers.Clear();
		TwitchGame.Instance.CommandQueue.Remove(call);
		TwitchGame.ModuleCameras?.SetNotes();
		IRCConnection.SendMessageFormat("{0} {1}: {2}", now 
			? "Bypassing the required number of calls, calling" 
			: TwitchGame.Instance.callsNeeded > 1 
				? "Required calls reached, calling" 
				: "Calling", call.Message.UserNickName, call.Message.Text);
		IRCConnection.ReceiveMessage(call.Message);
	}

	[Command(@"call *all")]
	public static void CallAllQueuedCommands(string user, bool isWhisper)
	{
		if (TwitchGame.Instance.CommandQueue.Count == 0)
		{
			IRCConnection.SendMessage($"{user}, the queue is empty.", user, !isWhisper);
			return;
		}

		// Take a copy of the list in case executing one of the commands modifies the command queue
		var allCommands = TwitchGame.Instance.CommandQueue.ToList();
		TwitchGame.Instance.CommandQueue.Clear();
		TwitchGame.ModuleCameras?.SetNotes();
		foreach (var call in allCommands)
		{
			IRCConnection.SendMessageFormat("Calling {0}: {1}", call.Message.UserNickName, call.Message.Text);
			IRCConnection.ReceiveMessage(call.Message);
		}
	}

	[Command(@"call *set +(\d*)")]
	public static void CallSetCommand(string user, [Group(1)] int minimum)
	{
		if (minimum <= 0 || minimum >= 25)
		{
			IRCConnection.SendMessageFormat("@{0}, {1} is in invalid number of calls!", user, minimum);
			return;
		}

		TwitchGame.Instance.callsNeeded = minimum;
		TwitchGame.Instance.callsTotal = 0;
		TwitchGame.Instance.CallingPlayers.Clear();
		IRCConnection.SendMessageFormat("Set minimum calls to {0}.", minimum);
	}

	[Command(@"call *count")]
	public static void CallCountCommand(string user) => IRCConnection.SendMessageFormat("{0} out of {1} calls needed.", TwitchGame.Instance.callsTotal, TwitchGame.Instance.callsNeeded);

	[Command(@"setmultiplier +(\d*\.?\d+)", AccessLevel.Admin, AccessLevel.Admin)]
	public static void SetMultiplier([Group(1)] float multiplier) => OtherModes.SetMultiplier(multiplier);

	[Command(@"solvebomb", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void SolveBomb()
	{
		foreach (var bomb in TwitchGame.Instance.Bombs.Where(x => GameRoom.Instance.IsCurrentBomb(x.BombID)))
			bomb.StartCoroutine(bomb.KeepAlive());

		var modules = TwitchGame.Instance.Modules
			.Where(x => GameRoom.Instance.IsCurrentBomb(x.BombID))
			.OrderByDescending(module => module.Solver.ModInfo.moduleID.EqualsAny("cookieJars", "organizationModule", "encryptionBingo"));
		foreach (var module in modules)
			if (!module.Solved)
				module.SolveSilently();
	}

	[Command(@"enableclaims", AccessLevel.Admin, AccessLevel.Admin)]
	public static void EnableClaims()
	{
		TwitchModule.ClaimsEnabled = true;
		IRCConnection.SendMessage("Claims have been enabled.");
	}

	[Command(@"disableclaims", AccessLevel.Admin, AccessLevel.Admin)]
	public static void DisableClaims()
	{
		TwitchModule.ClaimsEnabled = false;
		IRCConnection.SendMessage("Claims have been disabled.");
	}

	[Command(@"assign +(\S+) +(.+)")]
	public static void AssignModuleTo([Group(1)] string targetUser, [Group(2)] string queries, string user)
	{
		if (TwitchPlaySettings.data.AnarchyMode)
		{
			IRCConnection.SendMessage($"{user}, assigning modules is not allowed in anarchy mode.");
			return;
		}

		var query = queries.SplitFull(' ', ',', ';');
		var denied = new List<string>();
		foreach (var module in TwitchGame.Instance.Modules.Where(m => !m.Solved && GameRoom.Instance.IsCurrentBomb(m.BombID) && query.Any(q => q.EqualsIgnoreCase(m.Code))).Take(TwitchPlaySettings.data.ModuleClaimLimit))
		{
			if ((module.PlayerName != user || !module.ClaimQueue.Any(q => q.UserNickname != targetUser)) && !UserAccess.HasAccess(user, AccessLevel.Mod, true))
				denied.Add(module.Code);
			else
				ModuleCommands.Assign(module, user, targetUser);
		}
		if (denied.Count == 1)
			IRCConnection.SendMessage($"{user}, since you’re not a moderator, {denied.First()} has not been reassigned.", user, false);
		else if (denied.Count > 1)
			IRCConnection.SendMessage($"{user}, since you’re not a moderator, {denied.Take(denied.Count - 1).Join(", ")} and {denied.Last()} have not been reassigned.", user, false);
	}

	[Command(@"bot ?unclaim( ?all)?", AccessLevel.Mod, AccessLevel.Mod)]
	public static void BotUnclaim()
	{
		foreach (var module in TwitchGame.Instance.Modules)
			if (!module.Solved && module.PlayerName == IRCConnection.Instance.UserNickName && GameRoom.Instance.IsCurrentBomb(module.BombID))
				module.SetUnclaimed();
	}

	[Command(@"disableinteractive", AccessLevel.Mod, AccessLevel.Mod)]
	public static void DisableInteractive() => TwitchGame.ModuleCameras.DisableInteractive();

	[Command(@"(?:returntosetup|leave|exit)(?:room)?|return", AccessLevel.Mod, AccessLevel.Mod)]
	public static void ReturnToSetup() => SceneManager.Instance.ReturnToSetupState();

	[Command(@"enableinteractivemode", AccessLevel.Streamer, AccessLevel.Streamer)]
	public static void EnableInteractiveMode()
	{
		IRCConnection.SendMessage("Interactive Mode Enabled");
		TwitchPlaySettings.data.EnableInteractiveMode = true;
		TwitchPlaySettings.WriteDataToFile();
		TwitchGame.EnableDisableInput();
	}

	[Command(@"disableinteractivemode", AccessLevel.Streamer, AccessLevel.Streamer)]
	public static void DisableInteractiveMode()
	{
		IRCConnection.SendMessage("Interactive Mode Disabled");
		TwitchPlaySettings.data.EnableInteractiveMode = false;
		TwitchPlaySettings.WriteDataToFile();
		TwitchGame.EnableDisableInput();
	}

	[Command(@"solveunsupportedmodules", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void SolveUnsupportedModules()
	{
		IRCConnection.SendMessage("Solving unsupported modules.");
		TwitchModule.SolveUnsupportedModules();
	}

	[Command(@"removesolvebasedmodules", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void RemoveSolveBasedModules()
	{
		IRCConnection.SendMessage("Removing solve based modules");
		TwitchGame.Instance.RemoveSolveBasedModules();
	}

	[Command(null)]
	public static bool DefaultCommand(string cmd, string user, bool isWhisper)
	{
		if (TwitchPlaySettings.data.BombCustomMessages.ContainsKey(cmd.ToLowerInvariant()))
		{
			IRCConnection.SendMessage(TwitchPlaySettings.data.BombCustomMessages[cmd.ToLowerInvariant()], user, !isWhisper);
			return true;
		}
		return false;
	}
	#endregion

	#region Private methods
	private static void ClaimViewPin(string user, bool isWhisper, IEnumerable<TwitchModule> modules, bool claim = false, bool view = false, bool pin = false)
	{
		if (isWhisper)
		{
			IRCConnection.SendMessage($"{user}, claiming modules is not allowed in whispers.", user, false);
			return;
		}
		if (TwitchPlaySettings.data.AnarchyMode)
		{
			IRCConnection.SendMessage($"{user}, claiming modules is not allowed in anarchy mode.");
			return;
		}
		foreach (var module in modules)
		{
			if (claim)
				module.AddToClaimQueue(user, view, pin);
			else if (view)
				module.ViewPin(user, pin);
		}
	}

	private static IEnumerable<TwitchModule> FindModules(string[] queries, Func<TwitchModule, bool> predicate = null) => TwitchGame.Instance.Modules
			.Where(module => queries.Any(q => module.HeaderText.ContainsIgnoreCase(q)) && GameRoom.Instance.IsCurrentBomb(module.BombID) && (predicate == null || predicate(module)))
			.OrderByDescending(handle => queries.Any(q => handle.HeaderText.EqualsIgnoreCase(q)))
			.ThenBy(handle => handle.Solved)
			.ThenBy(handle => handle.PlayerName != null);

	private static void ShowClaimsOfUser(string targetUser, string user, bool isWhisper, string ownedListMsg, string noOwnedMsg)
	{
		var claimed = TwitchGame.Instance.Modules
			.Where(module => module.PlayerName != null && module.PlayerName.EqualsIgnoreCase(targetUser) && !module.Solved)
			.Select(module => string.Format(TwitchPlaySettings.data.OwnedModule, module.Code, module.HeaderText))
			.ToList();
		if (claimed.Count > 0)
		{
			string newMessage = string.Format(ownedListMsg, user, string.Join(", ", claimed.ToArray(), 0, Math.Min(claimed.Count, 5)));
			if (claimed.Count > 5)
				newMessage += string.Format(", and {0} more.", claimed.Count - 5);
			IRCConnection.SendMessage(newMessage, user, !isWhisper);
		}
		else
			IRCConnection.SendMessage(string.Format(noOwnedMsg, user), user, !isWhisper);
	}
	#endregion
}
