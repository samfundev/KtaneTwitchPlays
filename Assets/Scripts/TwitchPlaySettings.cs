using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;

public class TwitchPlaySettingsData
{
    public int SettingsVersion = 0;

    public bool EnableRewardMultipleStrikes = true;
    public bool EnableMissionBinder = true;
    public bool EnableFreeplayBriefcase = true;
    public bool EnableSoloPlayMode = true;
    public bool ForceMultiDeckerMode = false;
    public bool EnableRetryButton = true;
    public bool EnableTwitchPlaysMode = true;
    public bool EnableInteractiveMode = false;
    public int BombLiveMessageDelay = 0;
    public int ClaimCooldownTime = 30;
    public int ModuleClaimLimit = 2;

    public bool AllowSnoozeOnly = false;

    public string TPSharedFolder = Path.Combine(Application.persistentDataPath, "TwitchPlaysShared");
    public string TPSolveStrikeLog = "TPLog.txt";

    public string InvalidCommand = "Sorry @{0}, that command is invalid.";

    public string AwardSolve = "/me VoteYea {1} solved Module {0} ({3})! +{2} points. rooDuck";
    public string AwardStrike = "/me VoteNay Module {0} ({6}) got {1} strike{2}! {7} points from {4}{5} rooBooli";

    public string BombLiveMessage = "The next bomb is now live! Start sending your commands! MrDestructoid";
    public string MultiBombLiveMessage = "The next set of bombs are now live! Start sending your commands! MrDestructoid";

    public string BombExplodedMessage = "KAPOW KAPOW The bomb has exploded, with {0} remaining! KAPOW KAPOW";

    public string BombDefusedMessage = "PraiseIt PraiseIt The bomb has been defused, with {0} remaining!";
    public string BombDefusedBonusMessage = " {0} reward points to everyone who helped with this success.";
    public string BombDefusedFooter = " PraiseIt PraiseIt";

    public string BombSoloDefusalMessage = "PraiseIt PraiseIt {0} completed a solo defusal in {1}:{2:00}!";
    public string BombSoloDefusalNewRecordMessage = " It's a new record! (Previous record: {0}:{1:00})";
    public string BombSoloDefusalFooter = " PraiseIt PraiseIt";

    public string BombAbortedMessage = "VoteNay VoteNay The bomb was aborted, with {0} remaining! VoteNay VoteNay";

    public string RankTooLow = "Nobody here with that rank!";

    public string SolverAndSolo = "solver ";
    public string SoloRankQuery = ", and #{0} solo with a best time of {1}:{2:00.0}";
    public string RankQuery = "SeemsGood {0} is #{1} {4}with {2} solves and {3} strikes{5}";

    public string DoYouEvenPlayBro = "FailFish {0}, do you even play this game?";

    public string TooManyClaimed = "ItsBoshyTime Sorry, {0} , you may only have {1} claimed modules.";
}

public static class TwitchPlaySettings
{
    public static int SettingsVersion = 4;
    public static TwitchPlaySettingsData data;

    private static List<string> Players = new List<string>();
    private static int ClearReward = 0;

    public static void WriteDataToFile()
    {
        string path = Path.Combine(Application.persistentDataPath, usersSavePath);
        Debug.LogFormat("TwitchPlayStrings: Writing file {0}", path);
        try
        {
            File.WriteAllText(path,JsonConvert.SerializeObject(data, Formatting.Indented));
        }
        catch (FileNotFoundException)
        {
            Debug.LogWarningFormat("TwitchPlayStrings: File {0} was not found.", path);
            return;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return;
        }
        Debug.LogFormat("TwitchPlayStrings: Writing of file {0} completed successfully", path);
    }

    public static bool LoadDataFromFile()
    {
        string path = Path.Combine(Application.persistentDataPath, usersSavePath);
        try
        {
            Debug.Log("TwitchPlayStrings: Loading Custom strings data from file: " + path);
            data = JsonConvert.DeserializeObject<TwitchPlaySettingsData>(File.ReadAllText(path));
            if (SettingsVersion != data.SettingsVersion)
            {
                WriteDataToFile();
            }
        }
        catch (FileNotFoundException)
        {
            Debug.LogWarningFormat("TwitchPlayStrings: File {0} was not found.", path);
            data = new TwitchPlaySettingsData();
            WriteDataToFile();
            return false;
        }
        catch (Exception ex)
        {
            data = new TwitchPlaySettingsData();
            Debug.LogException(ex);
            return false;
        }
        return true;
    }

    private static bool CreateSharedDirectory()
    {
        if (string.IsNullOrEmpty(data.TPSharedFolder))
        {
            return false;
        }
        try
        {
            if (!Directory.Exists(data.TPSharedFolder))
            {
                Directory.CreateDirectory(data.TPSharedFolder);
            }
            return Directory.Exists(data.TPSharedFolder);
        }
        catch (Exception ex)
        {
            Debug.LogFormat("TwitchPlaysStrings: Failed to Create shared Directory due to Exception :{0}, Stack Trace: {1}", ex.Message, ex.StackTrace);
            return false;
        }
    }

    public static void AppendToSolveStrikeLog(string RecordMessageTone)
    {
        if (!CreateSharedDirectory() || string.IsNullOrEmpty(data.TPSolveStrikeLog))
        {
            return;
        }
        try
        {
            using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(Path.Combine(TwitchPlaySettings.data.TPSharedFolder, TwitchPlaySettings.data.TPSolveStrikeLog), true))
            {
                file.WriteLine(RecordMessageTone);
            }
        }
        catch (Exception ex)
        {
            Debug.LogFormat("TwitchPlaysStrings: Failed to log due to Exception: {0}, Stack Trace: {1}", ex.Message, ex.StackTrace);
        }
    }

    public static void AppendToPlayerLog(string userNickName)
    {
        if (!Players.Contains(userNickName) && !UserAccess.HasAccess(userNickName, AccessLevel.NoPoints))
        {
            Players.Add(userNickName);
        }
    }

    public static void ClearPlayerLog()
    {
        Players.Clear();
        ClearReward = 0;
    }

    public static string GiveBonusPoints(Leaderboard leaderboard)
    {
        if (ClearReward == 0 || Players.Count == 0)
        {
            return data.BombDefusedFooter;
        }
        ClearReward = Mathf.CeilToInt(((float)ClearReward) / Players.Count);
        foreach (string player in Players)
        {
            leaderboard.AddScore(player, ClearReward);
        }
        ClearPlayerLog();
        return string.Format(data.BombDefusedBonusMessage, ClearReward) + data.BombDefusedFooter;
    }

    public static void AddRewardBonus(int bonus)
    {
        ClearReward += bonus;
    }

    public static void SetRewardBonus(int moduleCountBonus)
    {
        ClearReward = moduleCountBonus;
    }

    public static string usersSavePath = "TwitchPlaySettings.json";
}
