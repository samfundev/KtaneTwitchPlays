using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TwitchLeaderboardStats : MonoBehaviour
{
    [Header("Text Elements")]
    public Text totalBombCountText = null;
    public Text totalSolveCountText = null;
    public Text totalStrikeCountText = null;
    public Text totalRateText = null;
    public Text sessionBombCountText = null;
    public Text sessionSolveCountText = null;
    public Text sessionStrikeCountText = null;
    public Text sessionRateText = null;

    [Header("Values")]
    public bool defaultToRight = true;
    public int entriesLess = 5;

    public Leaderboard leaderboard = null;
    
    private void Start()
    {
        if (leaderboard == null)
        {
            return;
        }

        int bombCount = leaderboard.BombsCleared;
        int solveCount = 0;
        int strikeCount = 0;
        float totalSolveRate = 0.0f;
        leaderboard.GetTotalSolveStrikeCounts(out solveCount, out strikeCount);

        if (strikeCount == 0)
        {
            totalSolveRate = solveCount;
        }
        else
        {
            totalSolveRate = ((float)solveCount) / strikeCount;
        }

        float sessionSolveRate = 0.0f;
        if ((strikeCount - leaderboard.OldStrikes) == 0)
        {
            sessionSolveRate = solveCount - leaderboard.OldSolves;
        }
        else
        {
            sessionSolveRate = ((float)solveCount - leaderboard.OldSolves) / (strikeCount - leaderboard.OldStrikes);
        }

        totalBombCountText.text = bombCount.ToString();
        totalSolveCountText.text = solveCount.ToString();
        totalStrikeCountText.text = strikeCount.ToString();
        totalRateText.text = string.Format("{0:0.00}", totalSolveRate);

        sessionBombCountText.text = (bombCount - leaderboard.OldBombsCleared).ToString();
        sessionSolveCountText.text = (solveCount - leaderboard.OldSolves).ToString();
        sessionStrikeCountText.text = (strikeCount - leaderboard.OldStrikes).ToString();
        sessionRateText.text = string.Format("{0:0.00}", sessionSolveRate);
    }

    private void OnDisable()
    {

    }
}
