using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>Commands that can be run on a module.</summary>
static class ModuleCommands
{
	/// <name>Help</name>
	/// <syntax>help</syntax>
	/// <summary>Sends a message to chat with information on what commands you can use to solve the module.</summary>
	/// <restriction>SolvedAllowed</restriction>
	[Command(@"(?:help|manual)( +pdf)?"), SolvedAllowed]
	public static void Help(TwitchModule module, [Group(1)] bool pdf)
	{
		string manualType = pdf ? "pdf" : "html";

		string manualText = module.Solver.ManualCode;
		string helpText = string.IsNullOrEmpty(module.Solver.HelpMessage) ? string.Empty : string.Format(module.Solver.HelpMessage, module.Code, module.HeaderText);

		IRCConnection.SendMessage($"{module.HeaderText} : {helpText} : {UrlHelper.ManualFor(manualText, manualType, VanillaRuleModifier.GetModuleRuleSeed(module.Solver.ModInfo.moduleID) != 1)}");
	}

	/// <name>Player</name>
	/// <syntax>player</syntax>
	/// <summary>Tells you what user has the module claimed.</summary>
	/// <restriction>SolvedAllowed</restriction>
	[Command("player"), SolvedAllowed]
	public static void Player(TwitchModule module, string user) => IRCConnection.SendMessage(module.PlayerName != null
			? string.Format(TwitchPlaySettings.data.ModulePlayer, module.Code, module.PlayerName, module.HeaderText)
			: string.Format(TwitchPlaySettings.data.ModuleNotClaimed, user, module.Code, module.HeaderText));

	/// <name>Queue Flip</name>
	/// <syntax>queue flip</syntax>
	/// <summary>Queues the bomb to be flipped over when the module is solved.</summary>
	[Command("(?:bomb|queue) +(?:turn(?: +a?round)?|flip|spin)")]
	public static void BombTurnAround(TwitchModule module)
	{
		if (!module.Solver.TurnQueued)
		{
			module.Solver.TurnQueued = true;
			module.StartCoroutine(module.Solver.TurnBombOnSolve());
		}
		IRCConnection.SendMessageFormat(TwitchPlaySettings.data.TurnBombOnSolve, module.Code, module.HeaderText);
	}

	/// <name>Cancel Queued Flip</name>
	/// <syntax>cancel queue flip</syntax>
	/// <summary>Cancels a previously queued flip when the module was solved.</summary>
	[Command("cancel +(?:bomb|queue) +(?:turn(?: +a?round)?|flip|spin)")]
	public static void BombTurnAroundCancel(TwitchModule module)
	{
		module.Solver.TurnQueued = false;
		IRCConnection.SendMessageFormat(TwitchPlaySettings.data.CancelBombTurn, module.Code, module.HeaderText);
	}

	/// <name>Claim</name>
	/// <syntax>claim</syntax>
	/// <summary>Claims the module or queues to claim it if it's already claimed.</summary>
	[Command("claim")]
	public static void Claim(TwitchModule module, string user, bool isWhisper) => ClaimViewOrPin(module, user, isWhisper, view: false, pin: false);

	/// <name>Unview</name>
	/// <syntax>unview</syntax>
	/// <summary>Stops viewing the module with a camera.</summary>
	[Command("unview")]
	public static void Unview(TwitchModule module) => TwitchGame.ModuleCameras?.UnviewModule(module);

	/// <name>View / ViewPin</name>
	/// <syntax>view\nviewpin</syntax>
	/// <summary>Puts the module into a dedicated view. viewpin requires either moderator+ or the module allows pinning at any time.</summary>
	[Command("(view(?: *pin)?|pin *view)")]
	public static void View(TwitchModule module, string user, [Group(1)] string cmd) => module.ViewPin(user, cmd.ContainsIgnoreCase("p"));

	public static IEnumerator Show(TwitchModule module, object yield)
	{
		bool select = !module.BombComponent.GetModuleID().EqualsAny("lookLookAway");
		IEnumerator focusCoroutine = module.Bomb.Focus(module.Selectable, module.FocusDistance, module.FrontFace, select);
		while (focusCoroutine.MoveNext())
			yield return focusCoroutine.Current;

		yield return new WaitForSeconds(0.5f);
		yield return yield is float delay ? new WaitForSecondsWithCancel(delay, false, module.Solver) : yield;
		if (CoroutineCanceller.ShouldCancel)
		{
			module.StartCoroutine(module.Bomb.Defocus(module.Selectable, module.FrontFace, select));
			yield break;
		}
		IEnumerator defocusCoroutine = module.Bomb.Defocus(module.Selectable, module.FrontFace, select);
		while (defocusCoroutine.MoveNext())
			yield return defocusCoroutine.Current;

		yield return new WaitForSeconds(0.5f);
	}

	/// <name>Solve</name>
	/// <syntax>solve</syntax>
	/// <summary>Forces a module to solve itself. Requires either Admin rank or the module to not be supported on TP.</summary>
	[Command("solve")]
	public static void Solve(TwitchModule module, string user)
	{
		if (
			// Admins can always auto-solve a module.
			UserAccess.HasAccess(user, AccessLevel.Admin, true) ||
			// Unsupported modules can always be auto-solved
			(module.Unsupported || module.Solver.GetType() == typeof(UnsupportedModComponentSolver))
		)
			module.Solver.SolveModule($"A module ({module.HeaderText}) is being automatically solved.");
	}

	/// <name>Votesolve</name>
	/// <syntax>votesolve</syntax>
	/// <summary>Starts a vote about solving the module</summary>
	[Command("votesolve")]
	public static void VoteSolve(TwitchModule module, string user) => Votes.StartVote(user, VoteTypes.Solve, module);

	/// <name>Claim View Pin</name>
	/// <syntax>claim view pin\ncvp</syntax>
	/// <summary>Claims, views and pins a module. You can remove one of three actions as well. (e.g. claim view)</summary>
	[Command("solve")]
	[Command(@"(claim view|view claim|claimview|viewclaim|cv|vc|claim view pin|view pin claim|claimviewpin|viewpinclaim|cvp|vpc)")]
	public static void ClaimViewPin(TwitchModule module, string user, bool isWhisper, [Group(1)] string cmd) => ClaimViewOrPin(module, user, isWhisper, view: true, pin: cmd.Contains("p"));

	/// <name>Unclaim</name>
	/// <syntax>unclaim\nunclaim unview</syntax>
	/// <summary>Removes your claim on a module or your queued claim. unclaim unview also unviews the module.</summary>
	[Command("(unclaim|un?c|unclaim unview|unview unclaim|unclaimview|unviewclaim|uncv|unvc)")]
	public static void Unclaim(TwitchModule module, string user, [Group(1)] string cmd)
	{
		if (module.Solved)
		{
			IRCConnection.SendMessageFormat(TwitchPlaySettings.data.AlreadySolved, module.Code, module.PlayerName, user, module.HeaderText);
			return;
		}

		// If module is already unclaimed, just remove from claim queue
		if (module.PlayerName == null)
		{
			module.RemoveFromClaimQueue(user);
			return;
		}

		// Error if a non-mod tries to unclaim someone else’s module
		if (!UserAccess.HasAccess(user, AccessLevel.Mod, true) && module.PlayerName != user)
		{
			IRCConnection.SendMessage($"{user}, module {module.Code} ({module.HeaderText}) is not claimed by you.");
			return;
		}

		module.SetUnclaimed();
		if (cmd.Contains("v"))
			TwitchGame.ModuleCameras?.UnviewModule(module);
	}

	/// <name>Solved</name>
	/// <syntax>solved</syntax>
	/// <summary>Changes the color of a module's ID tag to green to mark it as "solved".</summary>
	/// <restriction>Mod</restriction>
	[Command(@"solved", AccessLevel.Mod, AccessLevel.Mod)]
	public static void Solved(TwitchModule module, string user)
	{
		module.SetBannerColor(module.SolvedBackgroundColor);
		module.PlayerName = null;
		IRCConnection.SendMessageFormat(TwitchPlaySettings.data.ModuleReady, module.Code, user, module.HeaderText);
	}

	/// <name>Assign</name>
	/// <syntax>assign [username]</syntax>
	/// <summary>Assigns a module to another user. Usually requires mod rank but if you are claiming the module you can attempt to assign it to another user.</summary>
	[Command(@"assign +(.+)")]
	public static void Assign(TwitchModule module, string user, [Group(1)] string targetUser)
	{
		targetUser = targetUser.FormatUsername();
		if (module.PlayerName == targetUser)
		{
			IRCConnection.SendMessage($"{user}, the module is already assigned to {targetUser}.");
			return;
		}

		if (TwitchPlaySettings.data.AnarchyMode)
		{
			IRCConnection.SendMessage($"{user}, assigning modules is not allowed in anarchy mode.");
			return;
		}

		if (!UserAccess.HasAccess(user, AccessLevel.Mod, true))
		{
			if (module.PlayerName != user || !module.ClaimQueue.Any(q => q.UserNickname == targetUser))
			{
				IRCConnection.SendMessage($"{user}, {module.Code} can only be reassigned if you have it claimed and the other user is in its claim queue.");
				return;
			}
			if (TwitchGame.Instance.Modules.Count(md => !md.Solved && targetUser.EqualsIgnoreCase(md.PlayerName)) >= TwitchPlaySettings.data.ModuleClaimLimit)
			{
				IRCConnection.SendMessage($"{user}, {module.Code} cannot be reassigned because it would take the other user above their claim limit.");
				return;
			}
		}

		if (module.TakeInProgress != null)
		{
			module.StopCoroutine(module.TakeInProgress);
			module.TakeInProgress = null;
			module.TakeUser = null;
		}

		module.SetClaimedBy(targetUser);
		IRCConnection.SendMessageFormat(TwitchPlaySettings.data.AssignModule, module.Code, module.PlayerName, user, module.HeaderText);
	}

	/// <name>Take</name>
	/// <syntax>take</syntax>
	/// <summary>Request that the current user releases their claim on the module.</summary>
	[Command(@"take")]
	public static void Take(TwitchModule module, string user, bool isWhisper)
	{
		if (isWhisper)
			IRCConnection.SendMessage($"@{user}, taking modules is not allowed in whispers.");
		else if (TwitchPlaySettings.data.AnarchyMode)
			IRCConnection.SendMessage($"@{user}, taking modules is not allowed in anarchy mode.");

		// Module is already claimed by the same user
		else if (module.PlayerName == user)
			IRCConnection.SendMessageFormat(TwitchPlaySettings.data.ModuleAlreadyOwned, user, module.Code, module.HeaderText);

		// Module is not claimed at all: just claim it
		else if (module.PlayerName == null)
			IRCConnection.SendMessage(module.TryClaim(user).Message);

		// If there's already a queued command for the module, it could be problematic to take it.
		// However there still may be reasons to take it anyway, so ask for confirmation.
		else if (!module.TakeConfirmationShown && TwitchGame.Instance.CommandQueue.Any(c => c.Message.Text.StartsWith($"!{module.Code} ")))
		{
			IRCConnection.SendMessage($"@{user}, the module you are trying to take has a command in the queue already. Please use '!{module.Code} take' again to confirm you want to do this.");
			module.TakeConfirmationShown = true;
		}

		// Attempt to take over from another user
		else
		{
			module.TakeConfirmationShown = false;

			module.AddToClaimQueue(user);
			if (module.TakeInProgress != null)
				IRCConnection.SendMessageFormat(TwitchPlaySettings.data.TakeInProgress, user, module.Code, module.HeaderText);
			else
			{
				IRCConnection.SendMessageFormat(TwitchPlaySettings.data.TakeModule, module.PlayerName, user, module.Code, module.HeaderText);
				module.TakeUser = user;
				module.TakeInProgress = module.StartCoroutine(module.ProcessTakeover());
			}
		}
	}

	/// <name>Mine</name>
	/// <syntax>mine</syntax>
	/// <summary>Indicates that you are still working on the module. Only the person who has the claim can cancel the take.</summary>
	[Command(@"mine")]
	public static void Mine(TwitchModule module, string user, bool isWhisper)
	{
		if (isWhisper)
		{
			IRCConnection.SendMessage($"@{user}, using mine on modules is not allowed in whispers.", user, false);
			return;
		}

		// The module belongs to this user and there’s a takeover attempt in progress: cancel the takeover attempt
		if (module.PlayerName == user && module.TakeInProgress != null)
		{
			IRCConnection.SendMessageFormat(TwitchPlaySettings.data.ModuleIsMine, module.PlayerName, module.Code, module.HeaderText);
			module.StopCoroutine(module.TakeInProgress);
			module.TakeInProgress = null;
			module.TakeUser = null;
			module.TakeConfirmationShown = false;
		}

		// The module isn’t claimed: just claim it
		else if (module.PlayerName == null)
			IRCConnection.SendMessage(module.TryClaim(user).Message);

		// Someone else has a claim on the module
		else if (module.PlayerName != user)
			IRCConnection.SendMessageFormat(TwitchPlaySettings.data.AlreadyClaimed, module.Code, module.PlayerName, user, module.HeaderText);

		// If the user has a claim on the module but there’s no takeover attempt, just ignore this command
	}

	/// <name>Cancel Take</name>
	/// <syntax>canceltake</syntax>
	/// <summary>Cancels a take attempt on the module. Can only be run by the person who has the claim or a mod.</summary>
	[Command(@"canceltake")]
	public static void CancelTake(TwitchModule module, string user, bool isWhisper)
	{
		if (module.TakeInProgress == null)
		{
			IRCConnection.SendMessage($"@{user}, there are no takeover attempts on module {module.Code} ({module.HeaderText}).", user, !isWhisper);
			return;
		}

		if (!UserAccess.HasAccess(user, AccessLevel.Mod, true) && module.TakeUser != user)
		{
			IRCConnection.SendMessage($"@{user}, if you’re not a mod, you can only cancel your own takeover attempts.");
			return;
		}

		// Cancel the takeover attempt
		IRCConnection.SendMessage($"{module.TakeUser}’s takeover of module {module.Code} ({module.HeaderText}) was cancelled by {user}.");
		module.StopCoroutine(module.TakeInProgress);
		module.TakeInProgress = null;
		module.TakeUser = null;
		module.TakeConfirmationShown = false;
	}

	/// <name>Points</name>
	/// <syntax>points</syntax>
	/// <summary>Tells you how many points a module is worth.</summary>
	/// <restrictions>SolvedAllowed</restrictions>
	[Command(@"(points|score)"), SolvedAllowed]
	public static void Points(TwitchModule module) => IRCConnection.SendMessage($"{module.HeaderText} ({module.Code}) score: {module.Solver.ModInfo.ScoreExplanation}");

	/// <name>Mark</name>
	/// <syntax>mark</syntax>
	/// <summary>Changes the color of a module's ID tag to black.</summary>
	/// <restriction>Mod</restriction>
	[Command(@"mark", AccessLevel.Mod, AccessLevel.Mod)]
	public static void Mark(TwitchModule module) => module.SetBannerColor(module.MarkedBackgroundColor);

	/// <name>Unmark</name>
	/// <syntax>unmark</syntax>
	/// <summary>Returns the color of a module's ID back to default.</summary>
	/// <restriction>Mod</restriction>
	[Command(@"unmark", AccessLevel.Mod, AccessLevel.Mod)]
	public static void Unmark(TwitchModule module) => module.SetBannerColor(module.Claimed ? module.ClaimedBackgroundColour : module.unclaimedBackgroundColor);

	public static IEnumerator Zoom(TwitchModule module, SuperZoomData zoomData, object yield)
	{
		float alpha = module.CanvasGroupMultiDecker.alpha;
		module.CanvasGroupMultiDecker.alpha = 0.0f;

		var zoomCoroutine = TwitchGame.ModuleCameras?.ZoomCamera(module, zoomData, 1);
		if (zoomCoroutine != null)
			while (zoomCoroutine.MoveNext())
				yield return zoomCoroutine.Current;

		yield return yield is float delay ? new WaitForSecondsWithCancel(delay, false, module.Solver) : yield;

		if (CoroutineCanceller.ShouldCancel)
		{
			module.CanvasGroupMultiDecker.alpha = alpha;
			module.StartCoroutine(TwitchGame.ModuleCameras?.UnzoomCamera(module, zoomData, 0));
			yield break;
		}

		module.CanvasGroupMultiDecker.alpha = alpha;

		var unzoomCoroutine = TwitchGame.ModuleCameras?.UnzoomCamera(module, zoomData, 1);
		if (unzoomCoroutine != null)
			while (unzoomCoroutine.MoveNext())
				yield return unzoomCoroutine.Current;
	}

	public static IEnumerator Tilt(TwitchModule module, object yield, string direction, float tiltAngle)
	{
		float easeCubic(float t) { return 3 * t * t - 2 * t * t * t; }

		Dictionary<string[], int> directionNames = new Dictionary<string[], int>()
		{
			{ new[] { "up", "u", "top", "t" }, 0 },
			{ new[] { "upright", "rightup", "ur", "ru", "topright", "righttop", "tr", "rt" }, 45 },
			{ new[] { "right", "r" }, 90 },
			{ new[] { "downright", "rightdown", "dr", "rd", "bottomright", "rightbottom", "br", "rb" }, 135 },
			{ new[] { "down", "d", "bottom", "b" }, 180 },
			{ new[] { "downleft", "leftdown", "dl", "ld", "bottomleft", "leftbottom", "bl", "lb" }, 255 },
			{ new[] { "left", "l" }, 270 },
			{ new[] { "upleft", "leftup", "ul", "lu", "topleft", "lefttop", "tl", "lt" }, 315 },
		};

		var targetRotation = 180;
		if (!string.IsNullOrEmpty(direction))
		{
			var nameAngle = directionNames.Where(pair => pair.Key.Contains(direction)).Select(pair => pair.Value);
			if (nameAngle.Any())
			{
				targetRotation = nameAngle.First();
			}
			else if (int.TryParse(direction, out int directionAngle))
			{
				targetRotation = directionAngle;
			}
			else
			{
				yield break;
			}
		}

		IEnumerator focusCoroutine = module.Bomb.Focus(module.Selectable, module.FocusDistance, module.FrontFace, false);
		while (focusCoroutine.MoveNext())
			yield return focusCoroutine.Current;

		float moduleAlpha = module.CanvasGroupMultiDecker.alpha;
		if (moduleAlpha != 0.0f)
			module.CanvasGroupMultiDecker.alpha = 0.0f;

		yield return new WaitForSeconds(0.5f);

		var targetAngle = Quaternion.Euler(new Vector3(-Mathf.Cos(targetRotation * Mathf.Deg2Rad), 0, Mathf.Sin(targetRotation * Mathf.Deg2Rad)) * (module.FrontFace ? tiltAngle : -tiltAngle));
		foreach (float alpha in 1f.TimedAnimation())
		{
			var lerp = Quaternion.Lerp(Quaternion.identity, targetAngle, easeCubic(alpha));
			var bombLerp = module.FrontFace ? lerp : Quaternion.Euler(Vector3.Scale(lerp.eulerAngles, new Vector3(1, 1, -1)));
			module.Bomb.RotateByLocalQuaternion(bombLerp);
			TwitchBomb.RotateCameraByLocalQuaternion(module.BombComponent.gameObject, lerp);
			yield return null;
		}

		yield return yield is float delay ? new WaitForSecondsWithCancel(delay, false, module.Solver) : yield;

		if (CoroutineCanceller.ShouldCancel)
		{
			var angle = Quaternion.identity;
			var bombAngle = module.FrontFace ? angle : Quaternion.Euler(Vector3.Scale(angle.eulerAngles, new Vector3(1, 1, -1)));
			module.Bomb.RotateByLocalQuaternion(bombAngle);
			TwitchBomb.RotateCameraByLocalQuaternion(module.BombComponent.gameObject, angle);
			module.StartCoroutine(module.Bomb.Defocus(module.Selectable, module.FrontFace, false));
			if (moduleAlpha != 0.0f)
				module.CanvasGroupMultiDecker.alpha = moduleAlpha;
			yield break;
		}

		foreach (float alpha in 1f.TimedAnimation())
		{
			var lerp = Quaternion.Lerp(targetAngle, Quaternion.identity, easeCubic(alpha));
			var bombLerp = module.FrontFace ? lerp : Quaternion.Euler(Vector3.Scale(lerp.eulerAngles, new Vector3(1, 1, -1)));
			module.Bomb.RotateByLocalQuaternion(bombLerp);
			TwitchBomb.RotateCameraByLocalQuaternion(module.BombComponent.gameObject, lerp);
			yield return null;
		}

		if (moduleAlpha != 0.0f)
			module.CanvasGroupMultiDecker.alpha = moduleAlpha;

		IEnumerator defocusCoroutine = module.Bomb.Defocus(module.Selectable, module.FrontFace, false);
		while (defocusCoroutine.MoveNext())
			yield return defocusCoroutine.Current;

		yield return new WaitForSeconds(0.5f);
	}

	/// <name>Zoom, Superzoom, Show and Tilt</name>
	/// <syntax>zoom (duration) (command)\nsuperzoom (factor) (x) (y) (duration) (command)\ntilt (direction) (angle) (command)\nshow</syntax>
	/// <summary>Zooms into a module for (duration) seconds. (command) allows you to send a command to the module while it's zooming.
	/// Superzoom allows you more control over the zoom. (factor) controls how much it's zoomed in with 2 being a 2x zoom. (x) and (y) controls where the camera points with (0, 0) and (1, 1) being bottom left and top right respectively.
	/// Tilt will tilt the camera around the module in a direction so you can get better angle to look at the module. (direction) can be up, right, down or left and combinations like upleft, or any number where 0 is the top of the module and goes clockwise. (angle) is the tilt angle and can be any number between 0 to 90.
	/// Show will select the module on the bomb.
	/// Zoom and Tilt or Superzoom and Tilt or Zoom and Show or Superzoom and Show can be put back to back to do both at the same time.
	/// </summary>
	[Command(null)]
	public static IEnumerator DefaultCommand(TwitchModule module, string user, string cmd)
	{
		if (((Votes.Active && Votes.CurrentVoteType == VoteTypes.Solve && Votes.voteModule == module) || module.Votesolving) && !TwitchPlaySettings.data.AnarchyMode)
		{
			IRCConnection.SendMessage($"Sorry @{user}, the module you are trying to interact with is being votesolved.");
			yield break;
		}
		if (cmd.RegexMatch(out Match match, @"(?:(?<zoom>zoom *(?<time>\d*\.?\d+)?)|(?<superzoom>superzoom *(?<factor>\d*\.?\d+) *(?<x>\d*\.?\d+)? *(?<y>\d*\.?\d+)? *(?<stime>\d*\.?\d+)?))? *(?:(?<tilt>tilt *(?<direction>[uptobmdwnlefrigh]+|-?\d+)? *(?<angle>\d*\.?\d+)?)|(?<show>show)?)? *(?:send *to *module)? *(?<command>.+)?"))
		{
			var groups = match.Groups;
			var timed = groups["time"].Success || groups["stime"].Success;
			var zooming = groups["zoom"].Success || groups["superzoom"].Success;
			var tilt = groups["tilt"].Success;
			var show = groups["show"].Success;
			var command = groups["command"].Success;

			if (!timed && !zooming && !command && show)
			{
				yield return Show(module, 0.5);
				yield break;
			}
			// Either a zoom, show or tilt needs to take place otherwise, we should let the command run normally.
			if (zooming || tilt || show)
			{
				MusicPlayer musicPlayer = null;
				float delay = 2;
				if (timed)
				{
					delay = groups["time"].Value.TryParseFloat() ?? groups["stime"].Value.TryParseFloat() ?? 2;
					delay = Math.Max(2, delay);
				}

				List<object> yields = new List<object>();
				if (command) yields.Add(RunModuleCommand(module, user, groups["command"].Value));
				if (timed || !command) yields.Add(new WaitForSecondsWithCancel(delay, false, module.Solver));
				IEnumerator toYield = yields.GetEnumerator();

				IEnumerator routine = Show(module, toYield);
				if (tilt)
				{
					var tiltAngle = Mathf.Min(groups["angle"].Value.TryParseFloat() ?? 60, 90);
					routine = Tilt(module, toYield, groups["direction"].Value.ToLowerInvariant(), tiltAngle);
				}

				if (zooming)
				{
					var zoomData = new SuperZoomData(
						groups["factor"].Value.TryParseFloat() ?? 1,
						groups["x"].Value.TryParseFloat() ?? 0.5f,
						groups["y"].Value.TryParseFloat() ?? 0.5f
					);
					routine = Zoom(module, zoomData, routine ?? toYield);
				}

				if (delay >= 15)
					musicPlayer = MusicPlayer.StartRandomMusic();

				yield return routine;
				if (CoroutineCanceller.ShouldCancel)
				{
					CoroutineCanceller.ResetCancel();
					IRCConnection.SendMessage($"Sorry @{user}, your request to hold up the bomb for {delay} seconds has been cut short.");
				}

				if (musicPlayer != null)
					musicPlayer.StopMusic();

				yield break;
			}
		}

		yield return RunModuleCommand(module, user, cmd);
	}

	private static IEnumerator RunModuleCommand(TwitchModule module, string user, string cmd)
	{
		if (module.Solver == null)
			yield break;

		if (module.Solved && !TwitchPlaySettings.data.AnarchyMode)
		{
			IRCConnection.SendMessageFormat(TwitchPlaySettings.data.AlreadySolved, module.Code, module.PlayerName, user, module.HeaderText);
			yield break;
		}

		if (module.Bomb.BackdoorComponent != null && module.Bomb.BackdoorComponent.GetValue<bool>("BeingHacked") && module.BombComponent.GetModuleDisplayName() != "Backdoor Hacking")
		{
			IRCConnection.SendMessageFormat(TwitchPlaySettings.data.BackdoorHackingBlock, module.Code, user, module.HeaderText);
			yield break;
		}

		Transform tsLight = module.BombComponent.StatusLightParent?.transform.Find("statusLight(Clone)").Find("Component_LED_ERROR(Clone)");
		if (tsLight != null && tsLight.gameObject.activeSelf)
		{
			IRCConnection.SendMessageFormat(TwitchPlaySettings.data.TechSupportBlock, module.Code, user, module.HeaderText);
			yield break;
		}

		// We’re allowed to interact with this module if either:
		if (
			// the module is unclaimed;
			module.PlayerName == null ||
			// the module is claimed by the player;
			module.PlayerName == user ||
			// anarchy mode is on;
			TwitchPlaySettings.data.AnarchyMode ||
			// there is less than X time left on the clock;
			module.Bomb.CurrentTimer <= TwitchPlaySettings.data.MinTimeLeftForClaims ||
			// there are only X unsolved modules left.
			TwitchGame.Instance.Modules.Count(x => !x.Solved && GameRoom.Instance.IsCurrentBomb(x.BombID)) < TwitchPlaySettings.data.MinUnsolvedModulesLeftForClaims
		)
		{
			var response = module.Solver.RespondToCommand(user, cmd);
			while (response.MoveNext())
				yield return response.Current;

			module.Solver.EnableAnarchyStrike();
		}
		else
			IRCConnection.SendMessageFormat(TwitchPlaySettings.data.AlreadyClaimed, module.Code, module.PlayerName, user, module.HeaderText);
	}

	#region Private methods
	private static void ClaimViewOrPin(TwitchModule module, string user, bool isWhisper, bool view, bool pin)
	{
		if (isWhisper)
		{
			IRCConnection.SendMessage($"@{user}, claiming modules is not allowed in whispers.", user, false);
			return;
		}

		IRCConnection.SendMessage(module.TryClaim(user, view, pin).Message);
	}
	#endregion
}

public struct SuperZoomData
{
	public float factor;
	public float x;
	public float y;

	public SuperZoomData(float factor = 1, float x = 0.5f, float y = 0.5f)
	{
		this.factor = Math.Max(factor, 0.1f);
		this.x = Math.Max(Math.Min(x, 1), 0);
		this.y = Math.Max(Math.Min(y, 1), 0);
	}
}