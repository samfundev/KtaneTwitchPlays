using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TwitchLeaderboardTable : MonoBehaviour
{
    [Header("Prefabs")]
    public TwitchLeaderboardRow[] specialRows = null;
    public TwitchLeaderboardRow normalRow = null;

    [Header("Hierachy Management")]
    public RectTransform tableTransform = null;

    [Header("Values")]
    public int maximumRowCount = 20;

    public Leaderboard leaderboard = null;
    private List<TwitchLeaderboardRow> _instancedRows = new List<TwitchLeaderboardRow>();

    private void Start()
    {
        if (leaderboard == null)
        {
            return;
        }

        float delay = 0.6f;
        int index = 0;

        foreach (Leaderboard.LeaderboardEntry entry in leaderboard.GetSortedEntriesIncluding(leaderboard.CurrentSolvers.Keys, maximumRowCount))
        {
            TwitchLeaderboardRow row = Instantiate<TwitchLeaderboardRow>(index < specialRows.Length ? specialRows[index] : normalRow);

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
        foreach(TwitchLeaderboardRow row in _instancedRows)
        {
            DestroyObject(row);
        }

        _instancedRows.Clear();
    }
}
