using System;
using System.IO;
using UnityEngine;

public class MiscellaneousMessageResponder : MessageResponder
{
    public Leaderboard leaderboard = null;
    public int moduleCountBonus = 0;

    [HideInInspector]
    public MonoBehaviour bombComponent = null;

    protected override void OnMessageReceived(string userNickName, string userColorCode, string text)
    {
        if (text.Equals("!cancel", StringComparison.InvariantCultureIgnoreCase))
        {
            _coroutineCanceller.SetCancel();
            return;
        }
        else if (text.Equals("!stop", StringComparison.InvariantCultureIgnoreCase))
        {
            _coroutineCanceller.SetCancel();
            _coroutineQueue.CancelFutureSubcoroutines();
            return;
        }
        else if (text.Equals("!manual", StringComparison.InvariantCultureIgnoreCase) ||
                 text.Equals("!help", StringComparison.InvariantCultureIgnoreCase))
        {
            _ircConnection.SendMessage( string.Format("!{0} manual [link to module {0}'s manual] | Go to {1} to get the vanilla manual for KTaNE", UnityEngine.Random.Range(1, 100), TwitchPlaysService.urlHelper.VanillaManual) );
            _ircConnection.SendMessage(string.Format("!{0} help [commands for module {0}] | Go to {1} to get the command reference for TP:KTaNE (multiple pages, see the menu on the right)", UnityEngine.Random.Range(1, 100), TwitchPlaysService.urlHelper.CommandReference));
            return;
        }
        else if (text.StartsWith("!bonusscore", StringComparison.InvariantCultureIgnoreCase))
        {
            string[] parts = text.Split(' ');
            if (parts.Length < 3)
            {
                return;
            }
            string playerrewarded = parts[1];
            int scorerewarded;
            if (!int.TryParse(parts[2], out scorerewarded))
            {
                return;
            }
            if (UserAccess.HasAccess(userNickName, AccessLevel.SuperUser))
            {
                _ircConnection.SendMessage(string.Format("{0} awarded {1} points by {2}", parts[1], parts[2], userNickName));
                Color usedColor = new Color(.31f, .31f, .31f);
                leaderboard.AddScore(playerrewarded, usedColor, scorerewarded);
            }
            else
            {
                scorerewarded = Mathf.Abs(scorerewarded);
                _ircConnection.SendMessage(string.Format("{0} lost {1} points", userNickName, parts[2]));
                Color usedColor = new Color(.31f, .31f, .31f);
                leaderboard.AddScore(playerrewarded, usedColor, -scorerewarded);
            }
            return;
        }
        else if (text.StartsWith("!reward", StringComparison.InvariantCultureIgnoreCase))
        {
            if (UserAccess.HasAccess(userNickName, AccessLevel.SuperUser))
            {
                string[] parts = text.Split(' ');
                moduleCountBonus = Int32.Parse(parts[1]);
                TwitchPlaySettings.SetRewardBonus(moduleCountBonus);
            }
        }        
        else if (text.StartsWith("!rank", StringComparison.InvariantCultureIgnoreCase))
        {
            Leaderboard.LeaderboardEntry entry = null;
            if (text.Length > 6)
            {
                string[] parts = text.Split(' ');
                int desiredRank;
                if (parts[1].Equals("solo", StringComparison.InvariantCultureIgnoreCase) && int.TryParse(parts[2], out desiredRank))
                {
                    leaderboard.GetSoloRank(desiredRank, out entry);
                }
                else if (int.TryParse(parts[1], out desiredRank))
                {
                    leaderboard.GetRank(desiredRank, out entry);
                }
                else
                {
                    return;
                }
                if (entry == null)
                {
                    _ircConnection.SendMessage(TwitchPlaySettings.data.RankTooLow);
                    return;
                }
            }
            if (entry == null)
            {
                leaderboard.GetRank(userNickName, out entry);
            }
            if (entry != null)
            {
                string txtSolver = "";
                string txtSolo = ".";
                if (entry.TotalSoloClears > 0)
                {
                    TimeSpan recordTimeSpan = TimeSpan.FromSeconds(entry.RecordSoloTime);
                    txtSolver = TwitchPlaySettings.data.SolverAndSolo;
                    txtSolo = string.Format(TwitchPlaySettings.data.SoloRankQuery, entry.SoloRank, (int)recordTimeSpan.TotalMinutes, recordTimeSpan.Seconds);
                }
                _ircConnection.SendMessage(string.Format(TwitchPlaySettings.data.RankQuery, entry.UserName, entry.Rank, entry.SolveCount, entry.StrikeCount, txtSolver, txtSolo));
            }
            else
            {
                _ircConnection.SendMessage(string.Format(TwitchPlaySettings.data.DoYouEvenPlayBro, userNickName));
            }
            return;
        }
        else if (text.Equals("!log", StringComparison.InvariantCultureIgnoreCase) || text.Equals("!analysis", StringComparison.InvariantCultureIgnoreCase))
        {
            TwitchPlaysService.logUploader.PostToChat("Analysis for the previous bomb: {0}");
            return;
        }
        else if (text.Equals("!shorturl", StringComparison.InvariantCultureIgnoreCase))
        {
            _ircConnection.SendMessage((TwitchPlaysService.urlHelper.ToggleMode()) ? "Enabling shortened URLs" : "Disabling shortened URLs");
        }
        else if (text.Equals("!about", StringComparison.InvariantCultureIgnoreCase))
        {
            _ircConnection.SendMessage("Twitch Plays: KTaNE is an alternative way of playing !ktane. Unlike the original game, you play as both defuser and expert, and defuse the bomb by sending special commands to the chat. Try !help for more information!");
            return;
        }
        else if (text.Equals("!ktane", StringComparison.InvariantCultureIgnoreCase))
        {
            _ircConnection.SendMessage("Keep Talking and Nobody Explodes is developed by Steel Crate Games. It's available for Windows PC, Mac OS X, PlayStation VR, Samsung Gear VR and Google Daydream. See http://www.keeptalkinggame.com/ for more information!");
            return;
        }
        else if (text.StartsWith("!add ", StringComparison.InvariantCultureIgnoreCase) || text.StartsWith("!remove ", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!UserAccess.HasAccess(userNickName, AccessLevel.Mod, true))
            {
                return;
            }

            bool add = text.StartsWith("!add ", StringComparison.InvariantCultureIgnoreCase);
            string[] split = text.Split(' ');
            if (split.Length != 3)
            {
                return;
            }
            AccessLevel level = AccessLevel.User;
            switch (split[1].ToLowerInvariant())
            {
                case "mod":
                case "moderator":
                    level = UserAccess.HasAccess(userNickName, AccessLevel.SuperUser) ? AccessLevel.Mod : AccessLevel.User;
                    break;
                case "admin":
                case "administrator":
                    level = UserAccess.HasAccess(userNickName, AccessLevel.SuperUser) ? AccessLevel.Admin : AccessLevel.User;
                    break;
                case "superadmin":
                case "superuser":
                case "super-user":
                case "super-admin":
                case "super-mod":
                case "supermod":
                    level = UserAccess.HasAccess(userNickName, AccessLevel.SuperUser) ? AccessLevel.SuperUser : AccessLevel.User;
                    break;

                
                case "defuser":
                    level = AccessLevel.Defuser;
                    break;
                case "no-points":
                case "no-score":
                case "noscore":
                case "nopoints":
                    level = AccessLevel.NoPoints;
                    break;
            }
            if (level == AccessLevel.User)
            {
                return;
            }


            if (add)
            {
                UserAccess.AddUser(split[2], level);
                UserAccess.WriteAccessList();
                _ircConnection.SendMessage(string.Format("/me Added {0} as {1}", split[2], level));
            }
            else
            {
                if (level == AccessLevel.SuperUser && userNickName.Equals(split[2]))
                {
                    _ircConnection.SendMessage(string.Format("/me Sorry @{0}, you Can't remove yourself as Super User.",userNickName));
                    return; //Prevent locking yourself out.
                }
                UserAccess.RemoveUser(split[2], level);
                UserAccess.WriteAccessList();
                _ircConnection.SendMessage(string.Format("/me Removed {0} from {1}", split[2], level));
            }
        }


        if (UserAccess.HasAccess(userNickName, AccessLevel.SuperUser))
        {
            if (text.Equals("!reloaddata", StringComparison.InvariantCultureIgnoreCase))
            {
                ModuleData.LoadDataFromFile();
                TwitchPlaySettings.LoadDataFromFile();
                UserAccess.LoadAccessList();
                _ircConnection.SendMessage("Data reloaded");
            }
            else if (text.Equals("!enabletwitchplays", StringComparison.InvariantCultureIgnoreCase))
            {
                _ircConnection.SendMessage("Twitch Plays Enabled");
                TwitchPlaySettings.data.EnableTwitchPlaysMode = true;
                TwitchPlaySettings.WriteDataToFile();
                EnableDisableInput();
            }
            else if (text.Equals("!disabletwitchplays", StringComparison.InvariantCultureIgnoreCase))
            {
                _ircConnection.SendMessage("Twitch Plays Disabled");
                TwitchPlaySettings.data.EnableTwitchPlaysMode = false;
                TwitchPlaySettings.WriteDataToFile();
                EnableDisableInput();
            }
            else if (text.Equals("!enableinteractivemode", StringComparison.InvariantCultureIgnoreCase))
            {
                _ircConnection.SendMessage("Interactive Mode Enabled");
                TwitchPlaySettings.data.EnableInteractiveMode = true;
                TwitchPlaySettings.WriteDataToFile();
                EnableDisableInput();
            }
            else if (text.Equals("!disableinteractivemode", StringComparison.InvariantCultureIgnoreCase))
            {
                _ircConnection.SendMessage("Interactive Mode Disabled");
                TwitchPlaySettings.data.EnableInteractiveMode = false;
                TwitchPlaySettings.WriteDataToFile();
                EnableDisableInput();
            }
            else if (text.Equals("!solveunsupportedmodules", StringComparison.InvariantCultureIgnoreCase))
            {
                _ircConnection.SendMessage("Solving unsupported modules.");
                TwitchComponentHandle.SolveUnsupportedModules();
            }
            else if (text.Equals("!removesolvebasedmodules", StringComparison.InvariantCultureIgnoreCase))
            {
                _ircConnection.SendMessage("Removing Solve based modules");
                TwitchComponentHandle.RemoveSolveBasedModules();
            }
        }
    }

    private void EnableDisableInput()
    {
        if (!BombMessageResponder.EnableDisableInput())
        {
            return;
        }
        if (TwitchComponentHandle.SolveUnsupportedModules())
        {
            _ircConnection.SendMessage("Some modules were automatically solved to prevent problems with defusing this bomb.");
        }
    }

}
