using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

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
    public RectTransform leftMaskTransform = null;

    public Leaderboard leaderboard = null;
    private TwitchLeaderboardTable mainTable = null;
    private TwitchLeaderboardTableSolo soloTable = null;
    private TwitchLeaderboardStats statsTable = null;

    private void Start()
    {
        if (leaderboard == null)
        {
            return;
        }

        if (leaderboard.Success)
        {
            retryTransform.gameObject.SetActive(false);
        }

        statsTable = Instantiate<TwitchLeaderboardStats>(twitchLeaderboardStatsPrefab);
        statsTable.leaderboard = leaderboard;
        
        if (leaderboard.Count > 0)
        {
            mainTable = Instantiate<TwitchLeaderboardTable>(twitchLeaderboardTablePrefab);
            mainTable.leaderboard = leaderboard;
        }

        if (leaderboard.SoloCount > 0)
        {
            soloTable = Instantiate<TwitchLeaderboardTableSolo>(twitchLeaderboardTableSoloPrefab);
            soloTable.leaderboard = leaderboard;
            leftMaskTransform.gameObject.SetActive(true);

            bool prioritiseSolo = (leaderboard.SoloSolver != null);
            int countOnRight = prioritiseSolo ? leaderboard.SoloCount : leaderboard.Count;
            int countOnLeft = prioritiseSolo ? leaderboard.Count : leaderboard.SoloCount;
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
            if ((leaderboard.Count - statsTable.entriesLess) > mainTable.maximumRowCount)
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
