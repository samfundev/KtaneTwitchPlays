using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum VoteTypes
{
	Detonation,
	VSModeToggle,
	Solve
}

public class VoteData
{
	// Name of the vote (Displayed over !notes3 when in game)
	internal string Name
	{
		get => Votes.CurrentVoteType == VoteTypes.Solve ? $"Solve module {Votes.voteModule.Code} ({Votes.voteModule.HeaderText})" : _name;
		set => _name = value;
	}

	// Action to execute if the vote passes
	internal Action onSuccess;

	// Checks the validity of a vote
	internal List<Tuple<Func<bool>, string>> validityChecks;

	private string _name;
}

public static class Votes
{
	private static float VoteTimeRemaining = -1f;
	internal static VoteTypes CurrentVoteType;

	public static bool Active => voteInProgress != null;
	internal static int TimeLeft => Mathf.CeilToInt(VoteTimeRemaining);
	internal static int NumVoters => Voters.Count;

	internal static TwitchModule voteModule;

	internal static readonly Dictionary<VoteTypes, VoteData> PossibleVotes = new Dictionary<VoteTypes, VoteData>()
	{
		{
			VoteTypes.Detonation, new VoteData {
				Name = "Detonate the bomb",
				validityChecks = new List<Tuple<Func<bool>, string>>
				{
					CreateCheck(() => TwitchGame.Instance.VoteDetonateAttempted, "Sorry, {0}, a detonation vote was already attempted on this bomb. Another one cannot be started.")
				},
				onSuccess = () => TwitchGame.Instance.Bombs[0].CauseExplosionByVote()
			}
		},
		{
			VoteTypes.VSModeToggle, new VoteData {
				Name = "Toggle VS mode",
				validityChecks = null,
				onSuccess = () => {
					OtherModes.Toggle(TwitchPlaysMode.VS);
					IRCConnection.SendMessage($"{OtherModes.GetName(OtherModes.nextMode)} mode will be enabled next round.");
				}
			}
		},
		{
			VoteTypes.Solve, new VoteData {
				validityChecks = new List<Tuple<Func<bool>, string>>
				{
					CreateCheck(() => !TwitchPlaySettings.data.EnableVoteSolve, "Sorry, {0}, votesolving is disabled."),
					CreateCheck(() => voteModule.Votesolving, "Sorry, {0}, that module is already being votesolved."),
					CreateCheck(() => OtherModes.currentMode == TwitchPlaysMode.VS, "Sorry, {0}, votesolving is disabled during vsmode bombs."),
					CreateCheck(() => TwitchGame.Instance.VoteSolveCount >= 2, "Sorry, {0}, two votesolves have already been used. Another one cannot be started."),
					CreateCheck(() =>
						voteModule.BombComponent.GetModuleID().IsBossMod() &&
						((double)TwitchGame.Instance.CurrentBomb.BombSolvedModules / TwitchGame.Instance.CurrentBomb.BombSolvableModules >= .10f ||
						TwitchGame.Instance.CurrentBomb.BombStartingTimer - TwitchGame.Instance.CurrentBomb.CurrentTimer < 120),
						"Sorry, {0}, boss mods may only be votesolved before 10% of all modules are solved and when at least 2 minutes of the bomb has passed."),
					CreateCheck(() =>
						((double)TwitchGame.Instance.CurrentBomb.BombSolvedModuleIDs.Count(x => !x.IsBossMod()) /
						TwitchGame.Instance.CurrentBomb.BombSolvableModuleIDs.Count(x => !x.IsBossMod()) <= 0.75f) &&
						!voteModule.BombComponent.GetModuleID().IsBossMod(),
						"Sorry, {0}, more than 75% of all non-boss modules on the bomb must be solved in order to call a votesolve."),
					CreateCheck(() => voteModule.Claimed, "Sorry, {0}, the module must be unclaimed for it to be votesolved."),
					CreateCheck(() => voteModule.ClaimQueue.Count > 0, "Sorry, {0}, the module you are trying to votesolve has a queued claim on it."),
					CreateCheck(() => (int)voteModule.ScoreMethods.Sum(x => x.CalculateScore(null)) <= 8 && !voteModule.BombComponent.GetModuleID().IsBossMod(), "Sorry, {0}, the module must have a score greater than 8."),
					CreateCheck(() => TwitchGame.Instance.CommandQueue.Any(x => x.Message.Text.StartsWith($"!{voteModule.Code} ")), "Sorry, {0}, the module you are trying to solve is in the queue."),
					CreateCheck(() => GameplayState.MissionToLoad != "custom", "Sorry, {0}, you can't votesolve modules while in a mission bomb.")
				},
				onSuccess = () =>
				{
					voteModule.Solver.SolveModule($"A module ({voteModule.HeaderText}) is being automatically solved.");
					voteModule.SetClaimedUserMultidecker("VOTESOLVING");
					voteModule.Votesolving = true;
					TwitchPlaySettings.SetRewardBonus((TwitchPlaySettings.GetRewardBonus() * 0.75f).RoundToInt());
					IRCConnection.SendMessage($"Reward decreased by 25% for votesolving module {voteModule.Code} ({voteModule.HeaderText})");
				}
			}
		}
	};

	private static readonly Dictionary<string, bool> Voters = new Dictionary<string, bool>();

	private static Coroutine voteInProgress;
	private static IEnumerator VotingCoroutine()
	{
		while (VoteTimeRemaining >= 0f)
		{
			var oldTime = TimeLeft;
			VoteTimeRemaining -= Time.deltaTime;

			if (TwitchGame.BombActive && TimeLeft != oldTime) // Once a second, update notes.
				TwitchGame.ModuleCameras.SetNotes();
			yield return null;
		}

		if (TwitchGame.BombActive && (CurrentVoteType == VoteTypes.Detonation || CurrentVoteType == VoteTypes.Solve))
		{
			// Add claimed users who didn't vote as "no"
			int numAddedNoVotes = 0;
			List<string> usersWithClaims = TwitchGame.Instance.Modules
				.Where(m => !m.Solved && m.PlayerName != null).Select(m => m.PlayerName).Distinct().ToList();
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
		if (!votePassed && CurrentVoteType == VoteTypes.Solve)
		{
			voteModule.SetBannerColor(voteModule.unclaimedBackgroundColor);
			voteModule.SetClaimedUserMultidecker(null);
		}
		if (votePassed)
		{
			PossibleVotes[CurrentVoteType].onSuccess();
		}

		DestroyVote();
	}

	private static void CreateNewVote(string user, VoteTypes act, TwitchModule module = null)
	{
		voteModule = module;
		if (TwitchGame.BombActive && act != VoteTypes.VSModeToggle)
		{
			if (act == VoteTypes.Solve && module == null)
				throw new InvalidOperationException("Module is null in a votesolve! This should not happen, please send this logfile to the TP developers!");

			var validity = PossibleVotes[act].validityChecks.Find(x => x.First());
			if (validity != null && !(TwitchPlaySettings.data.AnarchyMode && !voteModule.Votesolving))
			{
				IRCConnection.SendMessage(string.Format(validity.Second, user));
				return;
			}

			switch (act)
			{
				case VoteTypes.Detonation:
					TwitchGame.Instance.VoteDetonateAttempted = true;
					break;
				case VoteTypes.Solve:
					if (voteModule is null)
						throw new InvalidOperationException("Votemodule cannot be null");
					TwitchGame.Instance.VoteSolveCount++;
					voteModule.SetBannerColor(voteModule.MarkedBackgroundColor);
					voteModule.SetClaimedUserMultidecker("VOTE IN PROGRESS");
					break;
			}
		}

		CurrentVoteType = act;
		VoteTimeRemaining = TwitchPlaySettings.data.VoteCountdownTime;
		Voters.Clear();
		Voters.Add(user, true);
		IRCConnection.SendMessage($"Voting has started by {user} to \"{PossibleVotes[CurrentVoteType].Name}\"! Vote with '!vote VoteYea ' or '!vote VoteNay '.");
		voteInProgress = TwitchPlaysService.Instance.StartCoroutine(VotingCoroutine());
		if (TwitchGame.Instance.alertSound != null)
			TwitchGame.Instance.alertSound.Play();
		if (TwitchGame.BombActive)
			TwitchGame.ModuleCameras.SetNotes();
	}

	private static void DestroyVote()
	{
		if (voteInProgress != null)
			TwitchPlaysService.Instance.StopCoroutine(voteInProgress);
		voteInProgress = null;
		Voters.Clear();
		voteModule = null;
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

	public static void StartVote(string user, VoteTypes act, TwitchModule module = null)
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

		CreateNewVote(user, act, module);
	}

	public static void TimeLeftOnVote(string user)
	{
		if (!Active)
		{
			IRCConnection.SendMessage($"{user}, there is no vote currently in progress.");
			return;
		}
		IRCConnection.SendMessage($"The current vote to \"{PossibleVotes[CurrentVoteType].Name}\" lasts for {TimeLeft} more seconds.");
	}

	public static void CancelVote(string user)
	{
		if (!Active)
		{
			IRCConnection.SendMessage($"{user}, there is no vote currently in progress.");
			return;
		}
		IRCConnection.SendMessage("The vote has been cancelled.");
		if (CurrentVoteType == VoteTypes.Solve)
		{
			voteModule.SetBannerColor(voteModule.unclaimedBackgroundColor);
			voteModule.SetClaimedUserMultidecker(null);
		}
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

	private static Tuple<Func<bool>, string> CreateCheck(Func<bool> func, string str) => new Tuple<Func<bool>, string>(func, str);
}