using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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

    public string InvalidCommand = "Sorry @{0}, that command for {1} ({2}) is invalid.";
    public string CommandError = "Sorry @{0}, Module {1} ({2}) responded with: {3}";

    public string AwardSolve = "VoteYea {1} solved Module {0} ({3})! +{2} points. VoteYea";
    public string AwardStrike = "VoteNay Module {0} ({6}) got {1} strike{2}! {7} points from {4}{5} VoteNay";

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

    public string TooManyClaimed = "ItsBoshyTime Sorry, {0}, you may only have {1} claimed modules.";

    private bool ValidateString(ref string input, string def, int parameters)
    {
        MatchCollection matches = Regex.Matches(input, @"(?<!\{)\{([0-9]+).*?\}(?!})");
        int count = matches.Count > 0 
                ? matches.Cast<Match>().Max(m => int.Parse(m.Groups[1].Value)) + 1
                : 0;

        DebugHelper.Log("TwitchPlaySettings.ValidateString( {0}, {1}, {2} ) = {3}", input, def, parameters, count == parameters);

        if (count != parameters)
        {
            input = def;
            return false;
        }
        return true;
    }

    public bool ValidateStrings()
    {
        TwitchPlaySettingsData data = new TwitchPlaySettingsData();
        bool valid = true;

        valid &= ValidateString(ref InvalidCommand, data.InvalidCommand, 3);
        valid &= ValidateString(ref CommandError, data.CommandError, 4);

        valid &= ValidateString(ref AwardSolve, data.AwardSolve, 4);
        valid &= ValidateString(ref AwardStrike, data.AwardStrike, 8);

        valid &= ValidateString(ref BombLiveMessage, data.BombLiveMessage, 0);
        valid &= ValidateString(ref MultiBombLiveMessage, data.MultiBombLiveMessage, 0);

        valid &= ValidateString(ref BombExplodedMessage, data.BombExplodedMessage, 1);

        valid &= ValidateString(ref BombDefusedMessage, data.BombDefusedMessage, 1);
        valid &= ValidateString(ref BombDefusedBonusMessage, data.BombDefusedBonusMessage, 1);
        valid &= ValidateString(ref BombDefusedFooter, data.BombDefusedFooter, 0);

        valid &= ValidateString(ref BombSoloDefusalMessage, data.BombSoloDefusalMessage, 3);
        valid &= ValidateString(ref BombSoloDefusalNewRecordMessage, data.BombSoloDefusalNewRecordMessage, 2);
        valid &= ValidateString(ref BombSoloDefusalFooter, data.BombSoloDefusalFooter, 0);

        valid &= ValidateString(ref BombAbortedMessage, data.BombAbortedMessage, 1);

        valid &= ValidateString(ref RankTooLow, data.RankTooLow, 0);

        valid &= ValidateString(ref SolverAndSolo, data.SolverAndSolo, 0);
        valid &= ValidateString(ref SoloRankQuery, data.SoloRankQuery, 3);
        valid &= ValidateString(ref RankQuery, data.RankQuery, 6);

        valid &= ValidateString(ref DoYouEvenPlayBro, data.DoYouEvenPlayBro, 1);

        valid &= ValidateString(ref TooManyClaimed, data.TooManyClaimed, 2);

        return valid;
    }
}

public static class TwitchPlaySettings
{
    public static int SettingsVersion = 5;  //Bump this up each time a new setting is added.
    public static TwitchPlaySettingsData data;

    private static List<string> Players = new List<string>();
    private static int ClearReward = 0;

    public static void WriteDataToFile()
    {
        string path = Path.Combine(Application.persistentDataPath, usersSavePath);
        DebugHelper.Log("TwitchPlayStrings: Writing file {0}", path);
        try
        {
            File.WriteAllText(path,JsonConvert.SerializeObject(data, Formatting.Indented));
        }
        catch (FileNotFoundException)
        {
            DebugHelper.LogWarning("TwitchPlayStrings: File {0} was not found.", path);
            return;
        }
        catch (Exception ex)
        {
            DebugHelper.LogException(ex);
            return;
        }
        DebugHelper.Log("TwitchPlayStrings: Writing of file {0} completed successfully", path);
    }

    public static bool LoadDataFromFile()
    {
        string path = Path.Combine(Application.persistentDataPath, usersSavePath);
        try
        {
            DebugHelper.Log("TwitchPlayStrings: Loading Custom strings data from file: {0}", path);
            data = JsonConvert.DeserializeObject<TwitchPlaySettingsData>(File.ReadAllText(path));
            bool result = data.ValidateStrings();
            result &= SettingsVersion == data.SettingsVersion;
            if (!result)
            {
                WriteDataToFile();
            }
        }
        catch (FileNotFoundException)
        {
            DebugHelper.LogWarning("TwitchPlayStrings: File {0} was not found.", path);
            data = new TwitchPlaySettingsData();
            WriteDataToFile();
            return false;
        }
        catch (Exception ex)
        {
            data = new TwitchPlaySettingsData();
            DebugHelper.LogException(ex);
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
            DebugHelper.LogException(ex, "TwitchPlaysStrings: Failed to Create shared Directory due to Exception:");
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
            using (StreamWriter file =
                new StreamWriter(Path.Combine(data.TPSharedFolder, data.TPSolveStrikeLog), true))
            {
                file.WriteLine(RecordMessageTone);
            }
        }
        catch (Exception ex)
        {
			DebugHelper.LogException(ex, "TwitchPlaysStrings: Failed to log due to Exception:");
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
