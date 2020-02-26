using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>Commands related to the bomb.</summary>
/// <prefix>bomb </prefix>
public static class BombCommands
{
	#region Commands
	/// <name>Help</name>
	/// <syntax>help</syntax>
	/// <summary>Gives you information about various bomb commands.</summary>
	[Command("help")]
	public static void Help(string user, bool isWhisper) => IRCConnection.SendMessage(TwitchPlaySettings.data.BombHelp, user, !isWhisper);

	/// <name>Turn</name>
	/// <syntax>turn</syntax>
	/// <summary>Turns the bomb around.</summary>
	[Command(@"(turn|turn round|turn around|rotate|flip|spin)")]
	public static IEnumerator TurnBomb(TwitchBomb bomb) => bomb.TurnBomb();

	/// <name>Hold</name>
	/// <syntax>hold</syntax>
	/// <summary>Holds the bomb.</summary>
	[Command(@"(hold|pick up)")]
	public static IEnumerator Hold(TwitchBomb bomb) => bomb.HoldBomb();
	/// <name>Drop</name>
	/// <syntax>drop</syntax>
	/// <summary>Drops the bomb.</summary>
	[Command(@"(drop|let go|put down)")]
	public static IEnumerator Drop(TwitchBomb bomb) => bomb.LetGoBomb();

	/// <name>Throw</name>
	/// <syntax>throw (strength)</syntax>
	/// <summary>Throws the bomb. (strength) is how much force the bomb is thrown with.</summary>
	/// <restriction>Admin</restriction>
	[Command(@"(?:throw|yeet) *(\d+)?", AccessLevel.Admin, AccessLevel.Admin)]
	public static IEnumerator Throw(TwitchBomb bomb, [Group(1)] int? strength = 5)
	{
		yield return HoldableCommands.Throw(bomb.Bomb.GetComponent<FloatingHoldable>(), strength);
	}

	/// <name>Elevator Edgework</name>
	/// <syntax>edgework (wall)</syntax>
	/// <summary>Shows the edgework on the elevator. (wall) is which wall of the elevator to show, ex: right, left or back.</summary>
	/// <restriction>ElevatorOnly</restriction>
	[Command(@"edgework((?: right| left| back| r| l| b)?)"), ElevatorOnly]
	public static IEnumerator EdgeworkElevator(TwitchBomb bomb, [Group(1)] string edge, string user, bool isWhisper) => Edgework(bomb, edge, user, isWhisper);
	/// <name>Edgework</name>
	/// <syntax>edgework (edge)\nedgework 45</syntax>
	/// <summary>Rotates the bomb to show the edgework. (edge) is which edge of the bomb will be shown, ex: top or top left. Using 45 will rotate the bomb in 45 degree increments.</summary>
	/// <restriction>ElevatorDisallowed</restriction>
	[Command(@"edgework((?: 45|-45)|(?: top right| right top| right bottom| bottom right| bottom left| left bottom| left top| top left| left| top| right| bottom| tr| rt| tl| lt| br| rb| bl| lb| t| r| b| l))?"), ElevatorDisallowed]
	public static IEnumerator Edgework(TwitchBomb bomb, [Group(1)] string edge, string user, bool isWhisper)
	{
		if (TwitchPlaySettings.data.EnableEdgeworkCommand || TwitchPlaySettings.data.AnarchyMode)
			return bomb.ShowEdgework(edge);
		else
		{
			IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.BombEdgework, bomb.EdgeworkText.text), user, !isWhisper);
			return null;
		}
	}

	/// <name>Time</name>
	/// <syntax>time</syntax>
	/// <summary>Sends a message with how much time is left.</summary>
	[Command(@"(timer?|clock)")]
	public static void Time(TwitchBomb bomb, string user, bool isWhisper) => IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.BombTimeRemaining, bomb.GetFullFormattedTime, bomb.GetFullStartingTime), user, !isWhisper);
	/// <name>Timestamp</name>
	/// <syntax>timestamp</syntax>
	/// <summary>Sends a message with when the bomb started.</summary>
	[Command(@"(timestamp|date)")]
	public static void Timestamp(TwitchBomb bomb, string user, bool isWhisper) => IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.BombTimeStamp, bomb.BombTimeStamp), user, !isWhisper);

	/// <name>End Zen Mode</name>
	/// <syntax>endzenmode</syntax>
	/// <summary>Ends a zen mode bomb. Requires either Defuser rank or a minimum score.</summary>
	[Command(@"endzenmode")]
	public static IEnumerator Explode(TwitchBomb bomb, string user, bool isWhisper)
	{
		if (!OtherModes.ZenModeOn)
		{
			IRCConnection.SendMessage("Zen mode is not on.", user, false, user);
			return null;
		}

		if (isWhisper)
		{
			IRCConnection.SendMessage("Sorry {0}, you can't end Zen mode in a whisper.", user, false, user);
			return null;
		}

		Leaderboard.Instance.GetRank(user, out var entry);
		if (!UserAccess.HasAccess(user, AccessLevel.Defuser, true) && entry != null && entry.SolveScore < TwitchPlaySettings.data.MinScoreForNewbomb)
		{
			IRCConnection.SendMessage("Sorry, you don't have enough points to end Zen mode.");
			return null;
		}

		return bomb.DelayBombExplosionCoroutine();
	}

	/// <name>Explode</name>
	/// <syntax>explode</syntax>
	/// <summary>Forces the bomb to explode.</summary>
	/// <restriction>Mod</restriction>
	[Command(@"(explode|detonate|kapow)", AccessLevel.Mod, AccessLevel.Mod)]
	public static IEnumerator Explode(TwitchBomb bomb) => bomb.DelayBombExplosionCoroutine();

	#region Voting
	/// <name>Start a vote</name>
	/// <syntax>vote [action]</syntax>
	/// <summary>Starts a vote about doing an action</summary>
	[Command(@"vote (explode|detonate|kapow)")]
	public static void VoteStart(TwitchBomb bomb, string user, [Group(1)] bool Detonation) => Votes.StartVote(bomb, user, Detonation ? VoteTypes.Detonation : 0);

	/// <name>Vote</name>
	/// <syntax>vote [choice]</syntax>
	/// <summary>Vote with yes or no</summary>
	[Command(@"vote (yes|voteyea)|(no|votenay)")]
	public static void Vote(string user, [Group(1)] bool yesVote) => GlobalCommands.Vote(user, yesVote);

	/// <name>Remove vote</name>
	/// <syntax>vote remove</syntax>
	/// <summary>Removes the vote of a user</summary>
	[Command(@"vote remove")]
	public static void RemoveVote(string user) => GlobalCommands.RemoveVote(user);

	/// <name>Cancel vote</name>
	/// <syntax>vote cancel</syntax>
	/// <summary>Cancels a voting process</summary>
	/// <restriction>Mod</restriction>
	[Command(@"vote cancel", AccessLevel.Mod, AccessLevel.Mod)]
	public static void CancelVote() => GlobalCommands.CancelVote();

	/// <name>Force-end vote</name>
	/// <syntax>vote forceend</syntax>
	/// <summary>Skips the countdown of the voting process</summary>
	/// <restriction>Mod</restriction>
	[Command(@"vote forceend", AccessLevel.Mod, AccessLevel.Mod)]
	public static void ForceEndVote() => GlobalCommands.ForceEndVote();
	#endregion

	/// <name>Status</name>
	/// <syntax>status</syntax>
	/// <summary>Sends a message with the current status of the bomb. Including things like time, strikes and solves.</summary>
	[Command(@"(status|info)")]
	public static void Status(TwitchBomb bomb, string user, bool isWhisper)
	{
		int currentReward = TwitchPlaySettings.GetRewardBonus();
		if (OtherModes.TimeModeOn)
		{
			IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.BombStatusTimeMode, bomb.GetFullFormattedTime, bomb.GetFullStartingTime,
				OtherModes.GetAdjustedMultiplier(), bomb.bombSolvedModules, bomb.bombSolvableModules, currentReward), user, !isWhisper);
		}
		else if (OtherModes.VSModeOn)
		{
			IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.BombStatusVsMode, bomb.GetFullFormattedTime,
				bomb.GetFullStartingTime, OtherModes.goodHealth, OtherModes.evilHealth, currentReward), user, !isWhisper);
		}
		else
		{
			IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.BombStatus, bomb.GetFullFormattedTime, bomb.GetFullStartingTime,
				bomb.StrikeCount, bomb.StrikeLimit, bomb.bombSolvedModules, bomb.bombSolvableModules, currentReward), user, !isWhisper);
		}
	}

	/// <name>Pause</name>
	/// <syntax>pause</syntax>
	/// <summary>Pauses the bomb timer.</summary>
	/// <restriction>Admin</restriction>
	[Command(@"pause", AccessLevel.Admin)]
	public static void Pause(TwitchBomb bomb)
	{
		if (bomb.Bomb.GetTimer().IsUpdating)
		{
			bomb.Bomb.GetTimer().StopTimer();
		}
	}
	/// <name>Unpause</name>
	/// <syntax>unpause</syntax>
	/// <summary>Starts the bomb timer.</summary>
	/// <restriction>Admin</restriction>
	[Command(@"(unpause|resume)", AccessLevel.Admin)]
	public static void Unpause(TwitchBomb bomb)
	{
		if (!bomb.Bomb.GetTimer().IsUpdating)
			bomb.Bomb.GetTimer().StartTimer();
	}

	/// <name>Change Timer</name>
	/// <syntax>add time [time]\nsubstract time [time]\nset time [time]</syntax>
	/// <summary>Adds, substracts or sets the bomb time.</summary>
	/// <restriction>Admin</restriction>
	[Command(@"(?:add|increase|(subtract|decrease|remove)|(change|set)) +(?:time|t) +(.+)", AccessLevel.Admin, AccessLevel.Admin)]
	public static void ChangeTimer(TwitchBomb bomb, string user, bool isWhisper, [Group(1)] bool negative, [Group(2)] bool direct, [Group(3)] string amount)
	{
		float time = 0;
		var timeLengths = new Dictionary<string, float>()
			{
				{ "ms", 0.001f },
				{ "s", 1 },
				{ "m", 60 },
				{ "h", 3600 },
				{ "d", 86400 },
				{ "w", 604800 },
				{ "y", 31536000 },
			};

		foreach (string part in amount.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
		{
			bool valid = false;
			foreach (string unit in timeLengths.Keys)
			{
				if (!part.EndsWith(unit, StringComparison.InvariantCultureIgnoreCase) || !float.TryParse(part.Substring(0, part.Length - unit.Length), out float length))
					continue;
				time += length * timeLengths[unit];
				valid = true;
				break;
			}

			if (!valid)
			{
				IRCConnection.SendMessage(@"I don’t understand “{0}”.", user, !isWhisper, part);
				return;
			}
		}

		time = (float) Math.Round((decimal) time, 2, MidpointRounding.AwayFromZero);
		if (!direct && Math.Abs(time) < 0.01f)
			return;

		bomb.CurrentTimer = direct ? time : negative ? bomb.CurrentTimer - time : bomb.CurrentTimer + time;

		// If the time requested was negative, we need to flip the message.
		bool negativeTime = time < 0 ? !negative : negative;

		IRCConnection.SendMessage(direct
			? $"Set the bomb's timer to {Math.Abs(time < 0 ? 0 : time).FormatTime()}."
			: $"{(negativeTime ? "Subtracted" : "Added")} {Math.Abs(time).FormatTime()} {(negativeTime ? "from" : "to")} the timer.", user, !isWhisper);
	}

	/// <name>Change Strikes</name>
	/// <syntax>add strikes [strikes]\nsubstract strikes [strikes]\nset strikes [strikes]</syntax>
	/// <summary>Adds, substracts or sets the number of strikes.</summary>
	/// <restriction>Admin</restriction>
	[Command(@"(?:add|increase|(subtract|decrease|remove)|(change|set)) +(?:(strikes?|s)|strikelimit|sl|maxstrikes?|ms) +(-?\d+)", AccessLevel.Admin, AccessLevel.Admin)]
	public static void ChangeStrikeParameter(TwitchBomb bomb, string user, bool isWhisper, [Group(1)] bool negative, [Group(2)] bool direct, [Group(3)] bool isStrikes, [Group(4)] int amount)
	{
		void setParameter(string thing1, string thing2, int originalAmount, Func<int, int> set)
		{
			// Don’t go below 0 strikes because Simon Says is unsolvable then.
			var newAmount = set(Math.Max(0, direct ? amount : negative ? originalAmount - amount : originalAmount + amount));

			if (direct)
				IRCConnection.SendMessage(string.Format("{2} set to {0} {1}.", newAmount, newAmount != 1 ? "strikes" : "strike", thing1), user, !isWhisper);
			else
			{
				var difference = Math.Abs(newAmount - originalAmount);
				IRCConnection.SendMessage(string.Format(newAmount >= originalAmount ? "Added {0} {1} to the {2}." : "Subtracted {0} {1} from the {2}.", difference, difference != 1 ? "strikes" : "strike", thing2), user, !isWhisper);
			}
		}

		if (isStrikes)
			setParameter("Strike count", "bomb", bomb.StrikeCount, am => bomb.StrikeCount = am);
		else    // strike limit
			setParameter("Strike limit", "strike limit", bomb.StrikeLimit, am => bomb.StrikeLimit = am);

		TwitchGame.ModuleCameras.UpdateStrikes();
	}
	#endregion
}
