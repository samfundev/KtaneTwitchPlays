using System;
using UnityEngine;

public class MiscellaneousMessageResponder : MessageResponder
{
    public Leaderboard leaderboard = null;

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
        else if (text.Equals("!manual", StringComparison.InvariantCultureIgnoreCase))
        {
            _ircConnection.SendMessage( string.Format("!{0} manual [link to module {0}'s manual] | Go to {1} to get the vanilla manual for KTaNE", UnityEngine.Random.Range(1, 100), TwitchPlaysService.urlHelper.VanillaManual) );
            return;
        }
        else if (text.Equals("!help", StringComparison.InvariantCultureIgnoreCase))
        {
            _ircConnection.SendMessage( string.Format("!{0} help [commands for module {0}] | Go to {1} to get the command reference for TP:KTaNE (multiple pages, see the menu on the right)", UnityEngine.Random.Range(1, 100), TwitchPlaysService.urlHelper.CommandReference) );
            return;
        }
        else if (text.StartsWith("!rank", StringComparison.InvariantCultureIgnoreCase))
        {
            Leaderboard.LeaderboardEntry entry = null;
            if (text.Length > 6)
            {
                string[] parts = text.Split(' ');
                int desiredRank;
                if ( parts[1].Equals("solo", StringComparison.InvariantCultureIgnoreCase) && int.TryParse(parts[2], out desiredRank) )
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
                    _ircConnection.SendMessage("Nobody here with that rank!");
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
                    txtSolver = "solver ";
                    txtSolo = string.Format(", and #{0} solo with a best time of {1}:{2:00.0}", entry.SoloRank, (int)recordTimeSpan.TotalMinutes, recordTimeSpan.Seconds);
                }
                _ircConnection.SendMessage(string.Format("SeemsGood {0} is #{1} {4}with {2} solves and {3} strikes{5}", entry.UserName, entry.Rank, entry.SolveCount, entry.StrikeCount, txtSolver, txtSolo));
            }
            else
            {
                _ircConnection.SendMessage(string.Format("FailFish {0}, do you even play this game?", userNickName));
            }
            return;
        }
        else if ( (text.Equals("!log", StringComparison.InvariantCultureIgnoreCase)) ||
            (text.Equals("!analysis", StringComparison.InvariantCultureIgnoreCase)) )
        {
            TwitchPlaysService.logUploader.PostToChat("Analysis for the previous bomb: {0}");
            return;
        }
        else if (text.Equals("!shorturl", StringComparison.InvariantCultureIgnoreCase))
        {
            _ircConnection.SendMessage(
                (TwitchPlaysService.urlHelper.ToggleMode()) ?
                "Enabling shortened URLs" :
                "Disabling shortened URLs"
            );
        }
        else if (text.Equals("!about", StringComparison.InvariantCultureIgnoreCase))
        {
            _ircConnection.SendMessage("Twitch Plays: KTaNE is an alternative way of playing !ktane. Unlike the original game, you play as both defuser and expert, and defuse the bomb by sending special commands to the chat room. Try !help for more information!");
            return;
        }
        else if (text.Equals("!ktane", StringComparison.InvariantCultureIgnoreCase))
        {
            _ircConnection.SendMessage("Keep Talking and Nobody Explodes is developed by Steel Crate Games. It's available for Windows PC, Mac OS X, PlayStation VR, Samsung Gear VR and Google Daydream. See http://www.keeptalkinggame.com/ for more information!");
            return;
        }
    }
}
