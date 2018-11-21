using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Props;
using UnityEngine;

static class GameCommands
{
	#region Commands during the game
	[Command(@"cancel")]
	public static void Cancel() => CoroutineCanceller.SetCancel();

	[Command(@"stop")]
	public static void Stop() => TwitchGame.Instance.StopCommands();

	[Command(@"notes(-?\d+)")]
	public static void ShowNotes([Group(1)] int index, [Group(2)] string notes, string user, bool isWhisper) =>
		IRCConnection.SendMessage(TwitchPlaySettings.data.Notes, user, !isWhisper, index, TwitchGame.Instance.NotesDictionary.TryGetValue(index - 1, out var note) ? note : TwitchPlaySettings.data.NotesSpaceFree);

	[Command(@"notes(-?\d+) +(.+)")]
	public static void SetNotes([Group(1)] int index, [Group(2)] string notes, string user, bool isWhisper)
	{
		IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.NotesTaken, index, notes), user, !isWhisper);
		index--;
		TwitchGame.Instance.NotesDictionary[index] = notes;
		TwitchGame.ModuleCameras?.SetNotes(index, notes);
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
		TwitchGame.ModuleCameras?.AppendNotes(index, notes);
	}

	[Command(@"(?:notes(-?\d+)clear|clearnotes(-?\d+))")]
	public static void SetNotesClear([Group(1)] int index, string user, bool isWhisper)
	{
		IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.NoteSlotCleared, index), user, !isWhisper);
		index--;
		TwitchGame.Instance.NotesDictionary[index] = (OtherModes.ZenModeOn && index == 3) ? TwitchPlaySettings.data.ZenModeFreeSpace : TwitchPlaySettings.data.NotesSpaceFree;
		TwitchGame.ModuleCameras?.SetNotes(index, TwitchGame.Instance.NotesDictionary[index]);
	}

	[Command(@"snooze")]
	public static IEnumerator Snooze(string user, bool isWhisper)
	{
		if (GameRoom.Instance is ElevatorGameRoom)
			yield break;
		if (!TwitchPlaysService.Instance.Holdables.TryGetValue("alarm", out var alarmClock))
			yield break;

		var e = alarmClock.Hold();
		while (e.MoveNext())
			yield return e.Current;

		e = AlarmClockCommands.Snooze(alarmClock.Holdable.GetComponent<AlarmClock>(), user, isWhisper);
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
			ShowClaimsOfUser(targetUser, user, isWhisper, TwitchPlaySettings.data.OwnedModuleListOther, TwitchPlaySettings.data.NoOwnedModulesOther);
	}

	[Command(@"claims")]
	public static void ShowClaims(string user, bool isWhisper)
	{
		if (TwitchPlaySettings.data.AnarchyMode)
			IRCConnection.SendMessage($"Sorry {user}, claiming modules is not available in anarchy mode.", user, !isWhisper);
		else
			ShowClaimsOfUser(user, user, isWhisper, TwitchPlaySettings.data.OwnedModuleList, TwitchPlaySettings.data.NoOwnedModules);
	}

	[Command(@"(?:claim *(view)?|(view) *claim) *all")]
	public static void ClaimAll(string user, bool isWhisper, [Group(1)] bool view1, [Group(2)] bool view2) => Claim(user, isWhisper, view1 || view2, TwitchGame.Instance.Modules.Where(m => !m.Solved));

	[Command(@"(?:claim *(view)?|(view) *claim) +(?!all$)(.+)")]
	public static void ClaimSpecific(string user, bool isWhisper, [Group(1)] bool view1, [Group(2)] bool view2, [Group(3)] string claimWhat)
	{
		var strings = claimWhat.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
		var modules = strings.Length == 0 ? null : TwitchGame.Instance.Modules.Where(md => strings.Any(str => str.EqualsIgnoreCase(md.Code))).ToArray();
		if (modules == null || modules.Length == 0)
		{
			IRCConnection.SendMessage($"{user}: no such module.", user, !isWhisper);
			return;
		}
		Claim(user, isWhisper, view1 || view2, modules);
	}

	[Command(@"(?:claim *(any|van|mod) *(view)?|(view) *claim *(any|van|mod))")]
	public static void ClaimAny([Group(1)] string claimWhat1, [Group(4)] string claimWhat2, [Group(2)] bool view1, [Group(3)] bool view2, string user, bool isWhisper)
	{
		var claimWhat = claimWhat1 ?? claimWhat2;

		var vanilla = claimWhat.EqualsIgnoreCase("van");
		var modded = claimWhat.EqualsIgnoreCase("mod");
		var view = claimWhat.EqualsIgnoreCase("view");
		var avoid = new[] { "Forget Everything", "Forget Me Not", "Souvenir", "The Swan", "The Time Keeper", "Turn The Key", "Turn The Keys" };

		var unclaimed = TwitchGame.Instance.Modules
			.Where(module => (vanilla ? !module.IsMod : !modded || module.IsMod) && !module.Claimed && !module.Solved && !avoid.Contains(module.HeaderText) && GameRoom.Instance.IsCurrentBomb(module.BombID))
			.Shuffle()
			.FirstOrDefault();

		if (unclaimed != null)
			Claim(user, isWhisper, view1 || view2, new[] { unclaimed });
		else
			IRCConnection.SendMessage($"There are no more unclaimed{(vanilla ? " vanilla" : modded ? " modded" : null)} modules.");
	}

	[Command(@"(?:unclaim|release) *all")]
	public static void UnclaimAll(string user, bool isWhisper)
	{
		foreach (var module in TwitchGame.Instance.Modules)
			module.UnclaimModule(user);
	}

	[Command(@"(?:unclaim|release) +(.+)")]
	public static void UnclaimSpecific([Group(1)] string unclaimWhat, string user, bool isWhisper)
	{
		var strings = unclaimWhat.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
		var modules = strings.Length == 0 ? null : TwitchGame.Instance.Modules.Where(md => md.PlayerName == user && strings.Any(str => str.EqualsIgnoreCase(md.Code))).ToArray();
		if (modules == null || modules.Length == 0)
		{
			IRCConnection.SendMessage($"{user}: no such module.", user, !isWhisper);
			return;
		}

		foreach (var module in TwitchGame.Instance.Modules)
			module.UnclaimModule(user);
	}

	[Command(@"unclaimed")]
	public static void ListUnclaimed(string user, bool isWhisper)
	{
		IEnumerable<string> unclaimed = TwitchGame.Instance.Modules
			.Where(handle => !handle.Claimed && !handle.Solved)
			.Shuffle().Take(3)
			.Select(handle => string.Format($"{handle.HeaderText} ({handle.Code})"))
			.ToList();

		IRCConnection.SendMessage(unclaimed.Any()
			? $"Unclaimed Modules: {unclaimed.Join(", ")}"
			: string.Format(TwitchPlaySettings.data.NoUnclaimed, user), user, !isWhisper);
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
		var modules = FindModules(queries.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(q => q.Trim()).Distinct().ToArray()).ToList();
		if (!modules.Any())
		{
			IRCConnection.SendMessage("No such modules.", user, !isWhisper);
			return;
		}

		var claim = commands.ContainsIgnoreCase("claim");
		var view = commands.ContainsIgnoreCase("view");
		if (claim)
			Claim(user, isWhisper, view, modules);
		else if (view)
		{
			foreach (var module in modules)
				module.ViewPin(user, pin: false);
		}
		else
			// Neither claim nor view: just “find”, so output top 3 search results
			IRCConnection.SendMessage("{0}, modules are: {1}", user, !isWhisper, user,
				modules.Take(3).Select(handle => string.Format("{0} ({1}) - {2}", handle.HeaderText, handle.Code, handle.Solved ? "solved" : handle.PlayerName == null ? "unclaimed" : "claimed by " + handle.PlayerName)).Join(", "));
	}

	[Command(@"(?:find *player|player *find|search *player|player *search) +(.+)", AccessLevel.Defuser, /* Disabled in Anarchy mode */ AccessLevel.Streamer)]
	public static void FindPlayer([Group(1)] string commands, [Group(2)] string queries, string user, bool isWhisper)
	{
		var modules = FindModules(queries.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(q => q.Trim()).Distinct().ToArray(), m => m.PlayerName != null)
			.Select(module => string.Format($"{module.HeaderText} ({module.Code}) - claimed by {module.PlayerName}"))
			.ToList();
		IRCConnection.SendMessage(modules.Any() ? $"Modules: {modules.Join(", ")}" : "No such claimed/solved modules.", user, !isWhisper);
	}

	[Command(@"(?:find *solved|solved *find|search *solved|solved *search) +(.+)", AccessLevel.Defuser, /* Disabled in Anarchy mode */ AccessLevel.Streamer)]
	public static void FindSolved([Group(1)] string commands, [Group(2)] string queries, string user, bool isWhisper)
	{
		var modules = FindModules(queries.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(q => q.Trim()).Distinct().ToArray(), m => m.Solved)
			.Select(module => string.Format($"{module.HeaderText} ({module.Code}) - claimed by {module.PlayerName}"))
			.ToList();
		IRCConnection.SendMessage(modules.Any() ? $"Modules: {modules.Join(", ")}" : "No such solved modules.", user, !isWhisper);
	}

	[Command(@"newbomb")]
	public static void NewBomb(string user, bool isWhisper)
	{
		if (!OtherModes.ZenModeOn)
		{
			IRCConnection.SendMessage($"Sorry {user}, the newbomb command is only allowed in Zen mode.", user, !isWhisper);
			return;
		}
		if (isWhisper)
		{
			IRCConnection.SendMessage($"Sorry {user}, the newbomb command is not allowed in whispers.", user, !isWhisper);
			return;
		}

		Leaderboard.Instance.GetRank(user, out var entry);
		if (entry.SolveScore < TwitchPlaySettings.data.MinScoreForNewbomb && !UserAccess.HasAccess(user, AccessLevel.Defuser, true))
			IRCConnection.SendMessage($"Sorry {user}, you don’t have enough points to use the newbomb command.");
		else
		{
			OtherModes.DisableLeaderboard(true);
			TwitchPlaySettings.AddRewardBonus(-TwitchPlaySettings.GetRewardBonus());

			foreach (var bomb in TwitchGame.Instance.Bombs.Where(x => GameRoom.Instance.IsCurrentBomb(x.BombID)))
				bomb.Bomb.GetTimer().StopTimer();

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
		if (TwitchPlaySettings.data.EnableAutomaticCameraWall && !UserAccess.HasAccess(user, AccessLevel.Admin, true))
			IRCConnection.SendChatMessage("The camera wall is being controlled automatically and cannot be enabled.");
		else
			TwitchGame.ModuleCameras.EnableCameraWall();
	}

	[Command(@"disablecamerawall")]
	public static void DisableCameraWall(string user)
	{
		if (TwitchPlaySettings.data.EnableAutomaticCameraWall && !UserAccess.HasAccess(user, AccessLevel.Admin, true))
			IRCConnection.SendChatMessage("The camera wall is being controlled automatically and cannot be disabled.");
		else
			TwitchGame.ModuleCameras.DisableCameraWall();
	}

	[Command(@"setmultiplier (\d*\.?\d+)", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void SetMultiplier([Group(1)] float multiplier) => OtherModes.SetMultiplier(multiplier);

	[Command(@"solvebomb", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void SolveBomb()
	{
		OtherModes.DisableLeaderboard();
		foreach (var bomb in TwitchGame.Instance.Bombs.Where(x => GameRoom.Instance.IsCurrentBomb(x.BombID)))
			bomb.Bomb.GetTimer().StopTimer();
		foreach (var module in TwitchGame.Instance.Modules.Where(x => GameRoom.Instance.IsCurrentBomb(x.BombID)))
			if (!module.Solved)
				module.SolveSilently();
	}

	[Command(@"enableclaims", AccessLevel.Admin, AccessLevel.Admin)]
	public static void EnableClaims()
	{
		TwitchModule.ClaimsEnabled = true;
		IRCConnection.SendChatMessage("Claims have been enabled.");
	}

	[Command(@"disableclaims", AccessLevel.Admin, AccessLevel.Admin)]
	public static void DisableClaims()
	{
		TwitchModule.ClaimsEnabled = false;
		IRCConnection.SendChatMessage("Claims have been disabled.");
	}

	[Command(@"assign +(\S+) +(.+)", AccessLevel.Mod, AccessLevel.Mod)]
	public static void AssignModuleTo([Group(1)] string targetUser, [Group(2)] string queries, string user)
	{
		var query = queries.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (var module in TwitchGame.Instance.Modules.Where(m => !m.Solved && GameRoom.Instance.IsCurrentBomb(m.BombID) && query.Any(q => q.EqualsIgnoreCase(m.Code))).Take(TwitchPlaySettings.data.ModuleClaimLimit))
			ModuleCommands.Assign(module, user, targetUser);
	}

	[Command(@"bot ?unclaim( ?all)?", AccessLevel.Mod, AccessLevel.Mod)]
	public static void BotUnclaim([Group(1)] bool all, string user)
	{
		var modules = TwitchGame.Instance.Modules;
		foreach (var module in modules)
		{
			module.RemoveFromClaimQueue(IRCConnection.Instance.UserNickName);

			if (!module.Solved && module.PlayerName != null && module.PlayerName.EqualsIgnoreCase(IRCConnection.Instance.UserNickName) && GameRoom.Instance.IsCurrentBomb(module.BombID))
				module.UnclaimModule(user);
		}
	}

	[Command(@"disableinteractive", AccessLevel.Mod, AccessLevel.Mod)]
	public static void DisableInteractive() => TwitchGame.ModuleCameras.DisableInteractive();

	[Command(@"enabletwitchplays", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void EnableTwitchPlays()
	{
		IRCConnection.SendChatMessage("Twitch Plays Enabled");
		TwitchPlaySettings.data.EnableTwitchPlaysMode = true;
		TwitchPlaySettings.WriteDataToFile();
		TwitchGame.EnableDisableInput();
	}

	[Command(@"disabletwitchplays", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void DisableTwitchPlays()
	{
		IRCConnection.SendChatMessage("Twitch Plays Disabled");
		TwitchPlaySettings.data.EnableTwitchPlaysMode = false;
		TwitchPlaySettings.WriteDataToFile();
		TwitchGame.EnableDisableInput();
	}

	[Command(@"enableinteractivemode", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void EnableInteractiveMode()
	{
		IRCConnection.SendChatMessage("Interactive Mode Enabled");
		TwitchPlaySettings.data.EnableInteractiveMode = true;
		TwitchPlaySettings.WriteDataToFile();
		TwitchGame.EnableDisableInput();
	}

	[Command(@"disableinteractivemode", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void DisableInteractiveMode()
	{
		IRCConnection.SendChatMessage("Interactive Mode Disabled");
		TwitchPlaySettings.data.EnableInteractiveMode = false;
		TwitchPlaySettings.WriteDataToFile();
		TwitchGame.EnableDisableInput();
	}

	[Command(@"solveunsupportedmodules", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void SolveUnsupportedModules()
	{
		IRCConnection.SendChatMessage("Solving unsupported modules.");
		TwitchModule.SolveUnsupportedModules();
	}

	[Command(@"removesolvebasedmodules", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void RemoveSolveBasedModules()
	{
		IRCConnection.SendChatMessage("Removing solve based modules");
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
	private static void Claim(string user, bool isWhisper, bool view, IEnumerable<TwitchModule> modules)
	{
		if (isWhisper)
		{
			IRCConnection.SendMessage($"Sorry {user}, claiming modules is not allowed in whispers.", user, false);
			return;
		}
		if (TwitchPlaySettings.data.AnarchyMode)
		{
			IRCConnection.SendMessage($"Sorry {user}, claiming modules is not allowed in anarchy mode.");
			return;
		}
		foreach (var module in modules)
		{
			module.AddToClaimQueue(user, view);
			if (view)
				module.ViewPin(user, pin: false);
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
