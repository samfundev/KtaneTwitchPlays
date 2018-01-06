using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;

public class TwitchPlaySettingsData
{
    public int SettingsVersion = 0;

    public bool EnableRewardMultipleStrikes = true;
    public bool EnableMissionBinder = true;
    public bool EnableFreeplayBriefcase = true;
    public bool EnableFreeplayNeedy = true;
    public bool EnableFreeplayHardcore = true;
    public bool EnableFreeplayModsOnly = true;
	public bool EnableRunCommand = true;
	public bool EnableSoloPlayMode = true;
    public bool ForceMultiDeckerMode = false;
    public bool EnableRetryButton = true;
    public bool EnableTwitchPlaysMode = true;
    public bool EnableInteractiveMode = false;
	public bool EnableAutomaticEdgework = false;
    public int BombLiveMessageDelay = 0;
    public int ClaimCooldownTime = 30;
    public int ModuleClaimLimit = 2;
	public bool EnableTwitchPlayShims = true;
	public float UnsubmittablePenaltyPercent = 0.3f;
	public Color UnclaimedColor = new Color(0.39f, 0.25f, 0.64f);
    public bool AllowTurnTheKeyEarlyLate = true;
    public bool DisableTurnTheKeysSoftLock = true;
    public bool EnforceSolveAllBeforeTurningKeys = true;

	public Dictionary<string, string> CustomMissions = new Dictionary<string, string>();
	public List<string> ProfileWhitelist = new List<string>();

	public string TwitchBotColorOnQuit = string.Empty;

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
    public string RankQuery = "SeemsGood {0} is #{1} {4}with {2} solves and {3} strikes and a total score of {6}{5}";

    public string DoYouEvenPlayBro = "FailFish {0}, do you even play this game?";

    public string TurnBombOnSolve = "Turning to the other side when Module {0} ({1}) is solved";
    public string CancelBombTurn = "Bomb turn on Module {0} ({1}) solve cancelled";

    public string ModuleClaimed = "{1} has claimed Module {0} ({2}).";
    public string ModuleUnclaimed = "{1} has released Module {0} ({2}).";

    public string AssignModule = "Module {0} ({3}) assigned to {1} by {2}";
    public string ModuleReady = "{1} says module {0} ({2}) is ready to be submitted";

    public string TakeModule = "@{0}, {1} wishes to take Module {2} ({3}). It will be freed up in one minute unless you type !{2} mine.";
    public string TakeInProgress = "Sorry @{0}, There is already a takeover attempt for Module {1} ({2}) in progress.";
    public string ModuleAbandoned = "{1} has released Module {0} ({2}).";
    public string ModuleIsMine = "{0} confirms he/she is still working on {1} ({2})";
    public string TooManyClaimed = "ItsBoshyTime Sorry, {0}, you may only have {1} claimed modules.";
    public string ModulePlayer = "Module {0} ({2}) was claimed by {1}";
    public string AlreadyClaimed = "Sorry @{2}, Module {0} ({3}) is currently claimed by {1}. If you think they have abandoned it, you may type !{0} take to free it up.";

    public string OwnedModule = "({0} - \"{1}\")";
    public string OwnedModuleList = "@{0}, your claimed modules are {1}";
    public string NoOwnedModules = "Sorry @{0}, you have no claimed modules.";

    public string TwitchPlaysDisabled = "Sorry @{0}, Twitch plays is only enabled for Authorized defusers";
    public string MissionBinderDisabled = "Sorry @{0}, Only authorized users may access the mission binder";
    public string FreePlayDisabled = "Sorry @{0}, Only authorized users may access the freeplay briefcase";
    public string FreePlayNeedyDisabled = "Sorry @{0}, Only authorized users may enable/disable Needy modules";
    public string FreePlayHardcoreDisabled = "Sorry @{0}, Only authorized users may enable/disable Hardcore mode";
    public string FreePlayModsOnlyDisabled = "Sorry @{0}, Only authorized users may enable/disable Mods only mode";
	public string RunCommandDisabled = "Sorry @{0}, Only authorized users may use the !run command.";
	public string ProfileCommandDisabled = "Sorry @{0}, profile management is currently disabled.";
	public string RetryInactive = "Sorry, retry is inactive. Returning to hallway instead.";

	public string ProfileActionUseless = "That profile ({0}) is already {1}.";
	public string ProfileNotWhitelisted = "That profile ({0}) can't not be enabled/disabled.";
	public string ProfileListEnabled = "Currently enabled profiles: {0}";
	public string ProfileListAll = "All profiles: {0}";

	public string AddedUserPower = "Added access levels ({0}) to user \"{1}\"";
    public string RemoveUserPower = "Removed access levels ({0}) from user \"{1}\"";

    public string BombHelp = "The Bomb: !bomb hold [pick up] | !bomb drop | !bomb turn [turn to the other side] | !bomb edgework [show the widgets on the sides] | !bomb top [show one side; sides are Top/Bottom/Left/Right | !bomb time [time remaining] | !bomb timestamp [bomb start time]";
    public string BlankBombEdgework = "Not set, use !edgework <edgework> to set!\nUse !bomb edgework or !bomb edgework 45 to view the bomb edges.";
    public string BombEdgework = "Edgework: {0}";
    public string BombTimeRemaining = "panicBasket [{0}] out of [{1}].";
    public string BombTimeStamp = "The Date/Time this bomb started is {0:F}";
    public string BombDetonateCommand = "panicBasket This bomb's gonna blow!";

    public string GiveBonusPoints = "{0} awarded {1} points by {2}";

    public string UnsubmittableAnswerPenalty = "Sorry {0}, The answer for module {1} ({2}) couldn't be submitted! You lose {3} point{4}, please only submit correct answers.";

    public string UnsupportedNeedyWarning = "Warning: This bomb is unlikely to live long due to an uninteractable needy being present.";

    private bool ValidateString(ref string input, string def, int parameters, bool forceUpdate = false)
    {
        MatchCollection matches = Regex.Matches(input, @"(?<!\{)\{([0-9]+).*?\}(?!})");
        int count = matches.Count > 0 
                ? matches.Cast<Match>().Max(m => int.Parse(m.Groups[1].Value)) + 1
                : 0;

        if (count != parameters || forceUpdate)
        {
			DebugHelper.Log("TwitchPlaySettings.ValidateString( {0}, {1}, {2} ) - {3}", input, def, parameters, forceUpdate ? "Updated because of version breaking changes" : "Updated because parameters didn't match expected count.");

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
        valid &= ValidateString(ref RankQuery, data.RankQuery, 7);

        valid &= ValidateString(ref DoYouEvenPlayBro, data.DoYouEvenPlayBro, 1);

        valid &= ValidateString(ref TurnBombOnSolve, data.TurnBombOnSolve, 2);
        valid &= ValidateString(ref CancelBombTurn, data.CancelBombTurn, 2);

        valid &= ValidateString(ref ModuleClaimed, data.ModuleClaimed, 3);
        valid &= ValidateString(ref ModuleUnclaimed, data.ModuleUnclaimed, 3);

        valid &= ValidateString(ref AssignModule, data.AssignModule, 4);
        valid &= ValidateString(ref ModuleReady, data.ModuleReady, 3);

        valid &= ValidateString(ref TakeModule, data.TakeModule, 4);
        valid &= ValidateString(ref TakeInProgress, data.TakeInProgress, 3);
        valid &= ValidateString(ref ModuleAbandoned, data.ModuleAbandoned, 3);
        valid &= ValidateString(ref ModuleIsMine, data.ModuleIsMine, 3);
        valid &= ValidateString(ref TooManyClaimed, data.TooManyClaimed, 2);
        valid &= ValidateString(ref ModulePlayer, data.ModulePlayer, 3);
        valid &= ValidateString(ref AlreadyClaimed, data.AlreadyClaimed, 4);

        valid &= ValidateString(ref OwnedModule, data.OwnedModule, 2);
        valid &= ValidateString(ref OwnedModuleList, data.OwnedModuleList, 2);
        valid &= ValidateString(ref NoOwnedModules, data.NoOwnedModules, 1);

        valid &= ValidateString(ref TwitchPlaysDisabled, data.TwitchPlaysDisabled, 1);
        valid &= ValidateString(ref MissionBinderDisabled, data.MissionBinderDisabled, 1);
        valid &= ValidateString(ref FreePlayDisabled, data.FreePlayDisabled, 1);
        valid &= ValidateString(ref FreePlayNeedyDisabled, data.FreePlayNeedyDisabled, 1);
        valid &= ValidateString(ref FreePlayHardcoreDisabled, data.FreePlayHardcoreDisabled, 1);
        valid &= ValidateString(ref FreePlayModsOnlyDisabled, data.FreePlayModsOnlyDisabled, 1);
        valid &= ValidateString(ref RetryInactive, data.RetryInactive, 0);

        valid &= ValidateString(ref AddedUserPower, data.AddedUserPower, 2, SettingsVersion < 1);
        valid &= ValidateString(ref RemoveUserPower, data.RemoveUserPower, 2, SettingsVersion < 1);

        valid &= ValidateString(ref BombHelp, data.BombHelp, 0);
        valid &= ValidateString(ref BlankBombEdgework, data.BlankBombEdgework, 0);
        valid &= ValidateString(ref BombEdgework, data.BombEdgework, 1);
        valid &= ValidateString(ref BombTimeRemaining, data.BombTimeRemaining, 2);
        valid &= ValidateString(ref BombTimeStamp, data.BombTimeStamp, 1);
        valid &= ValidateString(ref BombDetonateCommand, data.BombDetonateCommand, 0);

        valid &= ValidateString(ref GiveBonusPoints, data.GiveBonusPoints, 3);

        valid &= ValidateString(ref UnsubmittableAnswerPenalty, data.UnsubmittableAnswerPenalty, 4);

        valid &= ValidateString(ref UnsupportedNeedyWarning, data.UnsupportedNeedyWarning, 0);

        return valid;
    }
}

public static class TwitchPlaySettings
{
    public static int SettingsVersion = 1;  //Bump this up each time there is a breaking file format change. (like a changed to the string formats themselves)
    public static TwitchPlaySettingsData data;

    private static List<string> Players = new List<string>();
    private static int ClearReward = 0;

    public static void WriteDataToFile()
    {
        string path = Path.Combine(Application.persistentDataPath, usersSavePath);
        DebugHelper.Log("TwitchPlayStrings: Writing file {0}", path);
        try
        {
            data.SettingsVersion = SettingsVersion;
			File.WriteAllText(path, SettingsConverter.Serialize(data));//JsonConvert.SerializeObject(data, Formatting.Indented));
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
			data = SettingsConverter.Deserialize<TwitchPlaySettingsData>(File.ReadAllText(path));//JsonConvert.DeserializeObject<TwitchPlaySettingsData>(File.ReadAllText(path));
            data.ValidateStrings();
            WriteDataToFile();
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

    public static void AppendToSolveStrikeLog(string RecordMessageTone, int copies=1)
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
                for (int i = 0; i < copies; i++)
                {
                    file.WriteLine(RecordMessageTone);
                }
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
        int ClearReward2 = Mathf.CeilToInt(ClearReward / (float) Players.Count);
		string message = string.Format(data.BombDefusedBonusMessage, ClearReward2) + data.BombDefusedFooter;
		foreach (string player in Players)
        {
            leaderboard.AddScore(player, ClearReward2);
        }
        ClearPlayerLog();
		return message;
    }

    public static void AddRewardBonus(int bonus)
    {
        ClearReward += bonus;
    }

    public static void SetRewardBonus(int moduleCountBonus)
    {
        ClearReward = moduleCountBonus;
    }

    public static int GetRewardBonus()
    {
        return ClearReward;
    }

    public static string usersSavePath = "TwitchPlaySettings.json";
}
