using System.Collections.Generic;
using UnityEngine;

public class TwitchLeaderboardTable : MonoBehaviour
{
	[Header("Prefabs")]
	public TwitchLeaderboardRow[] specialRows = null;
	public TwitchLeaderboardRow normalRow = null;

	[Header("Hierarchy Management")]
	public RectTransform tableTransform = null;

	[Header("Values")]
	public int maximumRowCount = 20;

	private readonly List<TwitchLeaderboardRow> _instancedRows = new List<TwitchLeaderboardRow>();

	private void Start()
	{
		if (Leaderboard.Instance == null)
		{
			return;
		}

		float delay = 0.6f;
		int index = 0;

		foreach (Leaderboard.LeaderboardEntry entry in Leaderboard.Instance.GetSortedEntriesIncluding(Leaderboard.Instance.CurrentSolvers.Keys, maximumRowCount))
		{
			TwitchLeaderboardRow row = Instantiate(index < specialRows.Length ? specialRows[index] : normalRow);

			row.position = entry.Rank;
			row.leaderboardEntry = entry;
			row.delay = delay;
			row.transform.SetParent(tableTransform, false);

			_instancedRows.Add(row);

			delay += 0.1f;
			index++;
		}
	}

	private void OnDisable()
	{
		foreach (TwitchLeaderboardRow row in _instancedRows)
		{
			DestroyObject(row);
		}

		_instancedRows.Clear();
	}
}
