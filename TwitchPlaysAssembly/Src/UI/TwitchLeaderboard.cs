using System.Collections.Generic;
using UnityEngine;

public class TwitchLeaderboard : MonoBehaviour
{
	[Header("Prefabs")]
	public TwitchLeaderboardTable twitchLeaderboardTablePrefab = null;
	public TwitchLeaderboardTableSolo twitchLeaderboardTableSoloPrefab = null;
	public TwitchLeaderboardStats twitchLeaderboardStatsPrefab = null;

	[Header("Hierachy Management")]
	public RectTransform mainTableTransform = null;
	public RectTransform altTableTransform = null;
	public RectTransform promptTransform = null;
	public RectTransform retryTransform = null;
	public RectTransform runTransform = null;
	public RectTransform leftMaskTransform = null;

	private TwitchLeaderboardTable mainTable = null;
	private TwitchLeaderboardTableSolo soloTable = null;
	private TwitchLeaderboardStats statsTable = null;

	private void Start()
	{
		if (Leaderboard.Instance == null)
		{
			return;
		}

		if (Leaderboard.Instance.Success || !TwitchPlaySettings.data.EnableRetryButton)
		{
			retryTransform.gameObject.SetActive(false);
		}

		if (!TwitchPlaySettings.data.EnableRunCommand)
			runTransform.gameObject.SetActive(false);

		statsTable = Instantiate<TwitchLeaderboardStats>(twitchLeaderboardStatsPrefab);
		
		if (Leaderboard.Instance.Count > 0)
		{
			mainTable = Instantiate<TwitchLeaderboardTable>(twitchLeaderboardTablePrefab);
		}

		if (Leaderboard.Instance.SoloCount > 0)
		{
			soloTable = Instantiate<TwitchLeaderboardTableSolo>(twitchLeaderboardTableSoloPrefab);
			leftMaskTransform.gameObject.SetActive(true);

			bool prioritiseSolo = (Leaderboard.Instance.SoloSolver != null);
			int countOnRight = prioritiseSolo ? Leaderboard.Instance.SoloCount : Leaderboard.Instance.Count;
			int countOnLeft = prioritiseSolo ? Leaderboard.Instance.Count : Leaderboard.Instance.SoloCount;
			int maxOnRight = prioritiseSolo ? soloTable.maximumRowCount : mainTable.maximumRowCount;
			bool statsOnRight = (countOnRight <= (maxOnRight - statsTable.entriesLess)) // Right (priority) wouldn't be made smaller than its total count by fitting the stats in
				&& (countOnRight < countOnLeft); // and right is smaller than left (only possible if solo is on right)

			// Make the leaderboard that's sharing with the stats smaller
			if (statsOnRight == prioritiseSolo)
			{
				soloTable.maximumRowCount -= statsTable.entriesLess;
			}
			else
			{
				mainTable.maximumRowCount -= statsTable.entriesLess;
			}

			statsTable.transform.SetParent(statsOnRight ? mainTableTransform : altTableTransform, false);
			soloTable.transform.SetParent(prioritiseSolo ? mainTableTransform : altTableTransform, false);
			mainTable.transform.SetParent(prioritiseSolo ? altTableTransform : mainTableTransform, false);
		}
		else
		{
			if ((Leaderboard.Instance.Count - statsTable.entriesLess) > mainTable.maximumRowCount)
			{
				statsTable.transform.SetParent(altTableTransform, false);
				leftMaskTransform.gameObject.SetActive(true);
			}
			else
			{
				statsTable.transform.SetParent(mainTableTransform, false);
			}

			mainTable.transform.SetParent(mainTableTransform, false);
		}
		
		StartCoroutine(DelayPrompt(10.0f));
	}

	private IEnumerator<WaitForSeconds> DelayPrompt(float seconds)
	{
		yield return new WaitForSeconds(seconds);
		promptTransform.gameObject.SetActive(true);
	}

	private void OnDisable()
	{

	}
}
