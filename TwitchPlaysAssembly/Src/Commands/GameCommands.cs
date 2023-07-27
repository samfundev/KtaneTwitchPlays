using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Props;
using UnityEngine;

/// <summary>Commands that can be run during a game.</summary>
static class GameCommands
{
	public static List<IRCMessage> calledCommands = new List<IRCMessage>();

	#region Commands during the game
	/// <name>Cancel</name>
	/// <syntax>cancel</syntax>
	/// <summary>Cancels the current running command.</summary>
	[Command(@"cancel")]
	public static void Cancel() => CoroutineCanceller.SetCancel();

	/// <name>Stop</name>
	/// <syntax>stop</syntax>
	/// <summary>Stops the current and queued commands.</summary>
	[Command(@"stop")]
	public static void Stop() => TwitchGame.Instance.StopCommands();

	/// <name>Get Notes</name>
	/// <syntax>notes[note]</syntax>
	/// <summary>Sends the contents of a note to chat.</summary>
	/// <argument name="note">The note's number.</argument>
	[Command(@"notes(-?\d+)")]
	public static void ShowNotes([Group(1)] int index, string user, bool isWhisper) =>
		IRCConnection.SendMessage(TwitchPlaySettings.data.Notes, user, !isWhisper, index, TwitchGame.Instance.NotesDictionary.TryGetValue(index - 1, out var note) ? note : TwitchPlaySettings.data.NotesSpaceFree);

	/// <name>Set Notes</name>
	/// <syntax>notes[note] [contents]</syntax>
	/// <summary>Sets the contents of a note.</summary>
	/// <argument name="note">The note's number.</argument>
	/// <argument name="contents">New text of the note.</argument>
	[Command(@"notes(-?\d+) +(.+)")]
	public static void SetNotes([Group(1)] int index, [Group(2)] string notes, string user, bool isWhisper)
	{
		IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.NotesTaken, index, notes), user, !isWhisper);
		index--;
		TwitchGame.Instance.NotesDictionary[index] = notes;
		TwitchGame.ModuleCameras?.SetNotes();
	}

	/// <name>Append Notes</name>
	/// <syntax>notes[note]append [contents]</syntax>
	/// <summary>Appends the contents of a note.</summary>
	/// <argument name="note">The note's number.</argument>
	/// <argument name="contents">The text that will be appended to the note.</argument>
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

	/// <name>Clear Notes</name>
	/// <syntax>notes[note]clear</syntax>
	/// <summary>Clears the contents of a note.</summary>
	/// <argument name="note">The note's number.</argument>
	[Command(@"(?:notes(-?\d+)clear|clearnotes(-?\d+))")]
	public static void SetNotesClear([Group(1)] int index, string user, bool isWhisper)
	{
		IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.NoteSlotCleared, index), user, !isWhisper);
		index--;
		if (TwitchGame.Instance.NotesDictionary.ContainsKey(index))
			TwitchGame.Instance.NotesDictionary.Remove(index);
		TwitchGame.ModuleCameras?.SetNotes();
	}

	/// <name>Snooze</name>
	/// <syntax>snooze</syntax>
	/// <summary>Snoozes the alarm clock.</summary>
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

	/// <name>Show Claims</name>
	/// <syntax>claims [user]</syntax>
	/// <summary>Shows the claims of another user.</summary>
	/// <argument name="user">The user whose claims you want to see.</argument>
	[Command(@"claims +(.+)")]
	public static void ShowClaimsOfAnotherPlayer([Group(1)] string targetUser, string user, bool isWhisper)
	{
		if (TwitchPlaySettings.data.AnarchyMode)
			IRCConnection.SendMessage($"Sorry {user}, claiming modules is not available in anarchy mode.", user, !isWhisper);
		else if (isWhisper && TwitchPlaySettings.data.EnableWhispers)
			IRCConnection.SendMessage("Checking other people's claims in whispers is not supported.", user, false);
		else
			ShowClaimsOfUser(targetUser, isWhisper, TwitchPlaySettings.data.OwnedModuleListOther, TwitchPlaySettings.data.NoOwnedModulesOther);
	}

	/// <name>Claims</name>
	/// <syntax>claims</syntax>
	/// <summary>Shows all of your claims.</summary>
	[Command(@"claims")]
	public static void ShowClaims(string user, bool isWhisper)
	{
		if (TwitchPlaySettings.data.AnarchyMode)
			IRCConnection.SendMessage($"Sorry {user}, claiming modules is not available in anarchy mode.", user, !isWhisper);
		else
			ShowClaimsOfUser(user, isWhisper, TwitchPlaySettings.data.OwnedModuleList, TwitchPlaySettings.data.NoOwnedModules);
	}

	/// <name>Claim View Pin</name>
	/// <syntax>claim (what)\nview (what)\npin (what)\n(actions) (what)</syntax>
	/// <summary>Claims, views or pins a list of module codes. (actions) should be some combination of claim, view or pin seperated by spaces.</summary>
	/// <argument name="actions">A combination of claim, view or pin seperated by spaces.</argument>
	/// <argument name="what">A list of module codes to take the actions on. Can be "all" to do the action on all unsolved modules.</argument>
	[Command(@"((?:claim *|view *|pin *)+)(?: +(.+)| *(all))")]
	public static void ClaimViewPin(string user, bool isWhisper, [Group(1)] string command, [Group(2)] string claimWhat, [Group(3)] bool all)
	{
		var strings = all ? null : claimWhat.SplitFull(' ', ',', ';');
		var modules =
			all ? TwitchGame.Instance.Modules.Where(m => !m.Solved && !m.Hidden).ToArray() :
			strings.Length == 0 ? null :
			TwitchGame.Instance.Modules.Where(md => strings.Any(str => str.EqualsIgnoreCase(md.Code)) && !md.Hidden).ToArray();

		if (modules == null || modules.Length == 0)
		{
			IRCConnection.SendMessage($"@{user}, no such module.", user, !isWhisper);
			return;
		}
		ClaimViewPin(user, isWhisper, modules, command.Contains("claim"), command.Contains("view"), command.Contains("pin"));
	}

	/// <name>Claim Any</name>
	/// <syntax>claim [source]\nclaim [source] view</syntax>
	/// <summary>Claims one unsolved and unclaimed module from a source. any, van or mod are acceptable sources.</summary>
	/// <argument name="source">The source of the modules to pick from. any for any module, van for vanilla and mod for modded modules.</argument>
	[Command(@"(?:claim *(any|van|mod) *(view)?|(view) *claim *(any|van|mod))")]
	public static void ClaimAny([Group(1)] string claimWhat1, [Group(4)] string claimWhat2, [Group(2)] bool view1, [Group(3)] bool view2, string user, bool isWhisper)
	{
		var claimWhat = claimWhat1 ?? claimWhat2;

		var vanilla = claimWhat.EqualsIgnoreCase("van");
		var modded = claimWhat.EqualsIgnoreCase("mod");
		var view = view1 || view2;
		var avoid = new[] { "Forget Everything", "Forget Me Not", "Souvenir", "The Swan", "The Time Keeper", "Turn The Key", "Turn The Keys" };

		var unclaimed = TwitchGame.Instance.Modules
			.Where(module => (vanilla ? !module.IsMod : !modded || module.IsMod) && !module.Claimed && !module.Solved && !module.Hidden && !avoid.Contains(module.HeaderText) && GameRoom.Instance.IsCurrentBomb(module.BombID))
			.Shuffle()
			.FirstOrDefault();

		if (unclaimed != null)
			ClaimViewPin(user, isWhisper, new[] { unclaimed }, claim: true, view: view);
		else
			IRCConnection.SendMessage($"There are no more unclaimed{(vanilla ? " vanilla" : modded ? " modded" : null)} modules.");
	}

	/// <name>Unclaim All</name>
	/// <syntax>unclaim all\nunclaim queued</syntax>
	/// <summary>Unclaims all modules you have claimed and queued claims. If queued is used instead of all, then only queued claims will be removed.</summary>
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

	/// <name>Unclaim Specific</name>
	/// <syntax>unclaim [what]</syntax>
	/// <summary>Unclaims a list of module codes.</summary>
	/// <argument name="what">A list of module codes to unclaim.</argument>
	[Command(@"(?:unclaim|release) +(.+)")]
	public static void UnclaimSpecific([Group(1)] string unclaimWhat, string user, bool isWhisper)
	{
		var strings = unclaimWhat.SplitFull(' ', ',', ';');
		var modules = strings.Length == 0 ? null : TwitchGame.Instance.Modules.Where(md => !md.Solved && !md.Hidden && md.PlayerName == user && strings.Any(str => str.EqualsIgnoreCase(md.Code))).ToArray();
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
	/// <name>Unclaimed</name>
	/// <syntax>unclaimed</syntax>
	/// <summary>Sends a maximum of 3 unclaimed modules to chat.</summary>
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
				unclaimedModules = TwitchGame.Instance.Modules.Where(h => h.CanBeClaimed && !h.Claimed && !h.Solved && !h.Hidden && Votes.voteModule != h && !h.Votesolving)
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
			if (!handle.CanBeClaimed || handle.Claimed || handle.Solved || handle.Hidden || Votes.voteModule == handle || handle.Votesolving)
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

	/// <name>Unsolved</name>
	/// <syntax>unsolved</syntax>
	/// <summary>Sends a maximum of 3 unsolved modules to chat.</summary>
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
			.Where(module => !module.Solved && GameRoom.Instance.IsCurrentBomb(module.BombID) && !module.Hidden)
			.Shuffle().Take(3)
			.Select(module => $"{module.HeaderText} ({module.Code}) - {(module.PlayerName == null ? "Unclaimed" : "Claimed by " + module.PlayerName)}")
			.ToList();

		IRCConnection.SendMessage(unsolved.Any() ? $"Unsolved Modules: {unsolved.Join(", ")}" : "There are no unsolved modules on this bomb that aren't hidden.", user, !isWhisper);
	}

	/// <name>Find Claim View</name>
	/// <syntax>find (actions) [what]</syntax>
	/// <summary>Finds modules based on their module name. [what] can be partial module names seperated by commas or semicolons. If (actions) are specified, they will be executed on the matching modules.</summary>
	/// <argument name="actions">A combination of claim or view seperated by spaces.</argument>
	/// <argument name="what">Partial module names seperated by commas or semicolons.</argument>
	[Command(@"(?:find|search)((?: *claim| *view)*) +(.+)")]
	public static void FindClaimView([Group(1)] string commands, [Group(2)] string queries, string user, bool isWhisper)
	{
		var claim = commands.ContainsIgnoreCase("claim");
		var view = commands.ContainsIgnoreCase("view");

		var terms = queries.SplitFull(',', ';').Select(q => q.Trim()).Distinct().ToArray();
		if (terms.Length > TwitchPlaySettings.data.FindClaimTerms && claim && TwitchPlaySettings.data.FindClaimTerms != -1)
		{
			IRCConnection.SendMessageFormat("@{0}, please reduce the size of your list to {1} or fewer terms.", user, TwitchPlaySettings.data.FindClaimTerms); // Prevents lists greater than length of 3 while using !claim
			return;
		}

		var modules = FindModules(terms).ToList();
		if (modules.Count == 0)
		{
			IRCConnection.SendMessage("No such modules.", user, !isWhisper);
			return;
		}

		if (claim)
		{
			if (!TwitchGame.Instance.FindClaimEnabled && !OtherModes.TrainingModeOn)
			{
				IRCConnection.SendMessageFormat("@{0}, the findclaim command may only be used after {1} seconds have passed.", user, TwitchPlaySettings.data.FindClaimDelay); // Prevents findclaim spam at the start of a bomb
				return;
			}
			if (!TwitchGame.Instance.FindClaimPlayers.ContainsKey(user)) TwitchGame.Instance.FindClaimPlayers.Add(user, 0);

			var _prevClaims = TwitchGame.Instance.FindClaimPlayers[user];
			var _allowedClaims = TwitchGame.Instance.FindClaimUse;
			var _remainingClaims = _allowedClaims - _prevClaims;

			if (_remainingClaims < 1 && TwitchPlaySettings.data.FindClaimLimit != -1)
			{
				IRCConnection.SendMessageFormat("@{0}, you have no more findclaim uses.", user);
				return;
			}

			if (modules.Count > _remainingClaims && TwitchPlaySettings.data.FindClaimLimit != -1)
			{
				IRCConnection.SendMessageFormat("@{0}, that goes over your current findclaim limit of {1}. You will receive the first {2} claims.", user, _allowedClaims, _remainingClaims);
				ClaimViewPin(user, isWhisper, modules.Take(_remainingClaims), claim: claim, view: view);
			}
			else ClaimViewPin(user, isWhisper, modules, claim: claim, view: view);

			TwitchGame.Instance.FindClaimPlayers[user] = _prevClaims + modules.Count;
		}
		else if (view) ClaimViewPin(user, isWhisper, modules, claim: claim, view: view);
		else
			// Neither claim nor view: just “find”, so output top 3 search results
			IRCConnection.SendMessage("{0}, modules ({2} total) are: {1}", user, !isWhisper, user,
				modules.Take(3).Select(handle =>
					$"{handle.HeaderText} ({handle.Code}) - {(handle.Solved ? "solved" : handle.PlayerName == null ? "unclaimed" : "claimed by " + handle.PlayerName)}").Join(", "),
				modules.Count);
	}

	/// <name>Find Player</name>
	/// <syntax>findplayer [what]</syntax>
	/// <summary>Finds claimed modules based on their module name and shows who has the claim on the module. [what] can be partial module names seperated by commas or semicolons.</summary>
	/// <argument name="what">A combination of claim or view seperated by spaces.</argument>
	[Command(@"(?:find *player|player *find|search *player|player *search) +(.+)", AccessLevel.User, /* Disabled in Anarchy mode */ AccessLevel.Streamer)]
	public static void FindPlayer([Group(1)] string queries, string user, bool isWhisper)
	{
		List<string> modules = FindModules(queries.SplitFull(',', ';').Select(q => q.Trim()).Distinct().ToArray(), m => m.PlayerName != null)
			.Select(module => $"{module.HeaderText} ({module.Code}) - claimed by {module.PlayerName}")
			.ToList();
		IRCConnection.SendMessage(modules.Count > 0 ? $"Modules: {modules.Join(", ")}" : "No such claimed/solved modules.", user, !isWhisper);
	}

	/// <name>Find Solved</name>
	/// <syntax>findsolved [what]</syntax>
	/// <summary>Finds solved modules based on their module name and shows who has the claim on the module. [what] can be partial module names seperated by commas or semicolons.</summary>
	/// <argument name="what">A combination of claim or view seperated by spaces.</argument>
	[Command(@"(?:find *solved|solved *find|search *solved|solved *search) +(.+)", AccessLevel.User, /* Disabled in Anarchy mode */ AccessLevel.Streamer)]
	public static void FindSolved([Group(1)] string queries, string user, bool isWhisper)
	{
		List<string> modules = FindModules(queries.SplitFull(',', ';').Select(q => q.Trim()).Distinct().ToArray(), m => m.Solved)
			.Select(module => $"{module.HeaderText} ({module.Code}) - claimed by {module.PlayerName}")
			.ToList();
		IRCConnection.SendMessage(modules.Count > 0 ? $"Modules: {modules.Join(", ")}" : "No such solved modules.", user, !isWhisper);
	}

	/// <name>Find Duplicate</name>
	/// <syntax>findduplicate (what)</syntax>
	/// <summary>Finds duplicate modules based on their module name. (what) can be partial module names seperated by commas or semicolons. If not specified, all modules will be searched.</summary>
	/// <argument name="what">A combination of claim or view seperated by spaces.</argument>
	[Command(@"(?:find *dup(?:licate)?|dup(?:licate)? *find|search *dup(?:licate)?|dup(?:licate)? *search)( +.+)?")]
	public static void FindDuplicate([Group(1)] string queries, string user, bool isWhisper)
	{
		var allMatches = (string.IsNullOrEmpty(queries) ? TwitchGame.Instance.Modules : FindModules(queries.SplitFull(',', ';').Select(q => q.Trim()).Distinct().ToArray()))
			.GroupBy(module => module.HeaderText)
			.Where(grouping => grouping.Count() > 1)
			.Select(grouping => $"{grouping.Key} ({grouping.Select(module => module.Code).Join(", ")})");

		var modules = allMatches.Shuffle().Take(3).ToList();

		IRCConnection.SendMessage(modules.Count > 0 ? $"Duplicates ({allMatches.Count()} total): {modules.Join(", ")}" : "No such duplicate modules.", user, !isWhisper);
	}

	/// <name>New Bomb</name>
	/// <syntax>newbomb</syntax>
	/// <summary>Starts a new bomb in training mode. Requires either a minimum score or the defuser rank to run.</summary>
	[Command(@"newbomb")]
	public static void NewBomb(string user, bool isWhisper)
	{
		if (!OtherModes.TrainingModeOn)
		{
			IRCConnection.SendMessage($"{user}, the newbomb command is only allowed in Training mode.", user, !isWhisper);
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

	/// <name>Fill Edgework</name>
	/// <syntax>filledgework</syntax>
	/// <summary>Fills in the text-based edgework. Requires either mod rank or it to be enabled for everyone.</summary>
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

	/// <name>Elevator Edgework</name>
	/// <syntax>edgework (wall)</syntax>
	/// <summary>Shows the edgework on the elevator. (wall) is which wall of the elevator to show, ex: right, left or back.</summary>
	/// <restriction>ElevatorOnly</restriction>
	[Command(@"edgework((?: right| left| back| r| l| b)?)"), ElevatorOnly]
	public static IEnumerator EdgeworkElevator([Group(1)] string edge, string user, bool isWhisper) => Edgework(edge, user, isWhisper);
	/// <name>Edgework</name>
	/// <syntax>edgework (edge)\nedgework 45</syntax>
	/// <summary>Rotates the bomb to show the edgework. (edge) is which edge of the bomb will be shown, ex: top or top left. Using 45 will rotate the bomb in 45 degree increments.</summary>
	/// <restriction>ElevatorDisallowed</restriction>
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

	/// <name>Camera Wall</name>
	/// <syntax>camerawall [mode]</syntax>
	/// <summary>Sets the mode of the camera wall to either on, off or auto. If automatic camera wall is enabled, mod rank is required to use.</summary>
	/// <argument name="mode">The mode of the camera wall. Can be on, off or auto.</argument>
	[Command(@"(?:camerawall|cw) *(on|enabled?|off|disabled?|auto)")]
	public static void CameraWall(string user, [Group(1)] string mode)
	{
		if (TwitchPlaySettings.data.EnableAutomaticCameraWall && !UserAccess.HasAccess(user, AccessLevel.Mod, true))
		{
			IRCConnection.SendMessage("The camera wall is being controlled automatically and can only be changed by mods.");
			return;
		}

		if (mode.EqualsAny("on", "enable", "enabled"))
			TwitchGame.ModuleCameras.CameraWallMode = ModuleCameras.Mode.Enabled;
		else if (mode.EqualsAny("off", "disable", "disabled"))
			TwitchGame.ModuleCameras.CameraWallMode = ModuleCameras.Mode.Disabled;
		else if (mode.StartsWith("auto"))
			TwitchGame.ModuleCameras.CameraWallMode = ModuleCameras.Mode.Automatic;
	}

	/// <name>Queue Named Command</name>
	/// <syntax>queue [name] [command]</syntax>
	/// <summary>Queues a command that can be called by name.</summary>
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
		TwitchGame.Instance.CallUpdate(false);
	}

	/// <name>Queue Command</name>
	/// <syntax>queue [command]</syntax>
	/// <summary>Queues a command that will be called in order.</summary>
	[Command(@"q(?:ueue)? +(!.+)")]
	public static void EnqueueUnnamedCommand(IRCMessage msg, [Group(1)] string command)
	{
		var simplifiedCommand = command.Trim().ToLowerInvariant();
		if (!UserAccess.HasAccess(msg.UserNickName, AccessLevel.Admin, true) && (simplifiedCommand.StartsWith("!q") || simplifiedCommand.StartsWith("!bomb")))
		{
			IRCConnection.SendMessage("@{0}, you cannot queue that command.", msg.UserNickName, !msg.IsWhisper, msg.UserNickName);
			return;
		}

		TwitchGame.Instance.CommandQueue.Add(new CommandQueueItem(msg.Duplicate(command)));
		TwitchGame.ModuleCameras?.SetNotes();
		IRCConnection.SendMessage("@{0}, command queued.", msg.UserNickName, !msg.IsWhisper, msg.UserNickName);
		TwitchGame.Instance.CallUpdate(false);
	}

	/// <name>Unqueue/Show Command</name>
	/// <syntax>unqueue [command]\ndelqueue [command]\nshowqueue [command]</syntax>
	/// <summary>Unqueues, deletes or shows a queued command. Unqueuing only allows you to remove your own commands. Deleting is a moderator only action that can remove any command.</summary>
	/// <argument name="command">The command to find in the queue. Can be "all" for all of your commands or just all commands if delqueue is being used.</argument>
	[Command(@"(?:(un)|(del)|(show|list))q(?:ueue)?(?: *(all)| +(.+))?")]
	public static void UnqueueCommand(string user, bool isWhisper, [Group(1)] bool un, [Group(2)] bool del, [Group(3)] bool show, [Group(4)] bool all, [Group(5)] string command)
	{
		if (del && !UserAccess.HasAccess(user, AccessLevel.Mod, true))
		{
			IRCConnection.SendMessage($"{user}, you don’t have moderator access.", user, !isWhisper);
			return;
		}
		if ((del || un) && !all && string.IsNullOrEmpty(command?.Trim()))
		{
			IRCConnection.SendMessage($"{user}, specify a command name or use “!{(del ? "del" : "un")}qall”.", user, !isWhisper);
			return;
		}
		var matchingItems = all && un
			? TwitchGame.Instance.CommandQueue.Where(item => item.Message.UserNickName == user).ToArray()
			: all || (show && string.IsNullOrEmpty(command?.Trim()))
				? TwitchGame.Instance.CommandQueue.Where(item => all || item.Message.UserNickName == user).ToArray()
				: command.StartsWith("!")
					? TwitchGame.Instance.CommandQueue.Where(item => (all || del || item.Message.UserNickName == user) && item.Message.Text.StartsWith(command + " ")).ToArray()
					: command.Trim().Length > 0
						? TwitchGame.Instance.CommandQueue.Where(item => (all || del || item.Message.UserNickName == user) && item.Name != null && item.Name == command.Trim()).ToArray()
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

	/// <name>Queue On/Off</name>
	/// <syntax>queue on\nqueue off</syntax>
	/// <summary>Turns the queue on or off, letting other users know that they need to use the queue.</summary>
	[Command(@"q(?:ueue)?(on|off)")]
	public static void QueueEnabled([Group(1)] string state)
	{
		TwitchGame.Instance.QueueEnabled = state.EqualsIgnoreCase("on");
		TwitchGame.ModuleCameras?.SetNotes();
	}

	/// <name>Call Command</name>
	/// <syntax>call (name)\ncallnow (name)</syntax>
	/// <summary>Calls a command from the queue. callnow skips the requirement set by Call Set. If (name) is specified calls a named command instead of the next command in the queue.</summary>
	[Command(@"call( *now)?( +.+)?")]
	public static void CallQueuedCommand(string user, [Group(1)] bool now, [Group(2)] string name)
	{
		name = (name?.Trim()) ?? "";
		var response = TwitchGame.Instance.CheckIfCall(false, now, user, name, out bool callChanged);
		if (response != TwitchGame.CallResponse.Success)
		{
			TwitchGame.Instance.SendCallResponse(user, name, response, callChanged);
			return;
		}
		if (callChanged) IRCConnection.SendMessageFormat("@{0}, your call has been changed to {1}.", user, string.IsNullOrEmpty(name) ? "the next queued command" : name);
		TwitchGame.Instance.CommandQueue.Remove(TwitchGame.Instance.callSend);
		TwitchGame.ModuleCameras?.SetNotes();
		IRCConnection.SendMessageFormat("{0} {1}: {2}", TwitchGame.Instance.callWaiting && string.IsNullOrEmpty(user)
			? "Call waiting, calling"
			: now
				? "Bypassing the required number of calls, calling"
				: TwitchGame.Instance.callsNeeded > 1
					? "Required calls reached, calling"
					: "Calling", TwitchGame.Instance.callSend.Message.UserNickName, TwitchGame.Instance.callSend.Message.Text);
		DeleteCallInformation(true);
		if (TwitchGame.Instance.Bombs.Any(x => x.BackdoorHandleHack))
			IRCConnection.SendMessage("Hack detected, waiting until the hack is over to execute this command.");
		TwitchGame.Instance.StartCoroutine(WaitForCall(new List<IRCMessage>(){ TwitchGame.Instance.callSend.Message }));
	}

	/// <name>Call All</name>
	/// <syntax>callall\ncallall force</syntax>
	/// <summary>Calls all unnamed commands in the queue. If force is specified, named commands are included too.</summary>
	[Command(@"callall( *force)?")]
	public static void CallAllQueuedCommands(string user, bool isWhisper, [Group(1)] bool force)
	{
		if (TwitchGame.Instance.CommandQueue.Count == 0)
		{
			IRCConnection.SendMessage($"{user}, the queue is empty.", user, !isWhisper);
			return;
		}

		// Take a copy of the list in case executing one of the commands modifies the command queue
		var allCommands = TwitchGame.Instance.CommandQueue.Where(item => force || item.Name == null).ToList();
		if (allCommands.Count == 0)
		{
			IRCConnection.SendMessage($"{user}, the queue only contains named commands. Use '!callall force' to call them.", user, !isWhisper);
			return;
		}

		TwitchGame.Instance.CommandQueue.RemoveAll(item => allCommands.Contains(item));
		TwitchGame.ModuleCameras?.SetNotes();
		List<IRCMessage> cmdsToExecute = new List<IRCMessage>();
		foreach (var call in allCommands)
		{
			IRCConnection.SendMessageFormat("Calling {0}: {1}", call.Message.UserNickName, call.Message.Text);
			cmdsToExecute.Add(call.Message);
		}
		DeleteCallInformation(true);
		if (TwitchGame.Instance.Bombs.Any(x => x.BackdoorHandleHack))
			IRCConnection.SendMessageFormat("Hack detected, waiting until the hack is over to execute {0} command{1}.", cmdsToExecute.Count == 1 ? "this" : "these", cmdsToExecute.Count == 1 ? string.Empty : "s");
		TwitchGame.Instance.StartCoroutine(WaitForCall(cmdsToExecute));
	}

	/// <name>Call Set</name>
	/// <syntax>callset [minimum]</syntax>
	/// <summary>Sets a minimum a number of times Call Command must be run for a command to be called.</summary>
	[Command(@"callset +(\d*)")]
	public static void CallSetCommand(string user, [Group(1)] int minimum)
	{
		if (minimum <= 0 || minimum >= 25)
		{
			IRCConnection.SendMessageFormat("@{0}, {1} is in invalid number of calls!", user, minimum);
			return;
		}

		TwitchGame.Instance.callsNeeded = minimum;
		DeleteCallInformation(true);
		IRCConnection.SendMessageFormat("Set minimum calls to {0}.", minimum);
	}

	/// <name>Call Count</name>
	/// <syntax>callcount</syntax>
	/// <summary>Displays the number of times that the Call Command has been run since a command was last called.</summary>
	[Command(@"callcount")]
	public static void CallCountCommand() => IRCConnection.SendMessageFormat("{0} out of {1} calls needed.", TwitchGame.Instance.CallingPlayers.Count, TwitchGame.Instance.callsNeeded);

	/// <name>Delete Call</name>
	/// <syntax>delcall [user]</syntax>
	/// <summary>Removes a user's call.</summary>
	/// <restriction>Mod</restriction>
	[Command(@"delcall +(.+)", AccessLevel.Mod, AccessLevel.Mod)]
	public static void DeleteQueuedPlayer([Group(1)] string callUser, string user)
	{
		callUser = callUser.FormatUsername();
		if (string.IsNullOrEmpty(callUser)) IRCConnection.SendMessageFormat("@{0}, please specify a call to remove!", user);
		else if (!TwitchGame.Instance.CallingPlayers.Keys.Contains(user)) IRCConnection.SendMessageFormat("@{0}, @{1} has not called!", user, callUser);
		else
		{
			TwitchGame.Instance.CallingPlayers.Remove(callUser);
			IRCConnection.SendMessageFormat("@{0}, removed @{1}'s call.", user, callUser);
			TwitchGame.Instance.CallUpdate(true);
		}
	}

	/// <name>Uncall</name>
	/// <syntax>uncall</syntax>
	/// <summary>Retracts your Call Command when multiple are required.</summary>
	[Command(@"uncall")]
	public static void RemoveCalledPlayer(string user)
	{
		if (!TwitchGame.Instance.CallingPlayers.Keys.Contains(user))
		{
			IRCConnection.SendMessageFormat("@{0}, you haven't called yet!", user);
			return;
		}
		TwitchGame.Instance.CallingPlayers.Remove(user);
		IRCConnection.SendMessageFormat("@{0}, your call has been removed.", user);
		TwitchGame.Instance.CallUpdate(true);
	}

	/// <name>Call Players</name>
	/// <syntax>callplayers</syntax>
	/// <summary>Lists the current players who have called when multiple are required.</summary>
	[Command(@"callplayers")]
	public static void ListCalledPlayers()
	{
		int totalCalls = TwitchGame.Instance.CallingPlayers.Count;
		if (totalCalls == 0)
		{
			IRCConnection.SendMessageFormat("No calls have been made.");
			return;
		}
		string[] __calls = TwitchGame.Instance.CallingPlayers.Values.ToArray();
		string[] __callPlayers = TwitchGame.Instance.CallingPlayers.Keys.ToArray();
		string builder = "";
		for (int j = 0; j < __calls.Length; j++) builder = builder + ((j == 0) ? "@" : ", @") + __callPlayers[j] + ": " + (string.IsNullOrEmpty(__calls[j]) ? "Next queued command" : __calls[j]);
		IRCConnection.SendMessageFormat("These players have already called: {0}", builder);
	}

	/// <name>Delete All Calls</name>
	/// <syntax>delcallall</syntax>
	/// <summary>Removes all calls.</summary>
	/// <restriction>Mod</restriction>
	[Command(@"delcallall", AccessLevel.Mod, AccessLevel.Mod)]
	public static void DeleteCallInformation(bool silent)
	{
		TwitchGame.Instance.CallingPlayers.Clear();
		TwitchGame.Instance.callWaiting = false;
		if (!silent) IRCConnection.SendMessageFormat("All call information cleared.");
	}

	/// <name>Set Multiplier</name>
	/// <syntax>setmultiplier [multiplier]</syntax>
	/// <summary>Sets the time mode multiplier.</summary>
	/// <restriction>Admin</restriction>
	[Command(@"setmultiplier +(\d*\.?\d+)", AccessLevel.Admin, AccessLevel.Admin)]
	public static void SetMultiplier([Group(1)] float multiplier) => OtherModes.SetMultiplier(multiplier);

	/// <name>Solve Bomb</name>
	/// <syntax>solvebomb</syntax>
	/// <summary>Solves the currently held bomb.</summary>
	/// <restriction>SuperUser</restriction>
	[Command(@"solvebomb", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void SolveBomb()
	{
		foreach (var bomb in TwitchGame.Instance.Bombs.Where(x => GameRoom.Instance.IsCurrentBomb(x.BombID)))
			bomb.StartCoroutine(bomb.KeepAlive());

		var modules = TwitchGame.Instance.Modules
			.Where(x => GameRoom.Instance.IsCurrentBomb(x.BombID))
			.OrderByDescending(module => module.Solver.ModInfo.moduleID.EqualsAny("cookieJars", "organizationModule", "forgetMeLater", "encryptedHangman", "SecurityCouncil", "GSAccessCodes"));
		foreach (var module in modules)
			if (!module.Solved)
				module.SolveSilently();
	}

	/// <name>Enable Claims</name>
	/// <syntax>enableclaims</syntax>
	/// <summary>Enables the ability to claim modules.</summary>
	/// <restriction>Admin</restriction>
	[Command(@"enableclaims", AccessLevel.Admin, AccessLevel.Admin)]
	public static void EnableClaims()
	{
		TwitchModule.ClaimsEnabled = true;
		IRCConnection.SendMessage("Claims have been enabled.");
	}

	/// <name>Disable Claims</name>
	/// <syntax>disableclaims</syntax>
	/// <summary>Disables the ability to claim modules.</summary>
	/// <restriction>Admin</restriction>
	[Command(@"disableclaims", AccessLevel.Admin, AccessLevel.Admin)]
	public static void DisableClaims()
	{
		TwitchModule.ClaimsEnabled = false;
		IRCConnection.SendMessage("Claims have been disabled.");
	}

	/// <name>Assign</name>
	/// <syntax>assign [user] [codes]</syntax>
	/// <summary>Assigns modules to a user based on their module codes.</summary>
	[Command(@"assign +(\S+) +(.+)")]
	public static void AssignModuleTo([Group(1)] string targetUser, [Group(2)] string queries, string user)
	{
		targetUser = targetUser.FormatUsername();
		if (TwitchPlaySettings.data.AnarchyMode)
		{
			IRCConnection.SendMessage($"{user}, assigning modules is not allowed in anarchy mode.");
			return;
		}

		var query = queries.SplitFull(' ', ',', ';');
		var denied = new List<string>();
		foreach (var module in TwitchGame.Instance.Modules.Where(m => !m.Solved && GameRoom.Instance.IsCurrentBomb(m.BombID) && query.Any(q => q.EqualsIgnoreCase(m.Code))).Take(TwitchPlaySettings.data.ModuleClaimLimit))
		{
			if ((module.PlayerName != user || module.ClaimQueue.All(q => q.UserNickname == targetUser)) && !UserAccess.HasAccess(user, AccessLevel.Mod, true))
				denied.Add(module.Code);
			else
				ModuleCommands.Assign(module, user, targetUser);
		}
		if (denied.Count == 1)
			IRCConnection.SendMessage($"{user}, since you’re not a moderator, {denied[0]} has not been reassigned.", user, false);
		else if (denied.Count > 1)
			IRCConnection.SendMessage($"{user}, since you’re not a moderator, {denied.Take(denied.Count - 1).Join(", ")} and {denied.Last()} have not been reassigned.", user, false);
	}

	/// <name>Bot Unclaim</name>
	/// <syntax>bot unclaim</syntax>
	/// <summary>Makes the bot unclaim any module it has claimed.</summary>
	/// <restriction>Mod</restriction>
	[Command(@"bot ?unclaim( ?all)?", AccessLevel.Mod, AccessLevel.Mod)]
	public static void BotUnclaim()
	{
		foreach (var module in TwitchGame.Instance.Modules)
			if (!module.Solved && module.PlayerName == IRCConnection.Instance.UserNickName && GameRoom.Instance.IsCurrentBomb(module.BombID))
				module.SetUnclaimed();
	}

	/// <name>Disable Interactive</name>
	/// <syntax>disableinteractive</syntax>
	/// <summary>Disables interactive mode on the cameras. As if the escape key hadn't been pressed.</summary>
	/// <restriction>Mod</restriction>
	[Command(@"disableinteractive", AccessLevel.Mod, AccessLevel.Mod)]
	public static void DisableInteractive() => TwitchGame.ModuleCameras.DisableInteractive();

	/// <name>Return To Setup</name>
	/// <syntax>returntosetup</syntax>
	/// <summary>Forces the game to return to the setup room.</summary>
	/// <restriction>Mod</restriction>
	[Command(@"(?:returntosetup|leave|exit)(?:room)?|return", AccessLevel.Mod, AccessLevel.Mod)]
	public static void ReturnToSetup() => SceneManager.Instance.ReturnToSetupState();

	/// <name>Enable Interactive Mode</name>
	/// <syntax>enableinteractivemode</syntax>
	/// <summary>Enables interactive mode, allowing the streamer to interact with the bomb.</summary>
	/// <restriction>Streamer</restriction>
	[Command(@"enableinteractivemode", AccessLevel.Streamer, AccessLevel.Streamer)]
	public static void EnableInteractiveMode()
	{
		IRCConnection.SendMessage("Interactive Mode Enabled");
		TwitchPlaySettings.data.EnableInteractiveMode = true;
		TwitchPlaySettings.WriteDataToFile();
		TwitchGame.EnableDisableInput();
	}

	/// <name>Disable Interactive Mode</name>
	/// <syntax>disableinteractivemode</syntax>
	/// <summary>Disables interactive mode, preventing the streamer from interacting with the bomb.</summary>
	/// <restriction>Streamer</restriction>
	[Command(@"disableinteractivemode", AccessLevel.Streamer, AccessLevel.Streamer)]
	public static void DisableInteractiveMode()
	{
		IRCConnection.SendMessage("Interactive Mode Disabled");
		TwitchPlaySettings.data.EnableInteractiveMode = false;
		TwitchPlaySettings.WriteDataToFile();
		TwitchGame.EnableDisableInput();
	}

	/// <name>Solve Unsupported Modules</name>
	/// <syntax>solveunsupportedmodules</syntax>
	/// <summary>Solves modules that aren't supported by TP.</summary>
	/// <restriction>Admin</restriction>
	[Command(@"solveunsupportedmodules", AccessLevel.Admin, AccessLevel.Admin)]
	public static void SolveUnsupportedModules()
	{
		IRCConnection.SendMessage("Solving unsupported modules.");
		TwitchModule.SolveUnsupportedModules();
	}

	/// <name>Solve Boss Modules</name>
	/// <syntax>solvebossmodules</syntax>
	/// <summary>Solves modules that depend on the solve count of the bomb or are considered boss modules according to the repository of manual pages.</summary>
	/// <restriction>SuperUser</restriction>
	[Command(@"solvebossmodules", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void SolveBossModules()
	{
		IRCConnection.SendMessage("Solving boss modules");
		TwitchGame.Instance.SolveBossModules();
	}

	/// <name>Custom Messages</name>
	/// <syntax>ttks\nttksleft\nttksright\ninfozen\nqhelp</syntax>
	/// <summary>These commands send a predefined message to chat. Streamers can choose their own messages but the ones mentioned here are included by default.</summary>
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

	/// <name>Show Mission Name</name>
	/// <syntax>status\nmission</syntax>
	/// <summary>View currently running mission name, if any.</summary>
	[Command(@"(?:status|mission)")]
	public static void Mission(string cmd, string user, bool isWhisper)
	{
		if (GameplayState.MissionToLoad.EqualsAny(Assets.Scripts.Missions.FreeplayMissionGenerator.FREEPLAY_MISSION_ID, ModMission.CUSTOM_MISSION_ID))
		{
			IRCConnection.SendMessage(TwitchPlaySettings.data.CurrentMissionNull, user, !isWhisper);
			return;
		}
		var missionTerm = SceneManager.Instance.GameplayState.Mission.DisplayNameTerm;
		string missionName = Localization.GetLocalizedString(missionTerm);
		string missionLink = UrlHelper.MissionLink(missionName);
		IRCConnection.SendMessage(TwitchPlaySettings.data.CurrentMission, user, !isWhisper, missionName, missionLink);
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
			.Where(module => queries.Any(q => module.HeaderText.ContainsIgnoreCase(q)) && GameRoom.Instance.IsCurrentBomb(module.BombID) && (predicate == null || predicate(module)) && !module.Hidden)
			.OrderByDescending(handle => queries.Any(q => handle.HeaderText.EqualsIgnoreCase(q)))
			.ThenBy(handle => handle.Solved)
			.ThenBy(handle => handle.PlayerName != null);

	private static void ShowClaimsOfUser(string targetUser, bool isWhisper, string ownedListMsg, string noOwnedMsg)
	{
		targetUser = targetUser.FormatUsername();
		var claimed = TwitchGame.Instance.Modules
			.Where(module => module.PlayerName != null && module.PlayerName.EqualsIgnoreCase(targetUser) && !module.Solved)
			.Select(module => string.Format(TwitchPlaySettings.data.OwnedModule, module.Code, module.HeaderText))
			.Shuffle()
			.ToList();
		if (claimed.Count > 0)
		{
			string newMessage = string.Format(ownedListMsg, targetUser, string.Join(", ", claimed.ToArray(), 0, Math.Min(claimed.Count, 5)));
			if (claimed.Count > 5)
				newMessage += $", and {claimed.Count - 5} more.";
			IRCConnection.SendMessage(newMessage, targetUser, !isWhisper);
		}
		else
			IRCConnection.SendMessage(string.Format(noOwnedMsg, targetUser), targetUser, !isWhisper);
	}

	// Makes sure that all called commands are not executed until you are done being hacked by Backdoor Hacking
	private static IEnumerator WaitForCall(List<IRCMessage> cmdsToExecute)
	{
		bool alreadyExecuting = calledCommands.Count != 0;
		calledCommands.AddRange(cmdsToExecute);
		if (alreadyExecuting) yield break;

		if (TwitchGame.Instance.Bombs.Any(x => x.BackdoorHandleHack)) {
			yield return new WaitUntil(() => TwitchGame.Instance.Bombs.All(x => !x.BackdoorHandleHack));
			IRCConnection.SendMessage("The hack is over, executing all commands held up due to the hack.");
		}
		foreach (IRCMessage m in calledCommands)
			IRCConnection.ReceiveMessage(m);
		calledCommands.Clear();
	}
	#endregion
}
