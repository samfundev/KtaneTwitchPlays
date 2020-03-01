using System;
using System.Collections.Generic;
using System.Timers;
using System.Linq;

public enum VoteTypes
{
	Detonation,
	VSModeToggle
}

public static class Votes
{
	public static bool Active => Countdown.Enabled;
	private static readonly Dictionary<string, bool> Voters = new Dictionary<string, bool>();
	private static TwitchBomb Bomb;

	private static readonly Timer Countdown = new Timer();
	private static VoteTypes ActionType;

	private static readonly Dictionary<VoteTypes, Action> actionDict = new Dictionary<VoteTypes, Action>()
	{
		{ VoteTypes.Detonation, () => Bomb.CauseExplosionByModuleCommand("Voting ended. Detonating bomb...", "Voted detonation") },
		{ VoteTypes.VSModeToggle, () => SendAndDo("Voting ended. Toggling VS Mode...", () => OtherModes.Toggle(TwitchPlaysMode.VS)) }
	};

	private static readonly Dictionary<VoteTypes, string> voteNames = new Dictionary<VoteTypes, string>()
	{
		{ VoteTypes.Detonation, "Detonate the bomb" },
		{ VoteTypes.VSModeToggle, "Toggle VS mode" }
	};

	private static void SendAndDo(string message, Action action)
	{
		IRCConnection.SendMessage(message);
		action();
	}

	static Votes()
	{
		Countdown.Elapsed += Elapsed;
		Countdown.AutoReset = true;
		ResetTimer();
	}

	public static void Elapsed(object _ = null, ElapsedEventArgs __ = null)
	{
		if (!Active) return;

		Countdown.Enabled = false;
		int yesVotes = Voters.Count(pair => pair.Value);
		if (yesVotes >= Voters.Count * (TwitchPlaySettings.data.MinimumYesVotes[ActionType] / 100f))
		{
			actionDict[ActionType]();
			Clear();
			return;
		}

		IRCConnection.SendMessage("Voting ended with a result of no.");
		Clear();
	}

	#region UserCommands
	public static void Vote(string user, bool vote)
	{
		if (!Active)
		{
			IRCConnection.SendMessage("There is no vote currently in progress.");
			return;
		}

		if (!Voters.ContainsKey(user))
		{
			Voters.Add(user, vote);
			IRCConnection.SendMessage($"{user} voted with {(vote ? "yes" : "no")}.");
			return;
		}

		if (Voters[user] == vote)
		{
			IRCConnection.SendMessage($"You already voted with {(vote ? "yes" : "no")}");
			return;
		}

		Voters[user] = vote;
		IRCConnection.SendMessage($"{user} voted with {(vote ? "yes" : "no")}.");
	}

	public static void RemoveVote(string user)
	{
		if (!Active)
		{
			IRCConnection.SendMessage("There is no vote currently in progress.");
			return;
		}

		if (!Voters.ContainsKey(user))
		{
			IRCConnection.SendMessage($"@{user} You haven't voted so far.");
			return;
		}

		Voters.Remove(user);
		IRCConnection.SendMessage($"{user} has removed their vote.");
	}
	#endregion

	public static void StartVote(TwitchBomb TPBomb, string user, VoteTypes act)
	{
		if (!TwitchPlaySettings.data.EnableVoting)
		{
			IRCConnection.SendMessage("Voting is disabled");
			return;
		}

		if (Active)
		{
			IRCConnection.SendMessage("A voting is already in progress!");
			return;
		}

		Clear();
		Bomb = TPBomb;
		ActionType = act;
		Voters.Add(user, true);
		Countdown.Enabled = true;

		IRCConnection.SendMessage($"Voting has started by {user} to \"{voteNames[act]}\"! Enter with '!vote VoteYea ' or '!vote VoteNay '");
	}

	public static void Clear(bool clearGlobal = false)
	{
		if (Bomb == null && !clearGlobal) return;

		Bomb = null;
		Voters.Clear();
		ResetTimer();
	}

	private static void ResetTimer()
	{
		Countdown.Interval = TwitchPlaySettings.data.VoteCountdownTime * 1000;
		Countdown.Enabled = false;
	}
}