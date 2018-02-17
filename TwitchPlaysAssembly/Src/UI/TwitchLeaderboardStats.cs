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
    
    private void Start()
    {
        if (Leaderboard.Instance == null)
        {
            return;
        }

        int bombCount = Leaderboard.Instance.BombsCleared;
        int totalSolveScore = 0;
	    Leaderboard.Instance.GetTotalSolveStrikeCounts(out int solveCount, out int strikeCount, out int SolveScore);

            totalSolveScore = SolveScore;

        int sessionSolveScore = 0;
            sessionSolveScore = SolveScore - Leaderboard.Instance.OldScore;

        totalBombCountText.text = bombCount.ToString();
        totalSolveCountText.text = solveCount.ToString();
        totalStrikeCountText.text = strikeCount.ToString();
        totalRateText.text = string.Format("{0}", totalSolveScore);

        sessionBombCountText.text = (bombCount - Leaderboard.Instance.OldBombsCleared).ToString();
        sessionSolveCountText.text = (solveCount - Leaderboard.Instance.OldSolves).ToString();
        sessionStrikeCountText.text = (strikeCount - Leaderboard.Instance.OldStrikes).ToString();
        sessionRateText.text = string.Format("{0}", sessionSolveScore);
    }

    private void OnDisable()
    {

    }
}
