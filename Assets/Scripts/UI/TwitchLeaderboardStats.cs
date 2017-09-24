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
        int SolveScore = 0;
        int totalSolveScore = 0;
        leaderboard.GetTotalSolveStrikeCounts(out solveCount, out strikeCount,out SolveScore);

            totalSolveScore = SolveScore;

        int sessionSolveScore = 0;
            sessionSolveScore = SolveScore - leaderboard.OldScore;


        totalBombCountText.text = bombCount.ToString();
        totalSolveCountText.text = solveCount.ToString();
        totalStrikeCountText.text = strikeCount.ToString();
        totalRateText.text = string.Format("{0}", totalSolveScore);

        sessionBombCountText.text = (bombCount - leaderboard.OldBombsCleared).ToString();
        sessionSolveCountText.text = (solveCount - leaderboard.OldSolves).ToString();
        sessionStrikeCountText.text = (strikeCount - leaderboard.OldStrikes).ToString();
        sessionRateText.text = string.Format("{0}", sessionSolveScore);
    }

    private void OnDisable()
    {

    }
}
