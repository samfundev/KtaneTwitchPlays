using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Assets.Scripts.Missions;
using UnityEngine;

[RequireComponent(typeof(KMGameCommands))]
[RequireComponent(typeof(KMGameInfo))]
public class MiscellaneousMessageResponder : MessageResponder
{
    public Leaderboard leaderboard = null;
    public int moduleCountBonus = 0;

    [HideInInspector]
    public BombComponent bombComponent = null;

	private KMGameCommands GameCommands;
	private KMGameInfo GameInfo;
	private KMGameInfo.State CurrentState = KMGameInfo.State.Transitioning;

	private void Start()
	{
		GameCommands = GetComponent<KMGameCommands>();
		GameInfo = GetComponent<KMGameInfo>();
		GameInfo.OnStateChange += delegate (KMGameInfo.State state)
		{
			CurrentState = state;
		};
	}

	string resolveMissionID(string targetID, out string failureMessage)
	{
	    failureMessage = null;
	    ModManager modManager = ModManager.Instance;
	    List<Mission> missions = modManager.ModMissions;

	    Mission mission = missions.FirstOrDefault(x => Regex.IsMatch(x.name, "mod_.+_" + Regex.Escape(targetID), RegexOptions.CultureInvariant | RegexOptions.IgnoreCase));
	    if (mission == null)
	    {
	        failureMessage = string.Format("Unable to find a mission with an ID of \"{0}\".", targetID);
	        return null;
	    }

	    List<string> availableMods = GameInfo.GetAvailableModuleInfo().Where(x => x.IsMod).Select(y => y.ModuleId).ToList();
	    if (MultipleBombs.Installed())
	        availableMods.Add("Multiple Bombs");
	    List<string> missingMods = new List<string>();

	    GeneratorSetting generatorSetting = mission.GeneratorSetting;
	    List<ComponentPool> componentPools = generatorSetting.ComponentPools;
	    foreach (ComponentPool componentPool in componentPools)
	    {
	        List<string> modTypes = componentPool.ModTypes;
	        if (modTypes == null || modTypes.Count == 0) continue;
	        missingMods.AddRange(modTypes.Where(x => !availableMods.Contains(x) && !missingMods.Contains(x)));
	    }
	    if (missingMods.Count > 0)
	    {
	        failureMessage = string.Format("Mission {0} was found, however, the following mods are not installed / loaded: {1}", targetID, string.Join(", ", missingMods.ToArray()));
            return null;
	    }
        
	    return mission.name;
	}

	class Distribution
	{
		public string displayName;
		public float vanilla;
		public float modded;
	}

	Dictionary<string, Distribution> distributions = new Dictionary<string, Distribution>()
	{
		{ "vanilla", new Distribution { vanilla = 1f, modded = 0f, displayName = "Vanilla" } },
		{ "mods", new Distribution { vanilla = 0f, modded = 1f, displayName = "Modded" } },
		{ "mixed", new Distribution { vanilla = 0.5f, modded = 0.5f, displayName = "Mixed" } },
		{ "mixedlight", new Distribution { vanilla = 0.67f, modded = 0.33f, displayName = "Mixed Light" } },
		{ "mixedheavy", new Distribution { vanilla = 0.33f, modded = 0.67f, displayName = "Mixed Heavy" } },
		{ "light", new Distribution { vanilla = 0.8f, modded = 0.2f, displayName = "Light" } },
		{ "heavy", new Distribution { vanilla = 0.2f, modded = 0.8f, displayName = "Heavy" } },
	};

	protected override void OnMessageReceived(string userNickName, string userColorCode, string text)
    {
		if (!text.StartsWith("!") || text.Equals("!")) return;
        text = text.Substring(1);

        string[] split = text.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        string textAfter = split.Skip(1).Join();
        if (text.Equals("cancel", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!IsAuthorizedDefuser(userNickName)) return;
            _coroutineCanceller.SetCancel();
            return;
        }
        else if (text.Equals("stop", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!IsAuthorizedDefuser(userNickName)) return;
            _coroutineCanceller.SetCancel();
            _coroutineQueue.CancelFutureSubcoroutines();
            return;
        }
        else if (text.Equals("manual", StringComparison.InvariantCultureIgnoreCase) ||
                 text.Equals("help", StringComparison.InvariantCultureIgnoreCase))
        {
            _ircConnection.SendMessage("!{0} manual [link to module {0}'s manual] | Go to {1} to get the vanilla manual for KTaNE", UnityEngine.Random.Range(1, 100), TwitchPlaysService.urlHelper.VanillaManual);
            _ircConnection.SendMessage("!{0} help [commands for module {0}] | Go to {1} to get the command reference for TP:KTaNE (multiple pages, see the menu on the right)", UnityEngine.Random.Range(1, 100), TwitchPlaysService.urlHelper.CommandReference);
            return;
        }
        else if (text.StartsWith("bonusscore", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!IsAuthorizedDefuser(userNickName)) return;
            if (split.Length < 3)
            {
                return;
            }
            string playerrewarded = split[1];
            if (!int.TryParse(split[2], out int scorerewarded))
            {
                return;
            }
            if (UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true))
            {
                _ircConnection.SendMessage(TwitchPlaySettings.data.GiveBonusPoints, split[1], split[2], userNickName);
                Color usedColor = new Color(.31f, .31f, .31f);
                leaderboard.AddScore(playerrewarded, usedColor, scorerewarded);
            }
            return;
        }
        else if (text.StartsWith("reward", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!IsAuthorizedDefuser(userNickName)) return;
            if (UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true))
            {
                moduleCountBonus = Int32.Parse(split[1]);
                TwitchPlaySettings.SetRewardBonus(moduleCountBonus);
            }
        }
        else if (text.Equals("timemode", StringComparison.InvariantCultureIgnoreCase))
        {
            if (UserAccess.HasAccess(userNickName, AccessLevel.Mod, true))
            {
                OtherModes.toggleTimedMode();
                _ircConnection.SendMessage(OtherModes.timedModeCheck() ? "Time Mode Enabled" : "Time Mode Disabled");
            }
        }
        else if (text.StartsWith("rank", StringComparison.InvariantCultureIgnoreCase))
        {
            Leaderboard.LeaderboardEntry entry = null;
            if (split.Length > 1)
            {
                int desiredRank;
                switch (split.Length)
                {
                    case 3 when split[1].Equals("solo", StringComparison.InvariantCultureIgnoreCase) && int.TryParse(split[2], out desiredRank):
                        leaderboard.GetSoloRank(desiredRank, out entry);
                        break;
                    case 2 when int.TryParse(split[1], out desiredRank):
                        leaderboard.GetRank(desiredRank, out entry);
                        break;
                    default:
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
                _ircConnection.SendMessage(TwitchPlaySettings.data.RankQuery, entry.UserName, entry.Rank, entry.SolveCount, entry.StrikeCount, txtSolver, txtSolo, entry.SolveScore);
            }
            else
            {
                _ircConnection.SendMessage(TwitchPlaySettings.data.DoYouEvenPlayBro, userNickName);
            }
            return;
        }
        else if (text.Equals("log", StringComparison.InvariantCultureIgnoreCase) || text.Equals("analysis", StringComparison.InvariantCultureIgnoreCase))
        {
            TwitchPlaysService.logUploader.PostToChat("Analysis for the previous bomb: {0}");
            return;
        }
        else if (text.Equals("shorturl", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!IsAuthorizedDefuser(userNickName)) return;
            _ircConnection.SendMessage((TwitchPlaysService.urlHelper.ToggleMode()) ? "Enabling shortened URLs" : "Disabling shortened URLs");
        }
        else if (text.Equals("about", StringComparison.InvariantCultureIgnoreCase))
        {
            _ircConnection.SendMessage("Twitch Plays: KTaNE is an alternative way of playing !ktane. Unlike the original game, you play as both defuser and expert, and defuse the bomb by sending special commands to the chat. Try !help for more information!");
            return;
        }
        else if (text.Equals("ktane", StringComparison.InvariantCultureIgnoreCase))
        {
            _ircConnection.SendMessage("Keep Talking and Nobody Explodes is developed by Steel Crate Games. It's available for Windows PC, Mac OS X, PlayStation VR, Samsung Gear VR and Google Daydream. See http://www.keeptalkinggame.com/ for more information!");
            return;
        }
        else if (text.StartsWith("add ", StringComparison.InvariantCultureIgnoreCase) || text.StartsWith("remove ", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!IsAuthorizedDefuser(userNickName)) return;
            if (split.Length < 3)
            {
                return;
            }

            bool stepdown = split[0].Equals("remove",StringComparison.InvariantCultureIgnoreCase) && split[1].Equals(userNickName, StringComparison.InvariantCultureIgnoreCase);
            if (!UserAccess.HasAccess(userNickName, AccessLevel.Mod, true) && !stepdown)
            {
                return;
            }

            AccessLevel level = AccessLevel.User;
            foreach(string lvl in split.Skip(2))
            {
                switch (lvl)
                {
                    case "mod":
                    case "moderator":
                        level |= (stepdown || UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true)) ? AccessLevel.Mod : AccessLevel.User;
                        break;
                    case "admin":
                    case "administrator":
                        level |= (stepdown || UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true)) ? AccessLevel.Admin : AccessLevel.User;
                        break;
                    case "superadmin":
                    case "superuser":
                    case "super-user":
                    case "super-admin":
                    case "super-mod":
                    case "supermod":
                        level |= (stepdown || UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true)) ? AccessLevel.SuperUser : AccessLevel.User;
                        break;

                
                    case "defuser":
                        level |= AccessLevel.Defuser;
                        break;
                    case "no-points":
                    case "no-score":
                    case "noscore":
                    case "nopoints":
                        level |= UserAccess.HasAccess(userNickName, AccessLevel.Mod, true) ? AccessLevel.NoPoints : AccessLevel.User;
                        break;
                }
            }
            if (level == AccessLevel.User)
            {
                return;
            }

            if (text.StartsWith("add ", StringComparison.InvariantCultureIgnoreCase))
            {
                UserAccess.AddUser(split[1], level);
                _ircConnection.SendMessage(TwitchPlaySettings.data.AddedUserPower, level, split[1]);
            }
            else
            {
                UserAccess.RemoveUser(split[1], level);
                _ircConnection.SendMessage(TwitchPlaySettings.data.RemoveUserPower, level, split[1]);
            }
            UserAccess.WriteAccessList();
        }
		
		switch (split[0])
		{
			case "run":
				if (!((TwitchPlaySettings.data.EnableRunCommand && TwitchPlaySettings.data.EnableTwitchPlaysMode) || UserAccess.HasAccess(userNickName, AccessLevel.Mod, true)))
				{
					_ircConnection.SendMessage(TwitchPlaySettings.data.RunCommandDisabled, userNickName);
					break;
				}

				if (split.Length == 2)
				{
					string missionID = null;
				    string failureMessage = null;
					if (UserAccess.HasAccess(userNickName, AccessLevel.Mod, true))
					{
						missionID = resolveMissionID(textAfter, out failureMessage);
					}

					if (missionID == null && TwitchPlaySettings.data.CustomMissions.ContainsKey(textAfter))
					{
						missionID = resolveMissionID(TwitchPlaySettings.data.CustomMissions[textAfter], out failureMessage);
					}

					if (missionID == null)
					{
						string distributionName = distributions.Keys.OrderByDescending(x => x.Length).FirstOrDefault(y => split[1].Contains(y));
					    if (distributionName == null || !int.TryParse(split[1].Replace(distributionName, ""), out int modules) ||
							modules < 1 || modules > GameInfo.GetMaximumBombModules())
						{
						    _ircConnection.SendMessage(failureMessage);
						}
						else
						{
							split[1] = split[1].Replace(distributionName, "");
							Array.Resize(ref split, 3);
							split[2] = distributionName;
						}
					}
					else
					{
						if (CurrentState == KMGameInfo.State.PostGame) StartCoroutine(ReturnToSetup(userNickName, "!" + text));
						if (CurrentState != KMGameInfo.State.Setup) break;

						GameCommands.StartMission(missionID, "-1");
					}
				}

				if (split.Length == 3)
				{
				    if (distributions.ContainsKey(split[1]))
					{
						string temp = split[1];
						split[1] = split[2];
						split[2] = temp;
					}

					if (int.TryParse(split[1], out int modules) && modules > 0)
					{
						int maxModules = GameInfo.GetMaximumBombModules();
						if (modules > maxModules)
						{
							_ircConnection.SendMessage("Sorry, the maximum number of modules is {0}.", maxModules);
							break;
						}

						if (!distributions.ContainsKey(split[2]))
						{
							_ircConnection.SendMessage("Sorry, there is no distribution called \"{0}\".", split[2]);
							break;
						}

						if (CurrentState == KMGameInfo.State.PostGame) StartCoroutine(ReturnToSetup(userNickName, "!" + text));
						if (CurrentState != KMGameInfo.State.Setup) break;

						Distribution distribution = distributions[split[2]];
						int vanillaModules = Mathf.FloorToInt(modules * distribution.vanilla);
						int moddedModules = Mathf.FloorToInt(modules * distribution.modded);

						KMMission mission = ScriptableObject.CreateInstance<KMMission>();
						List<KMComponentPool> pools = new List<KMComponentPool>();

						if (vanillaModules > 0)
						{
							pools.Add(new KMComponentPool()
							{
								SpecialComponentType = KMComponentPool.SpecialComponentTypeEnum.ALL_SOLVABLE,
								AllowedSources = KMComponentPool.ComponentSource.Base,
								Count = vanillaModules
							});
						}

						if (moddedModules > 0)
						{
							pools.Add(new KMComponentPool()
							{
								SpecialComponentType = KMComponentPool.SpecialComponentTypeEnum.ALL_SOLVABLE,
								AllowedSources = KMComponentPool.ComponentSource.Mods,
								Count = moddedModules
							});
						}

						int bothModules = modules - moddedModules - vanillaModules;
						if (bothModules > 0)
						{
							pools.Add(new KMComponentPool() {
								SpecialComponentType = KMComponentPool.SpecialComponentTypeEnum.ALL_SOLVABLE,
								AllowedSources = KMComponentPool.ComponentSource.Base | KMComponentPool.ComponentSource.Mods,
								Count = bothModules
							});
						}

						mission.PacingEventsEnabled = true;
						mission.DisplayName = modules + " " + distribution.displayName;
						if (OtherModes.timedModeOn)
						{
							mission.GeneratorSetting = new KMGeneratorSetting()
							{
								ComponentPools = pools,
								TimeLimit = 300,
								NumStrikes = 9
							};
						}
						else
						{
							mission.GeneratorSetting = new KMGeneratorSetting()
							{
								ComponentPools = pools,
								TimeLimit = (120 * modules) - (60 * vanillaModules),
								NumStrikes = Math.Max(3, modules / 12)
							};
						}
						
						int rewardPoints = Convert.ToInt32((5 * modules) - (3 * vanillaModules));
						TwitchPlaySettings.SetRewardBonus(rewardPoints);
						_ircConnection.SendMessage("Reward for completing bomb: " + rewardPoints);
						GameCommands.StartMission(mission, "-1");
					}
				}
				break;
			case "runraw":
				if (UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true))
					if (CurrentState == KMGameInfo.State.Setup) GameCommands.StartMission(textAfter, "-1");
					else if (CurrentState == KMGameInfo.State.PostGame) StartCoroutine(ReturnToSetup(userNickName, "!" + text));
				break;
			case "profile":
			case "profiles":
				List<string> profileList = TwitchPlaySettings.data.ProfileWhitelist;
				if (profileList.Count == 0)
				{
					_ircConnection.SendMessage(TwitchPlaySettings.data.ProfileCommandDisabled, userNickName);
					break;
				}

				switch (split[1])
				{
					case "enable":
					case "add":
					case "activate":
					case "disable":
					case "remove":
					case "deactivate":
						string profileString = ProfileHelper.GetProperProfileName(split.Skip(2).Join());
						if (profileList.Contains(profileString))
						{
							string filename = profileString.Replace(' ', '_');
							if (split[1].EqualsAny("enable", "add"))
							{
								if (ProfileHelper.Add(filename)) _ircConnection.SendMessage("Enabled profile: {0}.", profileString);
								else _ircConnection.SendMessage(TwitchPlaySettings.data.ProfileActionUseless, profileString, "enabled");
							}
							else
							{
								if (ProfileHelper.Remove(filename)) _ircConnection.SendMessage("Disabled profile: {0}.", profileString);
								else _ircConnection.SendMessage(TwitchPlaySettings.data.ProfileActionUseless, profileString, "disabled");
							}
						}
						else
						{
							_ircConnection.SendMessage(TwitchPlaySettings.data.ProfileNotWhitelisted, split.Skip(2).Join());
						}
						break;
					case "enabled":
					case "enabledlist":
						_ircConnection.SendMessage(TwitchPlaySettings.data.ProfileListEnabled, ProfileHelper.Profiles.Select(str => str.Replace('_', ' ')).Intersect(profileList).DefaultIfEmpty("None").Join(", "));
						break;
					case "list":
					case "all":
						_ircConnection.SendMessage(TwitchPlaySettings.data.ProfileListAll, profileList.Join(", "));
						break;
				}
				break;
		}
		
        if (UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true))
        {
            if (text.Equals("reloaddata", StringComparison.InvariantCultureIgnoreCase))
            {
                bool streamer = UserAccess.HasAccess(userNickName, AccessLevel.Streamer);
                bool superuser = UserAccess.HasAccess(userNickName, AccessLevel.SuperUser);

                ModuleData.LoadDataFromFile();
                TwitchPlaySettings.LoadDataFromFile();
                UserAccess.LoadAccessList();

                if (streamer)
                    UserAccess.AddUser(userNickName, AccessLevel.Streamer);
                if (superuser)
                    UserAccess.AddUser(userNickName, AccessLevel.SuperUser);
                _ircConnection.SendMessage("Data reloaded");
            }
            else if (text.Equals("enabletwitchplays", StringComparison.InvariantCultureIgnoreCase))
            {
                _ircConnection.SendMessage("Twitch Plays Enabled");
                TwitchPlaySettings.data.EnableTwitchPlaysMode = true;
                TwitchPlaySettings.WriteDataToFile();
                EnableDisableInput();
            }
            else if (text.Equals("disabletwitchplays", StringComparison.InvariantCultureIgnoreCase))
            {
                _ircConnection.SendMessage("Twitch Plays Disabled");
                TwitchPlaySettings.data.EnableTwitchPlaysMode = false;
                TwitchPlaySettings.WriteDataToFile();
                EnableDisableInput();
            }
            else if (text.Equals("enableinteractivemode", StringComparison.InvariantCultureIgnoreCase))
            {
                _ircConnection.SendMessage("Interactive Mode Enabled");
                TwitchPlaySettings.data.EnableInteractiveMode = true;
                TwitchPlaySettings.WriteDataToFile();
                EnableDisableInput();
            }
            else if (text.Equals("disableinteractivemode", StringComparison.InvariantCultureIgnoreCase))
            {
                _ircConnection.SendMessage("Interactive Mode Disabled");
                TwitchPlaySettings.data.EnableInteractiveMode = false;
                TwitchPlaySettings.WriteDataToFile();
                EnableDisableInput();
            }
            else if (text.Equals("solveunsupportedmodules", StringComparison.InvariantCultureIgnoreCase))
            {
                _ircConnection.SendMessage("Solving unsupported modules.");
                TwitchComponentHandle.SolveUnsupportedModules();
            }
            else if (text.Equals("removesolvebasedmodules", StringComparison.InvariantCultureIgnoreCase))
            {
                _ircConnection.SendMessage("Removing Solve based modules");
                TwitchComponentHandle.RemoveSolveBasedModules();
            }
            else if (text.Equals("!silencemode", StringComparison.InvariantCultureIgnoreCase))
            {
                _ircConnection.ToggleSilenceMode();
            }

        }
    }

	private IEnumerator ReturnToSetup(string userNickName, string text)
	{
		_ircConnection.OnMessageReceived.Invoke(userNickName, null, "!back");
		yield return new WaitUntil(() => CurrentState == KMGameInfo.State.Setup);
		_ircConnection.OnMessageReceived.Invoke(userNickName, null, text);
	}

	private void EnableDisableInput()
    {
        if (BombMessageResponder.EnableDisableInput() && TwitchComponentHandle.SolveUnsupportedModules())
        {
            _ircConnection.SendMessage("Some modules were automatically solved to prevent problems with defusing this bomb.");
        }
    }

}
