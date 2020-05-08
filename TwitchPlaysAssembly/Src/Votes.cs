using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum VoteTypes
{
	Detonation,
	VSModeToggle
}

public class VoteData
{
	// Name of the vote (Displayed over !notes3 when in game)
	public string name;

	// Action to execute if the vote passes
	internal Action onSuccess;
}

public static class Votes
{
	private static float VoteTimeRemaining = -1f;
	internal static VoteTypes CurrentVoteType;

	public static bool Active => (voteInProgress != null);
	internal static int TimeLeft => Mathf.CeilToInt(VoteTimeRemaining);
	internal static int NumVoters => Voters.Count;

	internal static readonly Dictionary<VoteTypes, VoteData> PossibleVotes = new Dictionary<VoteTypes, VoteData>()
	{
		{
			VoteTypes.Detonation, new VoteData {
				name = "Detonate the bomb", 
				onSuccess = () => TwitchGame.Instance.Bombs[0].CauseExplosionByVote()
			}
		},
		{
			VoteTypes.VSModeToggle, new VoteData {
				name = "Toggle VS mode",
				onSuccess = () => {
					OtherModes.Toggle(TwitchPlaysMode.VS);
					IRCConnection.SendMessage($"{OtherModes.GetName(OtherModes.nextMode)} mode will be enabled next round.");
				}
			}
		}
	};

	private static readonly Dictionary<string, bool> Voters = new Dictionary<string, bool>();

	private static Coroutine voteInProgress = null;
	private static IEnumerator VotingCoroutine()
	{
		int oldTime;
		while (VoteTimeRemaining >= 0f)
		{
			oldTime = TimeLeft;
			VoteTimeRemaining -= Time.deltaTime;
			if (TwitchGame.BombActive && TimeLeft != oldTime) // Once a second, update notes.
				TwitchGame.ModuleCameras.SetNotes();
			yield return null;
		}

		if (TwitchGame.BombActive && CurrentVoteType == VoteTypes.Detonation)
		{
			// Add claimed users who didn't vote as "no"
			int numAddedNoVotes = 0;
			List<string> usersWithClaims = TwitchGame.Instance.Modules
				.Where(m => m.PlayerName != null).Select(m => m.PlayerName).Distinct().ToList();
			foreach (string user in usersWithClaims)
			{
				if (!Voters.ContainsKey(user))
				{
					++numAddedNoVotes;
					Voters.Add(user, false);
				}
			}

			if (numAddedNoVotes == 1)
				IRCConnection.SendMessage("1 no vote was added on the behalf of users with claims that did not vote.");
			else if (numAddedNoVotes > 1)
				IRCConnection.SendMessage($"{numAddedNoVotes} no votes were added on the behalf of users with claims that did not vote.");
		}

		int yesVotes = Voters.Count(pair => pair.Value);
		bool votePassed = (yesVotes >= Voters.Count * (TwitchPlaySettings.data.MinimumYesVotes[CurrentVoteType] / 100f));
		IRCConnection.SendMessage($"Voting has ended with {yesVotes}/{Voters.Count} yes votes. The vote has {(votePassed ? "passed" : "failed")}.");

		if (votePassed)
			PossibleVotes[CurrentVoteType].onSuccess();

		DestroyVote();
	}

	private static void CreateNewVote(string user, VoteTypes act)
	{
		if (TwitchGame.BombActive && act == VoteTypes.Detonation)
		{
			if (TwitchGame.Instance.VoteDetonateAttempted)
			{
				IRCConnection.SendMessage($"Sorry, {user}, a detonation vote was already attempted on this bomb. Another one cannot be started.");
				return;
			}
			TwitchGame.Instance.VoteDetonateAttempted = true;
		}

		CurrentVoteType = act;
		VoteTimeRemaining = TwitchPlaySettings.data.VoteCountdownTime;
		Voters.Clear();
		Voters.Add(user, true);
		IRCConnection.SendMessage($"Voting has started by {user} to \"{PossibleVotes[CurrentVoteType].name}\"! Vote with '!vote VoteYea ' or '!vote VoteNay '.");
		voteInProgress = TwitchPlaysService.Instance.StartCoroutine(VotingCoroutine());
		if (TwitchGame.BombActive)
			TwitchGame.ModuleCameras.SetNotes();
	}

	private static void DestroyVote()
	{
		if (voteInProgress != null)
			TwitchPlaysService.Instance.StopCoroutine(voteInProgress);
		voteInProgress = null;
		Voters.Clear();

		if (TwitchGame.BombActive)
			TwitchGame.ModuleCameras.SetNotes();
	}

	internal static void OnStateChange()
	{
		// Any ongoing vote ends.
		DestroyVote();
	}

	#region UserCommands
	public static void Vote(string user, bool vote)
	{
		if (!Active)
		{
			IRCConnection.SendMessage($"{user}, there is no vote currently in progress.");
			return;
		}

		if (Voters.ContainsKey(user) && Voters[user] == vote)
		{
			IRCConnection.SendMessage($"{user}, you've already voted {(vote ? "yes" : "no")}.");
			return;
		}

		if (!Voters.ContainsKey(user))
			Voters.Add(user, vote);
		else
			Voters[user] = vote;
		IRCConnection.SendMessage($"{user} voted {(vote ? "yes" : "no")}.");
	}

	public static void RemoveVote(string user)
	{
		if (!Active)
		{
			IRCConnection.SendMessage($"{user}, there is no vote currently in progress.");
			return;
		}

		if (!Voters.ContainsKey(user))
		{
			IRCConnection.SendMessage($"{user}, you haven't voted.");
			return;
		}

		Voters.Remove(user);
		IRCConnection.SendMessage($"{user} has removed their vote.");
	}
	#endregion

	public static void StartVote(string user, VoteTypes act)
	{
		if (!TwitchPlaySettings.data.EnableVoting)
		{
			IRCConnection.SendMessage($"Sorry, {user}, voting is disabled.");
			return;
		}

		if (Active)
		{
			IRCConnection.SendMessage($"Sorry, {user}, there's already a vote in progress.");
			return;
		}

		CreateNewVote(user, act);
	}

	public static void TimeLeftOnVote(string user)
	{
		if (!Active)
		{
			IRCConnection.SendMessage($"{user}, there is no vote currently in progress.");
			return;
		}
		IRCConnection.SendMessage($"The current vote to \"{PossibleVotes[CurrentVoteType].name}\" lasts for {TimeLeft} more seconds.");
	}

	public static void CancelVote(string user)
	{
		if (!Active)
		{
			IRCConnection.SendMessage($"{user}, there is no vote currently in progress.");
			return;
		}
		IRCConnection.SendMessage("The vote has been cancelled.");
		DestroyVote();
	}

	public static void EndVoteEarly(string user)
	{
		if (!Active)
		{
			IRCConnection.SendMessage($"{user}, there is no vote currently in progress.");
			return;
		}
		IRCConnection.SendMessage("The vote is being ended now.");
		VoteTimeRemaining = 0f;
	}
}
