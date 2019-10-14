using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

static class ModuleCommands
{
	[Command(@"(?:help|manual)( +pdf)?"), SolvedAllowed]
	public static void Help(TwitchModule module, [Group(1)] bool pdf)
	{
		string manualType = pdf ? "pdf" : "html";

		string manualText = string.IsNullOrEmpty(module.Solver.ManualCode) ? module.HeaderText : module.Solver.ManualCode;
		string helpText = string.IsNullOrEmpty(module.Solver.HelpMessage) ? string.Empty : string.Format(module.Solver.HelpMessage, module.Code, module.HeaderText);

		IRCConnection.SendMessage(Regex.IsMatch(manualText, @"^https?://", RegexOptions.IgnoreCase)
			? $"{module.HeaderText} : {helpText} : {manualText}"
			: $"{module.HeaderText} : {helpText} : {UrlHelper.Instance.ManualFor(manualText, manualType, VanillaRuleModifier.GetModuleRuleSeed(module.Solver.ModInfo.moduleID) != 1)}");
	}

	[Command("player"), SolvedAllowed]
	public static void Player(TwitchModule module, string user) => IRCConnection.SendMessage(module.PlayerName != null
			? string.Format(TwitchPlaySettings.data.ModulePlayer, module.Code, module.PlayerName, module.HeaderText)
			: string.Format(TwitchPlaySettings.data.ModuleNotClaimed, user, module.Code, module.HeaderText));

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

	[Command("cancel +(?:bomb|queue) +(?:turn(?: +a?round)?|flip|spin)")]
	public static void BombTurnAroundCancel(TwitchModule module)
	{
		module.Solver.TurnQueued = false;
		IRCConnection.SendMessageFormat(TwitchPlaySettings.data.CancelBombTurn, module.Code, module.HeaderText);
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
			(module.Unsupported || module.Solver.GetType() == typeof(UnsupportedModComponentSolver))
		)
			module.Solver.SolveModule($"A module ({module.HeaderText}) is being automatically solved.");
	}

	[Command(@"(claim view|view claim|claimview|viewclaim|cv|vc|claim view pin|view pin claim|claimviewpin|viewpinclaim|cvp|vpc)")]
	public static void ClaimViewPin(TwitchModule module, string user, bool isWhisper, [Group(1)] string cmd) => ClaimViewOrPin(module, user, isWhisper, view: true, pin: cmd.Contains("p"));

	[Command("(unclaim|un?c|release|rel|unclaim unview|unview unclaim|unclaimview|unviewclaim|uncv|unvc)")]
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

	[Command(@"solved", AccessLevel.Mod, AccessLevel.Mod)]
	public static void Solved(TwitchModule module, string user)
	{
		module.SetBannerColor(module.SolvedBackgroundColor);
		module.PlayerName = null;
		IRCConnection.SendMessageFormat(TwitchPlaySettings.data.ModuleReady, module.Code, user, module.HeaderText);
	}

	[Command(@"assign +(.+)")]
	public static void Assign(TwitchModule module, string user, [Group(1)] string targetUser)
	{
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

		// Attempt to take over from another user
		else
		{
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
		}

		// The module isn’t claimed: just claim it
		else if (module.PlayerName == null)
			IRCConnection.SendMessage(module.TryClaim(user).Message);

		// Someone else has a claim on the module
		else if (module.PlayerName != user)
			IRCConnection.SendMessageFormat(TwitchPlaySettings.data.AlreadyClaimed, module.Code, module.PlayerName, user, module.HeaderText);

		// If the user has a claim on the module but there’s no takeover attempt, just ignore this command
	}

	[Command(@"canceltake")]
	public static void CancelTake(TwitchModule module, string user, bool isWhisper)
	{
		if (module.TakeInProgress == null)
		{
			IRCConnection.SendMessage($"@{user}, there are no takeover attempts on module {module.Code} ({module.HeaderText}).", user, !isWhisper);
			return;
		}

		if (!UserAccess.HasAccess(user, AccessLevel.Mod) && module.TakeUser != user)
		{
			IRCConnection.SendMessage($"@{user}, if you’re not a mod, you can only cancel your own takeover attempts.");
			return;
		}

		// Cancel the takeover attempt
		IRCConnection.SendMessage($"{module.TakeUser}’s takeover of module {module.Code} ({module.HeaderText}) was cancelled by {user}.");
		module.StopCoroutine(module.TakeInProgress);
		module.TakeInProgress = null;
		module.TakeUser = null;
	}

	[Command(@"(points|score)"), SolvedAllowed]
	public static void Points(TwitchModule module) => IRCConnection.SendMessage($"{module.HeaderText} ({module.Code}) {(module.Solver.ModInfo.moduleScoreIsDynamic ? "awards points dynamically depending on the number of modules on the bomb." : $"current score: {module.Solver.ModInfo.moduleScore}")}");

	[Command(@"mark", AccessLevel.Mod, AccessLevel.Mod)]
	public static void Mark(TwitchModule module) => module.SetBannerColor(module.MarkedBackgroundColor);

	[Command(@"unmark", AccessLevel.Mod, AccessLevel.Mod)]
	public static void Unmark(TwitchModule module) => module.SetBannerColor(module.Claimed ? module.ClaimedBackgroundColour : module.unclaimedBackgroundColor);

	public static IEnumerator Zoom(TwitchModule module, SuperZoomData zoomData, object yield)
	{
		var zoomCoroutine = TwitchGame.ModuleCameras?.ZoomCamera(module, zoomData, 1);
		if (zoomCoroutine != null)
			while (zoomCoroutine.MoveNext())
				yield return zoomCoroutine.Current;

		yield return yield is int delay ? new WaitForSecondsWithCancel(delay, false, module.Solver) : yield;

		if (CoroutineCanceller.ShouldCancel)
		{
			module.StartCoroutine(TwitchGame.ModuleCameras?.UnzoomCamera(module, zoomData, 0));
			yield break;
		}

		var unzoomCoroutine = TwitchGame.ModuleCameras?.UnzoomCamera(module, zoomData, 1);
		if (unzoomCoroutine != null)
			while (unzoomCoroutine.MoveNext())
				yield return unzoomCoroutine.Current;
	}

	public static IEnumerator Tilt(TwitchModule module, object yield, string direction)
	{
		float easeCubic(float t) { return 3 * t * t - 2 * t * t * t; }

		IEnumerable TimedAnimation(float length)
		{
			float startTime = Time.time;
			float alpha = 0;
			while (alpha < 1)
			{
				alpha = Mathf.Min((Time.time - startTime) / length, 1);
				yield return alpha;
			}
		}

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

		yield return new WaitForSeconds(0.5f);

		var targetAngle = Quaternion.Euler(new Vector3(-Mathf.Cos(targetRotation * Mathf.Deg2Rad), 0, Mathf.Sin(targetRotation * Mathf.Deg2Rad)) * (module.FrontFace ? 60 : -60));
		foreach (float alpha in TimedAnimation(1f))
		{
			var lerp = Quaternion.Lerp(Quaternion.identity, targetAngle, easeCubic(alpha));
			var bombLerp = module.FrontFace ? lerp : Quaternion.Euler(Vector3.Scale(lerp.eulerAngles, new Vector3(1, 1, -1)));
			module.Bomb.RotateByLocalQuaternion(bombLerp);
			module.Bomb.RotateCameraByLocalQuaternion(module.BombComponent.gameObject, lerp);
			yield return null;
		}

		yield return yield is int delay ? new WaitForSecondsWithCancel(delay, false, module.Solver) : yield;

		if (CoroutineCanceller.ShouldCancel)
		{
			var angle = Quaternion.identity;
			var bombAngle = module.FrontFace ? angle : Quaternion.Euler(Vector3.Scale(angle.eulerAngles, new Vector3(1, 1, -1)));
			module.Bomb.RotateByLocalQuaternion(bombAngle);
			module.Bomb.RotateCameraByLocalQuaternion(module.BombComponent.gameObject, angle);
			module.StartCoroutine(module.Bomb.Defocus(module.Selectable, module.FrontFace, false));
			yield break;
		}

		foreach (float alpha in TimedAnimation(1f))
		{
			var lerp = Quaternion.Lerp(targetAngle, Quaternion.identity, easeCubic(alpha));
			var bombLerp = module.FrontFace ? lerp : Quaternion.Euler(Vector3.Scale(lerp.eulerAngles, new Vector3(1, 1, -1)));
			module.Bomb.RotateByLocalQuaternion(bombLerp);
			module.Bomb.RotateCameraByLocalQuaternion(module.BombComponent.gameObject, lerp);
			yield return null;
		}

		IEnumerator defocusCoroutine = module.Bomb.Defocus(module.Selectable, module.FrontFace, false);
		while (defocusCoroutine.MoveNext())
			yield return defocusCoroutine.Current;

		yield return new WaitForSeconds(0.5f);
	}

	[Command(null)]
	public static IEnumerator DefaultCommand(TwitchModule module, string user, string cmd)
	{
		if (cmd.RegexMatch(out Match match, @"(?:(?<zoom>zoom *(?<time>\d*\.?\d+)?)|(?<superzoom>superzoom *(?<factor>\d*\.?\d+) *(?<x>\d*\.?\d+)? *(?<y>\d*\.?\d+)? *(?<stime>\d*\.?\d+)?))? *(?<tilt>tilt *(?<direction>[uptobmdwnlefrigh]+|-?\d+)?)? *(?:send *to *module)? *(?<command>.+)?"))
		{
			var groups = match.Groups;
			var timed = groups["time"].Success || groups["stime"].Success;
			var zooming = groups["zoom"].Success || groups["superzoom"].Success;
			var tilt = groups["tilt"].Success;
			var command = groups["command"].Success;

			// Both a time and a command can't be entered. And either a zoom or tilt needs to take place otherwise, we should let the command run normally.
			if ((!timed || !command) && (zooming || tilt))
			{
				MusicPlayer musicPlayer = null;
				int delay = 2;
				if (timed)
				{
					delay = groups["time"].Value.TryParseInt() ?? groups["stime"].Value.TryParseInt() ?? 2;
					delay = Math.Max(2, delay);
					if (delay >= 15)
						musicPlayer = MusicPlayer.StartRandomMusic();
				}

				object toYield = command ? (object) RunModuleCommand(module, user, groups["command"].Value) : delay;

				IEnumerator routine = null;
				if (tilt)
				{
					routine = Tilt(module, toYield, groups["direction"].Value.ToLowerInvariant());
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
			var response = module.Solver.RespondToCommand(user, cmd);
			while (response.MoveNext())
				yield return response.Current;
			yield return new WaitForSeconds(0.1f);
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