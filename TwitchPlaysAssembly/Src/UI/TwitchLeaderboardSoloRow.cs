using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TwitchLeaderboardSoloRow : MonoBehaviour
{
    [Header("Text Elements")]
    public Text positionText = null;
    public Text userNameText = null;
    public Text totalClearsText = null;
    public Text recordTimeText = null;
    public Text avgSolveTimeText = null;

    [Header("Values")]
    public int position = 0;
    public float delay = 0.0f;
    public Leaderboard.LeaderboardEntry leaderboardEntry = null;

    private CanvasGroup _canvasGroup = null;
    private Animator _animator = null;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _animator = GetComponent<Animator>();

        _canvasGroup.alpha = 0.0f;
    }

    private IEnumerator Start()
    {
        positionText.text = position.ToString();

        if (leaderboardEntry != null)
        {
            TimeSpan recordTimeSpan = TimeSpan.FromSeconds(leaderboardEntry.RecordSoloTime);
            TimeSpan averageTimeSpan = TimeSpan.FromSeconds(leaderboardEntry.TimePerSoloSolve);
            
            userNameText.text = leaderboardEntry.UserName;
            userNameText.color = leaderboardEntry.UserColor;
            totalClearsText.text = leaderboardEntry.TotalSoloClears.ToString();
            recordTimeText.text = string.Format("{0}:{1:00}", (int)recordTimeSpan.TotalMinutes, recordTimeSpan.TotalSeconds % 60);
            avgSolveTimeText.text = string.Format("{0}:{1:00.0}", (int)averageTimeSpan.TotalMinutes, averageTimeSpan.TotalSeconds % 60);
        }

        yield return new WaitForSeconds(delay);
        _animator.enabled = true;
    }
}
