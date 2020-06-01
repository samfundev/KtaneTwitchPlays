using System.Collections.Generic;
using UnityEngine;

public class TwitchLeaderboardTableSolo : MonoBehaviour
{
	[Header("Prefabs")]
	public TwitchLeaderboardSoloRow[] specialRows = null;
	public TwitchLeaderboardSoloRow normalRow = null;

	[Header("Hierachy Management")]
	public RectTransform tableTransform = null;

	[Header("Values")]
	public int maximumRowCount = 20;

	private readonly List<TwitchLeaderboardSoloRow> _instancedRows = new List<TwitchLeaderboardSoloRow>();
	public Leaderboard.LeaderboardEntry solver = null;
	public int bombCount = 0;

	private void Start()
	{
		if (Leaderboard.Instance == null)
		{
			return;
		}

		float delay = 0.6f;
		int index = 0;

		solver = Leaderboard.Instance.SoloSolver;

		IEnumerable<Leaderboard.LeaderboardEntry> entries = (solver == null) ?
			Leaderboard.Instance.GetSortedSoloEntries(maximumRowCount) :
			Leaderboard.Instance.GetSortedSoloEntriesIncluding(solver.UserName, maximumRowCount);

		foreach (Leaderboard.LeaderboardEntry entry in entries)
		{
			TwitchLeaderboardSoloRow row = Instantiate(index < specialRows.Length ? specialRows[index] : normalRow);

			row.position = entry.SoloRank;
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
		foreach (TwitchLeaderboardSoloRow row in _instancedRows)
		{
			DestroyObject(row);
		}

		_instancedRows.Clear();
	}
}
