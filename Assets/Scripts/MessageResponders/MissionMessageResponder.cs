using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

public class MissionMessageResponder : MessageResponder
{
    private BombBinderCommander _bombBinderCommander = null;
    private FreeplayCommander _freeplayCommander = null;

    #region Unity Lifecycle
    private void OnEnable()
    {
        // InputInterceptor.DisableInput();

        StartCoroutine(CheckForBombBinderAndFreeplayDevice());
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        _bombBinderCommander = null;
        _freeplayCommander = null;
    }
    #endregion

    #region Protected/Private Methods
    private IEnumerator CheckForBombBinderAndFreeplayDevice()
    {
        yield return null;

        while (true)
        {
            UnityEngine.Object[] bombBinders = FindObjectsOfType(CommonReflectedTypeInfo.BombBinderType);
            if (bombBinders != null && bombBinders.Length > 0)
            {
                _bombBinderCommander = new BombBinderCommander((MonoBehaviour)bombBinders[0]);
                break;
            }

            yield return null;
        }

        while (true)
        {
            UnityEngine.Object[] freeplayDevices = FindObjectsOfType(CommonReflectedTypeInfo.FreeplayDeviceType);
            if (freeplayDevices != null && freeplayDevices.Length > 0)
            {
                _freeplayCommander = new FreeplayCommander((MonoBehaviour)freeplayDevices[0]);
                break;
            }

            yield return null;
        }
    }

	string resolveMissionID(string targetID)
	{
		object modManager = CommonReflectedTypeInfo.ModManagerInstanceField.GetValue(null);
		IEnumerable<ScriptableObject> missions = ((IEnumerable) CommonReflectedTypeInfo.ModMissionsField.GetValue(modManager, null)).Cast<ScriptableObject>();
		ScriptableObject mission = missions.FirstOrDefault(obj => Regex.IsMatch(obj.name, "mod_.+_" + Regex.Escape(targetID), RegexOptions.CultureInvariant | RegexOptions.IgnoreCase));
		if (mission == null) return null; else return mission.name;
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
		if (_bombBinderCommander == null)
		{
			return;
		}

		if (!text.StartsWith("!")) return;
		text = text.Substring(1);

		string[] split = text.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		string textAfter = split.Skip(1).Join();
		switch (split[0])
		{
			case "binder":
				if ((TwitchPlaySettings.data.EnableMissionBinder && TwitchPlaySettings.data.EnableTwitchPlaysMode) || UserAccess.HasAccess(userNickName, AccessLevel.Admin, true))
				{
					_coroutineQueue.AddToQueue(_bombBinderCommander.RespondToCommand(userNickName, textAfter, null, _ircConnection));
				}
				else
				{
					_ircConnection.SendMessage(TwitchPlaySettings.data.MissionBinderDisabled, userNickName);
				}
				break;
			case "freeplay":
				if((TwitchPlaySettings.data.EnableFreeplayBriefcase && TwitchPlaySettings.data.EnableTwitchPlaysMode) || UserAccess.HasAccess(userNickName, AccessLevel.Admin, true))
				{
					_coroutineQueue.AddToQueue(_freeplayCommander.RespondToCommand(userNickName, textAfter, null, _ircConnection));
				}
				else
				{
					_ircConnection.SendMessage(TwitchPlaySettings.data.FreePlayDisabled, userNickName);
				}
				break;
			case "run":
				if (!((TwitchPlaySettings.data.EnableRunCommand && TwitchPlaySettings.data.EnableTwitchPlaysMode) || UserAccess.HasAccess(userNickName, AccessLevel.Mod, true)))
				{
					_ircConnection.SendMessage(TwitchPlaySettings.data.RunCommandDisabled, userNickName);
					break;
				}

				if (split.Length == 2)
				{
					string missionID = null;
					if (UserAccess.HasAccess(userNickName, AccessLevel.Mod, true))
					{
						missionID = resolveMissionID(textAfter);
					}

					if (missionID == null && TwitchPlaySettings.data.CustomMissions.ContainsKey(textAfter))
					{
						missionID = resolveMissionID(TwitchPlaySettings.data.CustomMissions[textAfter]);
					}

					if (missionID == null)
					{
					    string distributionName = distributions.Keys.OrderByDescending(x => x.Length).FirstOrDefault(y => split[1].Contains(y));
					    int modules;
					    if (distributionName == null || !int.TryParse(split[1].Replace(distributionName, ""), out modules) || 
                            modules < 1 || modules > GetComponent<KMGameInfo>().GetMaximumBombModules())
					    {
					        _ircConnection.SendMessage("Unable to find a mission with an ID of \"{0}\".", textAfter);
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
						GetComponent<KMGameCommands>().StartMission(missionID, "-1");
					}
				}

				if (split.Length == 3)
				{
					int modules;

				    if (distributions.ContainsKey(split[1]))
				    {
				        string temp = split[1];
				        split[1] = split[2];
				        split[2] = temp;
				    }

					if (int.TryParse(split[1], out modules) && modules > 0)
					{
						int maxModules = GetComponent<KMGameInfo>().GetMaximumBombModules();
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
						Distribution distribution = distributions[split[2]];

						int vanillaModules = Mathf.FloorToInt(modules * distribution.vanilla);
						int moddedModules = Mathf.FloorToInt(modules * distribution.modded);

						KMMission mission = ScriptableObject.CreateInstance<KMMission>();
						List<KMComponentPool> pools = new List<KMComponentPool>();

						if (vanillaModules > 0)
						{
							KMComponentPool vanillaPool = new KMComponentPool();
							vanillaPool.SpecialComponentType = KMComponentPool.SpecialComponentTypeEnum.ALL_SOLVABLE;
							vanillaPool.AllowedSources = KMComponentPool.ComponentSource.Base;
							vanillaPool.Count = vanillaModules;
							pools.Add(vanillaPool);
						}

						if (moddedModules > 0)
						{
							KMComponentPool moddedPool = new KMComponentPool();
							moddedPool.SpecialComponentType = KMComponentPool.SpecialComponentTypeEnum.ALL_SOLVABLE;
							moddedPool.AllowedSources = KMComponentPool.ComponentSource.Mods;
							moddedPool.Count = moddedModules;
							pools.Add(moddedPool);
						}

						int bothModules = modules - moddedModules - vanillaModules;
						if (bothModules > 0)
						{
							KMComponentPool bothPool = new KMComponentPool();
							bothPool.SpecialComponentType = KMComponentPool.SpecialComponentTypeEnum.ALL_SOLVABLE;
							bothPool.AllowedSources = KMComponentPool.ComponentSource.Base | KMComponentPool.ComponentSource.Mods;
							bothPool.Count = bothModules;
							pools.Add(bothPool);
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
                                TimeLimit = (120*modules)-(60*vanillaModules),
                                NumStrikes = Math.Max(3, modules / 12)
                            };
                        }
                        int rewardPoints = Convert.ToInt32((5*modules)-(3*vanillaModules));
                        TwitchPlaySettings.SetRewardBonus(rewardPoints);
                        _ircConnection.SendMessage("Reward for completing bomb: " + rewardPoints);
                        GetComponent<KMGameCommands>().StartMission(mission, "-1");
					}
				}
				break;
			case "runraw":
				if (UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true))
					GetComponent<KMGameCommands>().StartMission(textAfter, "-1");
				break;
			case "profile":
			case "profiles":
				var profileList = TwitchPlaySettings.data.ProfileWhitelist;
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
	}
    #endregion
}
