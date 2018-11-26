using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

static class ModuleCommands
{
	[Command(@"(?:help|manual)( +pdf)?"), SolvedAllowed]
	public static void Help(TwitchModule module, [Group(1)] bool pdf)
	{
		string manualText = null;
		string manualType = "html";
		if (pdf)
			manualType = "pdf";

		manualText = string.IsNullOrEmpty(module.Solver.ManualCode) ? module.HeaderText : module.Solver.ManualCode;
		var helpText = string.Format(module.Solver.HelpMessage, module.Code, module.HeaderText);

		IRCConnection.SendMessage(Regex.IsMatch(manualText, @"^https?://", RegexOptions.IgnoreCase)
			? $"{module.HeaderText} : {helpText} : {manualText}"
			: $"{module.HeaderText} : {helpText} : {UrlHelper.Instance.ManualFor(manualText, manualType, VanillaRuleModifier.GetModuleRuleSeed(module.Solver.ModInfo.moduleID) != 1)}");
	}

	[Command("player"), SolvedAllowed]
	public static void Player(TwitchModule module, string user) => IRCConnection.SendMessage(module.PlayerName != null
			? string.Format(TwitchPlaySettings.data.ModulePlayer, module.Code, module.PlayerName, module.HeaderText)
			: string.Format(TwitchPlaySettings.data.ModuleNotClaimed, user, module.Code, module.HeaderText));

	[Command("(?:bomb|queue) +(?:turn(?: +a?round)?|flip|spin)"), SolvedAllowed]
	public static void BombTurnAround(TwitchModule module)
	{
		if (!module.Solver.TurnQueued)
		{
			module.Solver.TurnQueued = true;
			module.StartCoroutine(module.Solver.TurnBombOnSolve());
		}
		IRCConnection.SendMessage(TwitchPlaySettings.data.TurnBombOnSolve, module.Code, module.HeaderText);
	}

	[Command("cancel +(?:bomb|queue) +(?:turn(?: +a?round)?|flip|spin)"), SolvedAllowed]
	public static void BombTurnAroundCancel(TwitchModule module)
	{
		module.Solver.TurnQueued = false;
		IRCConnection.SendMessage(TwitchPlaySettings.data.CancelBombTurn, module.Code, module.HeaderText);
	}

	[Command("claim")]
	public static void Claim(TwitchModule module, string user, bool isWhisper) => ClaimViewOrPin(module, user, isWhisper, view: false, pin: false);

	[Command("unview")]
	public static void Unview(TwitchModule module) => TwitchGame.ModuleCameras?.UnviewModule(module);

	[Command("(view(?: *pin)?|pin *view)")]
	public static void View(TwitchModule module, string user, [Group(1)] string cmd) => module.ViewPin(user, cmd.ContainsIgnoreCase("p"));

	[Command("show")]
	public static IEnumerator Show(TwitchModule module)
	{
		IEnumerator focusCoroutine = module.Bomb.Focus(module.Selectable, module.FocusDistance, module.FrontFace);
		while (focusCoroutine.MoveNext())
			yield return focusCoroutine.Current;

		yield return new WaitForSeconds(0.5f);

		IEnumerator defocusCoroutine = module.Bomb.Defocus(module.Selectable, module.FrontFace);
		while (defocusCoroutine.MoveNext())
			yield return defocusCoroutine.Current;

		yield return new WaitForSeconds(0.5f);
	}

	[Command("solve")]
	public static void Solve(TwitchModule module, string user)
	{
		if (
			// Admins can always auto-solve a module.
			UserAccess.HasAccess(user, AccessLevel.Admin, true) ||
			// Unsupported modules can always be auto-solved
			(module.Unsupported && module.Solver.GetType() == typeof(UnsupportedModComponentSolver))
		)
			module.Solver.SolveModule($"A module ({module.HeaderText}) is being automatically solved.");
	}

	[Command(@"(claim view|view claim|claimview|viewclaim|cv|vc|claim view pin|view pin claim|claimviewpin|viewpinclaim|cvp|vpc)")]
	public static void ClaimViewPin(TwitchModule module, string user, bool isWhisper, [Group(1)] string cmd) => ClaimViewOrPin(module, user, isWhisper, view: true, pin: cmd.Contains("p"));

	[Command("(unclaim|release|unclaim unview|unview unclaim|unclaimview|unviewclaim|uncv|unvc)")]
	public static void Unclaim(TwitchModule module, string user, [Group(1)] string cmd)
	{
		var result = module.UnclaimModule(user);
		// If UnclaimModule responds with a null message, someone tried to unclaim a module that no one has claimed but they were waiting to claim.
		// It's a valid command and they were removed from the queue but no message is sent.
		if (result.Second != null)
			IRCConnection.SendMessage(result.Second);

		if (result.First && cmd.Contains("v"))
			TwitchGame.ModuleCameras?.UnviewModule(module);
	}

	[Command(@"solved", AccessLevel.Mod, AccessLevel.Mod)]
	public static void Solved(TwitchModule module, string user)
	{
		module.SetBannerColor(module.SolvedBackgroundColor);
		module.PlayerName = null;
		IRCConnection.SendMessage(TwitchPlaySettings.data.ModuleReady, module.Code, user, module.HeaderText);
	}

	[Command(@"assign +(.+)", AccessLevel.Mod, AccessLevel.Mod)]
	public static void Assign(TwitchModule module, string user, [Group(1)] string targetUser)
	{
		if (TwitchPlaySettings.data.AnarchyMode)
		{
			IRCConnection.SendMessage("Sorry {0}, assigning modules is not allowed in anarchy mode.", user);
			return;
		}

		if (module.TakeInProgress != null)
		{
			module.StopCoroutine(module.TakeInProgress);
			module.TakeInProgress = null;
		}

		module.PlayerName = targetUser;
		module.RemoveFromClaimQueue(user);
		module.CanClaimNow(user, true, true);
		module.SetBannerColor(module.ClaimedBackgroundColour);
		IRCConnection.SendMessage(TwitchPlaySettings.data.AssignModule, module.Code, module.PlayerName, user, module.HeaderText);
	}

	[Command(@"take")]
	public static void Take(TwitchModule module, string user, bool isWhisper)
	{
		if (isWhisper)
			IRCConnection.SendMessage("Sorry {0}, taking modules is not allowed in whispers.", user);
		else if (TwitchPlaySettings.data.AnarchyMode)
			IRCConnection.SendMessage("Sorry {0}, taking modules is not allowed in anarchy mode.", user);

		// Attempt to take over from another user
		else if (module.PlayerName != null && user != module.PlayerName)
		{
			module.AddToClaimQueue(user);
			if (module.TakeInProgress == null)
			{
				IRCConnection.SendMessage(TwitchPlaySettings.data.TakeModule, module.PlayerName, user, module.Code, module.HeaderText);
				module.TakeInProgress = module.TakeModule();
				module.StartCoroutine(module.TakeInProgress);
			}
			else
				IRCConnection.SendMessage(TwitchPlaySettings.data.TakeInProgress, user, module.Code, module.HeaderText);
		}

		// Module is already claimed by the same user
		else if (module.PlayerName != null)
		{
			if (!module.PlayerName.Equals(user))
				module.AddToClaimQueue(user);
			IRCConnection.SendMessage(TwitchPlaySettings.data.ModuleAlreadyOwned, user, module.Code, module.HeaderText);
		}

		// Module is not claimed at all: just claim it
		else
			IRCConnection.SendMessage(module.ClaimModule(user).Second);
	}

	[Command(@"mine")]
	public static void Mine(TwitchModule module, string user, bool isWhisper)
	{
		if (isWhisper)
		{
			IRCConnection.SendMessage($"Sorry {user}, using mine on modules is not allowed in whispers", user, false);
			return;
		}

		// The module belongs to this user and there’s a takeover attempt in progress: cancel the takeover attempt
		if (module.PlayerName == user && module.TakeInProgress != null)
		{
			IRCConnection.SendMessage(TwitchPlaySettings.data.ModuleIsMine, module.PlayerName, module.Code, module.HeaderText);
			module.StopCoroutine(module.TakeInProgress);
			module.TakeInProgress = null;
		}

		// The module isn’t claimed: just claim it
		else if (module.PlayerName == null)
			IRCConnection.SendMessage(module.ClaimModule(user).Second);

		// Someone else has a claim on the module
		else if (module.PlayerName != user)
			IRCConnection.SendMessage(TwitchPlaySettings.data.AlreadyClaimed, module.Code, module.PlayerName, user, module.HeaderText);

		// If the user has a claim on the module but there’s no takeover attempt, just ignore this command
	}

	[Command(@"canceltake", AccessLevel.Mod, AccessLevel.Mod)]
	public static void CancelTake(TwitchModule module, string user, bool isWhisper)
	{
		// cancel the takeover attempt if there is one
		if (module.TakeInProgress != null)
		{
			IRCConnection.SendChatMessage(
				$"The takeover attempt on module {module.Code} ({module.HeaderText}) was manually cancelled by {user}");
			module.StopCoroutine(module.TakeInProgress);
			module.TakeInProgress = null;
		}
		else
			IRCConnection.SendMessage("There are no takeover attempts on this module", user, !isWhisper);
	}

	[Command(@"(points|score)")]
	public static void Points(TwitchModule module) => IRCConnection.SendMessage("{0} ({1}) current score: {2}", module.HeaderText, module.Code, module.Solver.ModInfo.moduleScore);

	[Command(@"mark", AccessLevel.Mod, AccessLevel.Mod)]
	public static void Mark(TwitchModule module) => module.SetBannerColor(module.MarkedBackgroundColor);

	[Command(@"unmark", AccessLevel.Mod, AccessLevel.Mod)]
	public static void Unmark(TwitchModule module) => module.SetBannerColor(module.Claimed ? module.ClaimedBackgroundColour : module.unclaimedBackgroundColor);

	[Command(@"zoom(?: +(\d*\.?\d+))?")]
	public static IEnumerator Zoom(TwitchModule module, string user, [Group(1)] float? duration)
	{
		MusicPlayer musicPlayer = null;
		var delay = duration ?? 2;
		delay = Math.Max(2, delay);
		module.Solver._zoom = true;
		if (delay >= 15)
			musicPlayer = MusicPlayer.StartRandomMusic();

		var zoomCoroutine = TwitchGame.ModuleCameras?.ZoomCamera(module, 1);
		if (zoomCoroutine != null)
			while (zoomCoroutine.MoveNext())
				yield return zoomCoroutine.Current;

		yield return new WaitForSecondsWithCancel(delay, false, module.Solver);
		if (CoroutineCanceller.ShouldCancel)
		{
			CoroutineCanceller.ResetCancel();
			IRCConnection.SendMessage($"Sorry @{user}, your request to hold up the bomb for {delay} seconds has been cut short.");
		}

		if (musicPlayer != null)
			musicPlayer.StopMusic();

		var unzoomCoroutine = TwitchGame.ModuleCameras?.UnzoomCamera(module, 1);
		if (unzoomCoroutine != null)
			while (unzoomCoroutine.MoveNext())
				yield return unzoomCoroutine.Current;
	}

	[Command(@"zoom +(?!\d*\.?\d+$)(?:send +to +module +)?(.*)")]
	public static IEnumerator DefaultZoomCommand1(TwitchModule module, string user, [Group(1)] string zoomCmd) => RunModuleCommand(module, user, zoomCmd, zoom: true);

	[Command(null)]
	public static IEnumerator DefaultCommand(TwitchModule module, string user, string cmd) => RunModuleCommand(module, user, cmd, zoom: false);

	private static IEnumerator RunModuleCommand(TwitchModule module, string user, string cmd, bool zoom)
	{
		if (module.Solver == null)
			yield break;

		if (module.Solved && !TwitchPlaySettings.data.AnarchyMode)
		{
			IRCConnection.SendMessage(TwitchPlaySettings.data.AlreadySolved, module.Code, module.PlayerName, user, module.HeaderText);
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
			yield return new WaitForSeconds(0.1f);
			var response = module.Solver.RespondToCommand(user, cmd, zoom);
			while (response.MoveNext())
				yield return response.Current;
			yield return new WaitForSeconds(0.1f);
		}
		else
			IRCConnection.SendMessage(TwitchPlaySettings.data.AlreadyClaimed, module.Code, module.PlayerName, user, module.HeaderText);
	}

	#region Private methods
	private static void ClaimViewOrPin(TwitchModule module, string user, bool isWhisper, bool view, bool pin)
	{
		if (isWhisper)
		{
			IRCConnection.SendMessage($"Sorry {user}, claiming modules is not allowed in whispers", user, false);
			return;
		}

		var result = module.ClaimModule(user);
		IRCConnection.SendMessage(result.Second);
		if (result.First && view)
			module.ViewPin(user, pin);
	}
	#endregion
}
