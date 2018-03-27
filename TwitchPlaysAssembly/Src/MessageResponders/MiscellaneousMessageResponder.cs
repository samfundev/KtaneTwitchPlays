using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Assets.Scripts.Missions;
using Newtonsoft.Json;
using TwitchPlaysAssembly.Helpers;
using UnityEngine;

[RequireComponent(typeof(KMGameCommands))]
[RequireComponent(typeof(KMGameInfo))]
public class MiscellaneousMessageResponder : MessageResponder
{
	[HideInInspector]
	public int moduleCountBonus = 0;

	[HideInInspector]
	public BombComponent bombComponent = null;

	private KMGameCommands GameCommands;
	private KMGameInfo GameInfo;
	private KMGameInfo.State CurrentState = KMGameInfo.State.Transitioning;
	private static List<KMHoldableCommander> HoldableCommanders = new List<KMHoldableCommander>();
	private bool RankCommand = true;

	private void Start()
	{
		GameCommands = GetComponent<KMGameCommands>();
		GameInfo = GetComponent<KMGameInfo>();
		GameInfo.OnStateChange += delegate (KMGameInfo.State state)
		{
			CurrentState = state;
		};
	}

	public static IEnumerator FindHoldables()
	{
		string[] blacklistedHoldables =
		{
			"FreeplayDevice", "BombBinder", "BasicRectangleBomb"
		};
		HoldableCommanders.Clear();
		yield return new WaitForSeconds(0.1f);
		FloatingHoldable[] holdables = FindObjectsOfType<FloatingHoldable>();
		foreach (FloatingHoldable holdable in holdables)
		{
			//Bombs are blacklisted, as they are already handled by BombCommander.
			if (holdable.GetComponentInChildren<KMBomb>() != null) continue;
			if (blacklistedHoldables.Contains(holdable.name.Replace("(Clone)", ""))) continue;
			try
			{
				DebugHelper.Log($"Creating holdable handler for {holdable.name}");
				KMHoldableCommander holdableCommander = new KMHoldableCommander(holdable);
				HoldableCommanders.Add(holdableCommander);
			}
			catch (Exception ex)
			{
				DebugHelper.LogException(ex, $"Could not create a handler for holdable {holdable.name} due to an exception:");
			}
		}
	}

	public static IEnumerator DropAllHoldables()
	{
		IEnumerator drop;
		foreach (KMHoldableCommander commander in HoldableCommanders)
		{
			drop = commander.RespondToCommand("The Bomb", "drop");
			while (drop != null && drop.MoveNext())
				yield return drop.Current;
		}
		drop = FreeplayCommander.Instance?.FreeplayRespondToCommand("The Bomb", "drop", null);
		while (drop != null && drop.MoveNext())
			yield return drop.Current;
		drop = BombBinderCommander.Instance?.RespondToCommand("The Bomb", "drop", null);
		while (drop != null && drop.MoveNext())
			yield return drop.Current;
	}

	int GetMaximumModules(int maxAllowed=int.MaxValue)
	{
		return Math.Min(TPElevatorSwitch.IsON ? 54 : GameInfo.GetMaximumBombModules(),maxAllowed);
	}

	string ResolveMissionID(string targetID, out string failureMessage)
	{
		failureMessage = null;
		ModManager modManager = ModManager.Instance;
		List<Mission> missions = modManager.ModMissions;

		Mission mission = missions.FirstOrDefault(x => Regex.IsMatch(x.name, "mod_.+_" + Regex.Escape(targetID), RegexOptions.CultureInvariant | RegexOptions.IgnoreCase));
		if (mission == null)
		{
			failureMessage = $"Unable to find a mission with an ID of \"{targetID}\".";
			return null;
		}

		List<string> availableMods = GameInfo.GetAvailableModuleInfo().Where(x => x.IsMod).Select(y => y.ModuleId).ToList();
		if (MultipleBombs.Installed())
			availableMods.Add("Multiple Bombs");
		HashSet<string> missingMods = new HashSet<string>();
		List<ModuleInformation> modules = ComponentSolverFactory.GetModuleInformation().ToList();

		GeneratorSetting generatorSetting = mission.GeneratorSetting;
		List<ComponentPool> componentPools = generatorSetting.ComponentPools;
		int moduleCount = 0;
		foreach (ComponentPool componentPool in componentPools)
		{
			moduleCount += componentPool.Count;
			List<string> modTypes = componentPool.ModTypes;
			if (modTypes == null || modTypes.Count == 0) continue;
			foreach (string mod in modTypes.Where(x => !availableMods.Contains(x)))
			{
				missingMods.Add(modules.FirstOrDefault(x => x.moduleID == mod)?.moduleDisplayName ?? mod);
			}
		}
		if (missingMods.Count > 0)
		{
			failureMessage = $"Mission \"{targetID}\" was found, however, the following mods are not installed / loaded: {string.Join(", ", missingMods.OrderBy(x => x).ToArray())}";
			return null;
		}
		if (moduleCount > GetMaximumModules())
		{
			failureMessage = TPElevatorSwitch.IsON 
				? $"Mission \"{targetID}\" was found, however, this mission has too many modules to use in the elevator."
				: $"Mission \"{targetID}\" was found, however, a bomb case with at least {moduleCount} is not installed / enabled";
			return null;
		}
		
		return mission.name;
	}

	protected override void OnMessageReceived(string userNickName, string userColorCode, string text)
	{
		Match match;

		if (!text.StartsWith("!") || text.Equals("!")) return;
		text = text.Substring(1);

		string[] split = text.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		string textAfter = split.Skip(1).Join();
		if (text.Equals("cancel", StringComparison.InvariantCultureIgnoreCase))
		{
			if (!IsAuthorizedDefuser(userNickName)) return;
			CoroutineCanceller.SetCancel();
			return;
		}
		else if (text.Equals("stop", StringComparison.InvariantCultureIgnoreCase))
		{
			if (!IsAuthorizedDefuser(userNickName)) return;
			CoroutineCanceller.SetCancel();
			_coroutineQueue.CancelFutureSubcoroutines();
			BombMessageResponder.Instance?.SetCurrentBomb();
			return;
		}
		else if (text.Equals("manual", StringComparison.InvariantCultureIgnoreCase) ||
				 text.Equals("help", StringComparison.InvariantCultureIgnoreCase))
		{
			IRCConnection.Instance.SendMessage("!{0} manual [link to module {0}'s manual] | Go to {1} to get the vanilla manual for KTaNE", UnityEngine.Random.Range(1, 100), UrlHelper.Instance.VanillaManual);
			IRCConnection.Instance.SendMessage("!{0} help [commands for module {0}] | Go to {1} to get the command reference for TP:KTaNE (multiple pages, see the menu on the right)", UnityEngine.Random.Range(1, 100), UrlHelper.Instance.CommandReference);
			return;
		}
		else if (text.RegexMatch(@"^bonusscore (\S+) (-?[0-9]+)$"))
		{
			if (!IsAuthorizedDefuser(userNickName)) return;
			string playerrewarded = split[1];
			if (!int.TryParse(split[2], out int scorerewarded))
			{
				return;
			}
			if (UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true))
			{
				IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.GiveBonusPoints, split[1], split[2], userNickName);
				Color usedColor = new Color(.31f, .31f, .31f);
				Leaderboard.Instance.AddScore(playerrewarded, usedColor, scorerewarded);
			}
			return;
		}
		else if (text.RegexMatch(@"^bonussolves? (\S+) (-?[0-9]+)$"))
		{
			if (!IsAuthorizedDefuser(userNickName)) return;
			string playerrewarded = split[1];
			if (!int.TryParse(split[2], out int solverewarded))
			{
				return;
			}
			if (UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true))
			{
				IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.GiveBonusSolves, split[1], split[2], userNickName);
				Color usedColor = new Color(.31f, .31f, .31f);
				Leaderboard.Instance.AddSolve(playerrewarded, usedColor, solverewarded);
			}
			return;
		}
		else if (text.RegexMatch(@"^bonusstrikes? (\S+) (-?[0-9]+)$"))
		{
			if (!IsAuthorizedDefuser(userNickName)) return;
			string playerrewarded = split[1];
			if (!int.TryParse(split[2], out int strikerewarded))
			{
				return;
			}
			if (UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true))
			{
				IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.GiveBonusStrikes, split[1], split[2], userNickName);
				Color usedColor = new Color(.31f, .31f, .31f);
				Leaderboard.Instance.AddStrike(playerrewarded, usedColor, strikerewarded);
			}
			return;
		}
		else if (text.RegexMatch("^reward (-?[0-9]+)$"))
		{
			if (!IsAuthorizedDefuser(userNickName)) return;
			if (UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true) && int.TryParse(split[1], out moduleCountBonus))
			{
				TwitchPlaySettings.SetRewardBonus(moduleCountBonus);
			}
		}
		else if (text.RegexMatch(out match, $"^timemode ?((?:on|off)?)$"))
		{
			if (UserAccess.HasAccess(userNickName, AccessLevel.Mod, true) || TwitchPlaySettings.data.EnableTimeModeForEveryone)
			{
				switch (match.Groups[1].Value.ToLowerInvariant())
				{
					case "on":
						OtherModes.TimedModeOn = true;
						break;
					case "off":
						OtherModes.TimedModeOn = false;
						break;
					default:
						OtherModes.ToggleTimedMode();
						break;
				}
			}
			else
			{
				IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.TimeModeCommandDisabled, userNickName);
			}
		}
		else if (text.RegexMatch(out match, $"^zenmode ?((?:on|off)?)$"))
		{
			if (UserAccess.HasAccess(userNickName, AccessLevel.Mod, true) || TwitchPlaySettings.data.EnableZenModeForEveryone)
			{
				switch (match.Groups[1].Value.ToLowerInvariant())
				{
					case "on":
						OtherModes.ZenModeOn = true;
						break;
					case "off":
						OtherModes.ZenModeOn = false;
						break;
					default:
						OtherModes.ToggleZenMode();
						break;
				}
			}
			else
			{
				IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.ZenModeCommandDisabled, userNickName);
			}
		}
		else if (text.Equals("modes", StringComparison.InvariantCultureIgnoreCase))
		{
			string zenmodetext = !OtherModes.GetZenModeCurrent() ? "Enabled" : "Disabled";
			string timemodetext = !OtherModes.GetTimeModeCurrent() ? "Enabled" : "Disabled";
			string vsmodetext = !OtherModes.GetVSModeCurrent() ? "Enabled" : "Disabled";
			IRCConnection.Instance.SendMessage("Zen Mode is currently {0}{1}", OtherModes.GetZenModeCurrent() ? "Enabled" : "Disabled", OtherModes.GetZenModeNext() ? ", it will be " + zenmodetext + " for the next bomb." : ".");
			IRCConnection.Instance.SendMessage("Time Mode is currently {0}{1}", OtherModes.GetTimeModeCurrent() ? "Enabled" : "Disabled", OtherModes.GetTimeModeNext() ? ", it will be " + timemodetext + " for the next bomb." : ".");
			IRCConnection.Instance.SendMessage("VS mode is currently {0}{1}", OtherModes.GetVSModeCurrent() ? "Enabled" : "Disabled", OtherModes.GetVSModeNext() ? ", it will be " + vsmodetext + " for the next bomb." : ".");
		}
		else if (text.Equals("togglerankcommand", StringComparison.InvariantCultureIgnoreCase))
		{
			if (UserAccess.HasAccess(userNickName, AccessLevel.Mod, true))
			{
				if (TwitchPlaySettings.data.EnableRankCommand)
				{
					RankCommand = !RankCommand;
				}
				else
				{
					IRCConnection.Instance.SendMessage("Sorry {0}, but the rank command has been globally disabled by the streamer", userNickName);
				}
			}
		}
		else if (text.Equals("enablerankcommand", StringComparison.InvariantCultureIgnoreCase))
		{
			if (UserAccess.HasAccess(userNickName, AccessLevel.Mod, true))
			{
				if (TwitchPlaySettings.data.EnableRankCommand)
				{
					RankCommand = true;
				}
				else
				{
					IRCConnection.Instance.SendMessage("Sorry {0}, but the rank command has been globally disabled by the streamer", userNickName);
				}
			}
		}
		else if (text.Equals("disablerankcommand", StringComparison.InvariantCultureIgnoreCase))
		{
			if (UserAccess.HasAccess(userNickName, AccessLevel.Mod, true))
			{
				if (TwitchPlaySettings.data.EnableRankCommand)
				{
					RankCommand = false;
				}
				else
				{
					IRCConnection.Instance.SendMessage("Sorry {0}, but the rank command has been globally disabled by the streamer", userNickName);
				}
			}
		}
		else if (text.StartsWith("rank", StringComparison.InvariantCultureIgnoreCase))
		{
			if (TwitchPlaySettings.data.EnableRankCommand && RankCommand)
			{
				Leaderboard.LeaderboardEntry entry = null;
				if (split.Length > 1)
				{
					int desiredRank;
					switch (split.Length)
					{
						case 3 when split[1].Equals("solo", StringComparison.InvariantCultureIgnoreCase) && int.TryParse(split[2], out desiredRank):
							Leaderboard.Instance.GetSoloRank(desiredRank, out entry);
							break;
						case 3 when split[1].Equals("solo", StringComparison.InvariantCultureIgnoreCase) && !int.TryParse(split[2], out _):
							Leaderboard.Instance.GetRank(split[2], out entry);
							if (entry != null) break;
							IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.DoYouEvenPlayBro, split[2]);
							return;
						case 2 when int.TryParse(split[1], out desiredRank):
							Leaderboard.Instance.GetRank(desiredRank, out entry);
							break;
						case 2 when !int.TryParse(split[1], out _):
							Leaderboard.Instance.GetRank(split[1], out entry);
							if (entry != null) break;
							IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.DoYouEvenPlayBro, split[1]);
							return;
						default:
							return;
					}
					if (entry == null)
					{
						IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.RankTooLow);
						return;
					}
				}
				if (entry == null)
				{
					Leaderboard.Instance.GetRank(userNickName, out entry);
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
					IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.RankQuery, entry.UserName, entry.Rank, entry.SolveCount, entry.StrikeCount, txtSolver, txtSolo, entry.SolveScore);
				}
				else
				{
					IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.DoYouEvenPlayBro, userNickName);
				}
				return;
			}
		}
		else if (text.Equals("log", StringComparison.InvariantCultureIgnoreCase) || text.Equals("analysis", StringComparison.InvariantCultureIgnoreCase))
		{
			LogUploader.Instance.PostToChat("Analysis for the previous bomb: {0}");
			return;
		}
		else if (text.Equals("shorturl", StringComparison.InvariantCultureIgnoreCase))
		{
			if (!IsAuthorizedDefuser(userNickName)) return;
			IRCConnection.Instance.SendMessage((UrlHelper.Instance.ToggleMode()) ? "Enabling shortened URLs" : "Disabling shortened URLs");
		}
		else if (text.Equals("about", StringComparison.InvariantCultureIgnoreCase))
		{
			IRCConnection.Instance.SendMessage("Twitch Plays: KTaNE is an alternative way of playing !ktane. Unlike the original game, you play as both defuser and expert, and defuse the bomb by sending special commands to the chat. Try !help for more information!");
			return;
		}
		else if (text.Equals("ktane", StringComparison.InvariantCultureIgnoreCase))
		{
			IRCConnection.Instance.SendMessage("Keep Talking and Nobody Explodes is developed by Steel Crate Games. It's available for Windows PC, Mac OS X, PlayStation VR, Samsung Gear VR and Google Daydream. See http://www.keeptalkinggame.com/ for more information!");
			return;
		}
		else if (text.RegexMatch(out match, @"^(?:read|write|change) ?settings? (\S+)$"))
		{
			if (!UserAccess.HasAccess(userNickName, AccessLevel.Mod, true)) return;
			IRCConnection.Instance.SendMessage("{0}", TwitchPlaySettings.GetSetting(match.Groups[1].Value));
		}
		else if (text.RegexMatch(out match, @"^(?:write|change) ?settings? (\S+) (.+)$"))
		{
			if (!UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true)) return;
			var result = TwitchPlaySettings.ChangeSetting(match.Groups[1].Value, match.Groups[2].Value);
			IRCConnection.Instance.SendMessage("{0}", result.Second);
			if (result.First) TwitchPlaySettings.WriteDataToFile();
		}
		else if (text.RegexMatch(out match, @"^read ?module ?(help(?: ?message)?|manual(?: ?code)?|score|statuslight|(?:camera ?|module ?)?pin ?allowed|strike(?: ?penalty)|colou?r) (.+)$"))
		{
			Match match1 = match;
			var modules = ComponentSolverFactory.GetModuleInformation().Where(x => x.moduleDisplayName.ToLowerInvariant().Contains(match1.Groups[2].Value.ToLowerInvariant())).ToList();
			switch (modules.Count)
			{
				case 0:
					modules = ComponentSolverFactory.GetModuleInformation().Where(x => x.moduleID.ToLowerInvariant().Contains(match1.Groups[2].Value.ToLowerInvariant())).ToList();
					if (modules.Count == 1) goto case 1;
					if (modules.Count > 1)
					{
						var onemoduleID = modules.Where(x => x.moduleID.Equals(match1.Groups[2].Value, StringComparison.InvariantCultureIgnoreCase)).ToList();
						if (onemoduleID.Count == 1)
						{
							modules = onemoduleID;
							goto case 1;
						}
						goto default;
					}

					IRCConnection.Instance.SendMessage($"Sorry, there were no modules with the name \"{match.Groups[2].Value}\"");
					break;
				case 1:
					var moduleName = $"(\"{modules[0].moduleID}\":\"{modules[0].moduleDisplayName}\")";
					switch (match.Groups[1].Value)
					{
						case "help":
						case "helpmessage":
						case "help message":
							IRCConnection.Instance.SendMessage($"Module \"{moduleName}\" help message: {modules[0].helpText}");
							break;
						case "manual":
						case "manualcode":
						case "manual code":
							IRCConnection.Instance.SendMessage($"Module \"{moduleName}\" manual code: {(string.IsNullOrEmpty(modules[0].manualCode) ? modules[0].moduleDisplayName : modules[0].manualCode)}");
							break;
						case "score":
							IRCConnection.Instance.SendMessage($"Module \"{moduleName}\" score: {modules[0].moduleScore}");
							break;
						case "statuslight":
							if(modules[0].statusLightDown && modules[0].statusLightLeft)
								IRCConnection.Instance.SendMessage($"Module \"{moduleName}\" status light position: Bottom Left");
							else if (modules[0].statusLightDown)
								IRCConnection.Instance.SendMessage($"Module \"{moduleName}\" status light position: Bottom Right");
							else if (modules[0].statusLightLeft)
								IRCConnection.Instance.SendMessage($"Module \"{moduleName}\" status light position: Top Left");
							else
								IRCConnection.Instance.SendMessage($"Module \"{moduleName}\" status light position: Top Right");
							break;
						case "module pin allowed":
						case "camera pin allowed":
						case "module pinallowed":
						case "camera pinallowed":
						case "modulepin allowed":
						case "camerapin allowed":
						case "modulepinallowed":
						case "camerapinallowed":
						case "pinallowed":
						case "pin allowed":
							IRCConnection.Instance.SendMessage($"Module \"{moduleName}\" Module pinning always allowed: {(modules[0].CameraPinningAlwaysAllowed ? "Yes" : "No")}");
							break;
						case "strike":
						case "strikepenalty":
						case "strike penalty":
							IRCConnection.Instance.SendMessage($"Module \"{moduleName}\" Strike Penalty: {modules[0].strikePenalty}");
							break;
						case "color":
						case "colour":
							var moduleColor = JsonConvert.SerializeObject(TwitchPlaySettings.data.UnclaimedColor, Formatting.None, new ColorConverter());
							if (modules[0].unclaimedColor != new Color())
								moduleColor = JsonConvert.SerializeObject(modules[0].unclaimedColor, Formatting.None, new ColorConverter());
							IRCConnection.Instance.SendMessage($"Module \"{moduleName}\" Unclaimed color: {moduleColor}");
							break;
					}
					
					break;
				default:
					var onemodule = modules.Where(x => x.moduleDisplayName.Equals(match1.Groups[2].Value)).ToList();
					if (onemodule.Count == 1)
					{
						modules = onemodule;
						goto case 1;
					}

					IRCConnection.Instance.SendMessage($"Sorry, there is more than one module matching your search term. They are: {modules.Select(x => $"(\"{x.moduleID}\":\"{x.moduleDisplayName}\")").Join(", ")}");
					break;
			}
		}
		else if (text.RegexMatch(out match, @"^(?:write|change) ?module ?(help(?: ?message)?|manual(?: ?code)?|score|statuslight|(?:camera ?|module ?)?pin ?allowed|strike(?: ?penalty)|colou?r) (.+);(.*)$"))
		{
			if (!UserAccess.HasAccess(userNickName, AccessLevel.Admin, true)) return;
			var search = match.Groups[2].Value.ToLowerInvariant().Trim();
			var changeTo = match.Groups[3].Value.Trim();
			var modules = ComponentSolverFactory.GetModuleInformation().Where(x => x.moduleDisplayName.ToLowerInvariant().Contains(search)).ToList();
			switch (modules.Count)
			{
				case 0:
					modules = ComponentSolverFactory.GetModuleInformation().Where(x => x.moduleID.ToLowerInvariant().Contains(search)).ToList();
					if (modules.Count == 1) goto case 1;
					if (modules.Count > 1)
					{
						var onemoduleID = modules.Where(x => x.moduleID.Equals(search, StringComparison.InvariantCultureIgnoreCase)).ToList();
						if (onemoduleID.Count == 1)
						{
							modules = onemoduleID;
							goto case 1;
						}
						goto default;
					}

					IRCConnection.Instance.SendMessage($"Sorry, there were no modules with the name \"{match.Groups[2].Value}\"");
					break;
				case 1:
					var moduleName = $"(\"{modules[0].moduleID}\":\"{modules[0].moduleDisplayName}\")";
					var module = modules[0];
					var defaultModule = ComponentSolverFactory.GetDefaultInformation(module.moduleID);
					switch (match.Groups[1].Value)
					{
						case "help":
						case "helpmessage":
						case "help message":
							if (string.IsNullOrEmpty(changeTo))
							{
								module.helpTextOverride = false;
								module.helpText = defaultModule.helpText;
							}
							else
							{
								module.helpText = changeTo;
								module.helpTextOverride = true;
							}
							IRCConnection.Instance.SendMessage($"Module \"{moduleName}\" help message changed to: {module.helpText}");
							break;
						case "manual":
						case "manualcode":
						case "manual code":
							if (string.IsNullOrEmpty(changeTo))
							{
								module.manualCodeOverride = false;
								module.manualCode = defaultModule.manualCode;
							}
							else
							{
								module.manualCode = changeTo;
								module.manualCodeOverride = true;
							}

							IRCConnection.Instance.SendMessage($"Module \"{moduleName}\" manual code changed to: {(string.IsNullOrEmpty(module.manualCode) ? module.moduleDisplayName : module.manualCode)}");
							break;
						case "score":
							module.moduleScore = !int.TryParse(changeTo, out int moduleScore) ? defaultModule.moduleScore : moduleScore;
							IRCConnection.Instance.SendMessage($"Module \"{moduleName}\" score changed to: {module.moduleScore}");
							break;
						case "statuslight":
							switch (changeTo.ToLowerInvariant())
							{
								case "bl":
								case "bottomleft":
								case "bottom left":
									module.statusLightOverride = true;
									module.statusLightDown = true;
									module.statusLightLeft = true;
									break;
								case "br":
								case "bottomright":
								case "bottom right":
									module.statusLightOverride = true;
									module.statusLightDown = true;
									module.statusLightLeft = false;
									break;
								case "tr":
								case "topright":
								case "top right":
									module.statusLightOverride = true;
									module.statusLightDown = false;
									module.statusLightLeft = false;
									break;
								case "tl":
								case "topleft":
								case "top left":
									module.statusLightOverride = true;
									module.statusLightDown = false;
									module.statusLightLeft = true;
									break;
								default:
									module.statusLightOverride = false;
									module.statusLightDown = defaultModule.statusLightDown;
									module.statusLightLeft = defaultModule.statusLightLeft;
									break;
							}
							if (module.statusLightDown && module.statusLightLeft)
								IRCConnection.Instance.SendMessage($"Module \"{moduleName}\" status light position changed to: Bottom Left");
							else if (module.statusLightDown)
								IRCConnection.Instance.SendMessage($"Module \"{moduleName}\" status light position changed to: Bottom Right");
							else if (module.statusLightLeft)
								IRCConnection.Instance.SendMessage($"Module \"{moduleName}\" status light position changed to: Top Left");
							else
								IRCConnection.Instance.SendMessage($"Module \"{moduleName}\" status light position changed to: Top Right");
							break;
						case "module pin allowed":
						case "camera pin allowed":
						case "module pinallowed":
						case "camera pinallowed":
						case "modulepin allowed":
						case "camerapin allowed":
						case "modulepinallowed":
						case "camerapinallowed":
						case "pinallowed":
						case "pin allowed":
							switch (changeTo.ToLowerInvariant())
							{
								case "true":
								case "yes":
									module.CameraPinningAlwaysAllowed = true;
									break;
								case "no":
								case "false":
									module.CameraPinningAlwaysAllowed = false;
									break;
								default:
									module.CameraPinningAlwaysAllowed = defaultModule.CameraPinningAlwaysAllowed;
									break;
							}
							IRCConnection.Instance.SendMessage($"Module \"{moduleName}\" Module pinning always allowed changed to: {(modules[0].CameraPinningAlwaysAllowed ? "Yes" : "No")}");
							break;
						case "strike":
						case "strikepenalty":
						case "strike penalty":
							module.strikePenalty = !int.TryParse(changeTo, out int strikePenalty) ? defaultModule.strikePenalty : -strikePenalty;
							IRCConnection.Instance.SendMessage($"Module \"{moduleName}\" Strike Penalty changed to: {modules[0].strikePenalty}");
							break;
						case "color":
						case "colour":
							string moduleColor;
							try
							{
								var newModuleColor = SettingsConverter.Deserialize<Color>(changeTo);
								moduleColor = newModuleColor == new Color() 
									? JsonConvert.SerializeObject(modules[0].unclaimedColor, Formatting.None, new ColorConverter()) 
									: changeTo;
								module.unclaimedColor = newModuleColor == new Color()
									? defaultModule.unclaimedColor
									: newModuleColor;
							}
							catch
							{
								moduleColor = JsonConvert.SerializeObject(TwitchPlaySettings.data.UnclaimedColor, Formatting.None, new ColorConverter());
								if (defaultModule.unclaimedColor != new Color())
									moduleColor = JsonConvert.SerializeObject(modules[0].unclaimedColor, Formatting.None, new ColorConverter());
								module.unclaimedColor = defaultModule.unclaimedColor;
							}
							
							IRCConnection.Instance.SendMessage($"Module \"{moduleName}\" Unclaimed color changed to: {moduleColor}");
							break;
					}
					ModuleData.DataHasChanged = true;
					ModuleData.WriteDataToFile();

					break;
				default:
					var onemodule = modules.Where(x => x.moduleDisplayName.Equals(search)).ToList();
					if (onemodule.Count == 1)
					{
						modules = onemodule;
						goto case 1;
					}

					IRCConnection.Instance.SendMessage($"Sorry, there is more than one module matching your search term. They are: {modules.Select(x => $"(\"{x.moduleID}\":\"{x.moduleDisplayName}\")").Join(", ")}");
					break;
			}
		}
		else if (text.RegexMatch(out match, @"^(?:erase|remove|reset) ?settings? (\S+)$"))
		{
			if (!UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true)) return;

			var result = TwitchPlaySettings.ResetSettingToDefault(match.Groups[1].Value);
			IRCConnection.Instance.SendMessage("{0}", result.Second);
			if (result.First) TwitchPlaySettings.WriteDataToFile();
		}
		else if (text.RegexMatch(out match, @"^timeout (\S+) (\d+) (.+)") && int.TryParse(match.Groups[2].Value, out int banTimeout))
		{
			UserAccess.TimeoutUser(match.Groups[1].Value, userNickName, match.Groups[3].Value, banTimeout);
		}
		else if (text.RegexMatch(out match, @"^timeout (\S+) (\d+)") && int.TryParse(match.Groups[2].Value, out banTimeout))
		{
			UserAccess.TimeoutUser(match.Groups[1].Value, userNickName, null, banTimeout);
		}
		else if (text.RegexMatch(out match, @"^ban (\S+) (.+)"))
		{
			UserAccess.BanUser(match.Groups[1].Value, userNickName, match.Groups[2].Value);
		}
		else if (text.RegexMatch(out match, @"^ban (\S+)"))
		{
			UserAccess.BanUser(match.Groups[1].Value, userNickName, null);
		}
		else if (text.RegexMatch(out match, @"^unban (\S+)$"))
		{
			UserAccess.UnbanUser(match.Groups[1].Value, userNickName);
		}
		else if (text.RegexMatch(@"^(isbanned|banstats|bandata) (\S+)"))
		{
			if (!IsAuthorizedDefuser(userNickName)) return;

			if (UserAccess.HasAccess(userNickName, AccessLevel.Mod, true))
			{
				bool found = false;
				string trimmed = text.Split(' ')[1];
				List<string> target = trimmed.Split(';').ToList();
				Dictionary<string, BanData> bandata = UserAccess.GetBans();
				foreach (string person in target)
				{
					if (bandata.Keys.Contains(person))
					{
						bandata.TryGetValue(person, out BanData value);
						if (double.IsPositiveInfinity(value.BanExpiry))
						{
							IRCConnection.Instance.SendMessage("User: {0}, Banned by: {1}{2} This ban is permanant.", person, value.BannedBy, string.IsNullOrEmpty(value.BannedReason) ? ", For the follow reason: " + value.BannedReason + "," : ".");
						}
						else
						{
							double durationleft = value.BanExpiry - DateTime.Now.TotalSeconds();
							IRCConnection.Instance.SendMessage("User: {0}, Banned by: {1}{2} Ban duration left: {3}.", person, value.BannedBy, string.IsNullOrEmpty(value.BannedReason) ? ", For the follow reason: " + value.BannedReason + "," : ".", durationleft);
						}
						found = true;
					}
				}
				if (!found)
				{
					IRCConnection.Instance.SendMessage("The specified user has no ban data.");
				}
			}
		}
		else if (text.RegexMatch(@"^(?:add|remove) \S+ .+"))
		{
			if (!IsAuthorizedDefuser(userNickName)) return;

			bool stepdown = split[0].Equals("remove", StringComparison.InvariantCultureIgnoreCase) && split[1].Equals(userNickName, StringComparison.InvariantCultureIgnoreCase);
			if (!UserAccess.HasAccess(userNickName, AccessLevel.Mod, true) && !stepdown)
			{
				return;
			}

			AccessLevel level = AccessLevel.User;
			foreach (string lvl in split.Skip(2))
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
				IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.AddedUserPower, level, split[1]);
			}
			else
			{
				UserAccess.RemoveUser(split[1], level);
				IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.RemoveUserPower, level, split[1]);
			}
			UserAccess.WriteAccessList();
		}
		else if (text.Equals("tpmods", StringComparison.InvariantCultureIgnoreCase) || text.Equals("moderators", StringComparison.InvariantCultureIgnoreCase))
		{
			if (!TwitchPlaySettings.data.EnableModeratorsCommand)
			{
				IRCConnection.Instance.SendMessage("The moderators command has been disabled.");
				return;
			}
			Dictionary<string, AccessLevel> moderators = new Dictionary<string, AccessLevel>(UserAccess.GetUsers());
			string finalmessage = "Current moderators: ";

			foreach (string user in moderators.Keys)
			{
				AccessLevel userAccessLevel = UserAccess.HighestAccessLevel(user);
				string accessLevel = null;
				switch (userAccessLevel)
				{
					case AccessLevel.User:
					case AccessLevel.NoPoints:
					case AccessLevel.Banned:
					case AccessLevel.Defuser:
						break;
					case AccessLevel.Mod:
						accessLevel = "Moderator";
						break;
					case AccessLevel.Admin:
						accessLevel = "Admin";
						break;
					case AccessLevel.SuperUser:
						accessLevel = "Super User";
						break;
					case AccessLevel.Streamer:
						accessLevel = "Streamer";
						break;
				}
				if (accessLevel != null && user != "_usernickname1" && user != "_usernickname2" && user != "" && user != null) finalmessage += user + ", " + userAccessLevel + "; ";
			}
			IRCConnection.Instance.SendMessage(finalmessage);
		}
		else if (text.RegexMatch(@"^(getaccess|accessstats|accessdata) (\S+)"))
		{
			if (!IsAuthorizedDefuser(userNickName)) return;

			if (UserAccess.HasAccess(userNickName, AccessLevel.Mod, true))
			{
				string trimmed = text.Split(' ')[1];
				List<string> target = trimmed.Split(';').ToList();
				foreach (string person in target)
				{
					AccessLevel level = UserAccess.HighestAccessLevel(person);
					string stringLevel = UserAccess.LevelToString(level);
					IRCConnection.Instance.SendMessage("User {0}, Access Level: {1}", person, stringLevel);
				}
			}
		}
		switch (split[0])
		{
			case "run":
				if (!((TwitchPlaySettings.data.EnableRunCommand && TwitchPlaySettings.data.EnableTwitchPlaysMode) || UserAccess.HasAccess(userNickName, AccessLevel.Mod, true)))
				{
					IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.RunCommandDisabled, userNickName);
					break;
				}

				if (split.Length == 1)
				{
					string[] validDistributions = TwitchPlaySettings.data.ModDistributions.Where(x => x.Value.Enabled && !x.Value.Hidden).Select(x => x.Key).ToArray();
					IRCConnection.Instance.SendMessage(validDistributions.Any() 
						? $"Usage: !run <module_count> <distribution>. Valid distributions are {validDistributions.Join(", ")}" 
						: "Sorry, !run <module_count> <distribution> has been disabled.");
					break;
				}

				if (split.Length == 2 && split[1].Equals("zen", StringComparison.InvariantCultureIgnoreCase))
				{
					if (!TwitchPlaySettings.data.ModDistributions.TryGetValue("zen", out ModuleDistributions zenModeDistribution))
					{
						zenModeDistribution = new ModuleDistributions { Vanilla = 0.5f, Modded = 0.5f, DisplayName = "Zen Mode", MinModules = 1, MaxModules = GetMaximumModules(18), Hidden = true };
						TwitchPlaySettings.data.ModDistributions["zen"] = zenModeDistribution;
					}
					zenModeDistribution.MinModules = 1;
					zenModeDistribution.MaxModules = GetMaximumModules(18);
					zenModeDistribution.Hidden = true;
					
					Array.Resize(ref split, 3);
					split[2] = "zen";
				}

				if (split.Length == 2)
				{
					string missionID = null;
					string failureMessage = null;
					if (UserAccess.HasAccess(userNickName, AccessLevel.Mod, true))
					{
						missionID = ResolveMissionID(textAfter, out failureMessage);
					}

					if (missionID == null && TwitchPlaySettings.data.CustomMissions.ContainsKey(textAfter))
					{
						missionID = ResolveMissionID(TwitchPlaySettings.data.CustomMissions[textAfter], out failureMessage);
					}

					if (missionID == null)
					{
						string distributionName = TwitchPlaySettings.data.ModDistributions.Keys.FirstOrDefault(y => int.TryParse(split[1].Replace(y,""),out _));
						if (distributionName == null || !int.TryParse(split[1].Replace(distributionName, ""), out int modules) ||
							modules < TwitchPlaySettings.data.ModDistributions[distributionName].MinModules || 
							modules > GetMaximumModules(TwitchPlaySettings.data.ModDistributions[distributionName].MaxModules) ||
							!(TwitchPlaySettings.data.ModDistributions[distributionName].Enabled && !UserAccess.HasAccess(userNickName, AccessLevel.Mod, true)))
						{
							IRCConnection.Instance.SendMessage(failureMessage);
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
					if (TwitchPlaySettings.data.ModDistributions.ContainsKey(split[1]))
					{
						string temp = split[1];
						split[1] = split[2];
						split[2] = temp;
					}

					if (int.TryParse(split[1], out int modules) && modules > 0 || split[1].Equals("zen", StringComparison.InvariantCultureIgnoreCase))
					{
						bool zen = split[1].Equals("zen", StringComparison.InvariantCultureIgnoreCase);
						if (zen) modules = GetMaximumModules(18);

						if (!TwitchPlaySettings.data.ModDistributions.TryGetValue(split[2], out ModuleDistributions distribution))
						{
							IRCConnection.Instance.SendMessage("Sorry, there is no distribution called \"{0}\".", split[2]);
							break;
						}

						if (!distribution.Enabled && !UserAccess.HasAccess(userNickName, AccessLevel.Mod))
						{
							IRCConnection.Instance.SendMessage("Sorry, distribution \"{0}\" is disabled", distribution.DisplayName);
							break;
						}

						if (modules < distribution.MinModules && !zen)
						{
							IRCConnection.Instance.SendMessage("Sorry, the minimum number of modules for \"{0}\" is {1}.", distribution.DisplayName , distribution.MinModules);
							break;
						}
						
						int maxModules = GetMaximumModules(zen ? GetMaximumModules(18) : distribution.MaxModules);
						if (modules > maxModules)
						{
							if (modules > distribution.MaxModules)
								IRCConnection.Instance.SendMessage("Sorry, the maximum number of modules for {0} is {1}.", distribution.DisplayName, distribution.MaxModules);
							else
								IRCConnection.Instance.SendMessage("Sorry, the maximum number of modules is \"{0}\".", maxModules);
							break;
						}

						if (CurrentState == KMGameInfo.State.PostGame) StartCoroutine(ReturnToSetup(userNickName, "!" + text));
						if (CurrentState != KMGameInfo.State.Setup) break;
						
						int vanillaModules = Mathf.FloorToInt(modules * distribution.Vanilla);
						int moddedModules = Mathf.FloorToInt(modules * distribution.Modded);

						KMMission mission = ScriptableObject.CreateInstance<KMMission>();
						int bothModules = modules - moddedModules - vanillaModules;
						List<KMComponentPool> pools = new List<KMComponentPool>
						{
							new KMComponentPool()
							{
								SpecialComponentType = KMComponentPool.SpecialComponentTypeEnum.ALL_SOLVABLE,
								AllowedSources = KMComponentPool.ComponentSource.Base,
								Count = vanillaModules
							},
							new KMComponentPool()
							{
								SpecialComponentType = KMComponentPool.SpecialComponentTypeEnum.ALL_SOLVABLE,
								AllowedSources = KMComponentPool.ComponentSource.Mods,
								Count = moddedModules
							},
							new KMComponentPool()
							{
								SpecialComponentType = KMComponentPool.SpecialComponentTypeEnum.ALL_SOLVABLE,
								AllowedSources = KMComponentPool.ComponentSource.Base | KMComponentPool.ComponentSource.Mods,
								Count = bothModules
							}
						};
						if (zen && FactoryRoomAPI.Installed())
						{
							KMComponentPool factoryPool = new KMComponentPool
							{
								Count = 8,
								ModTypes = new List<string>(new[] {"Factory Mode"})
							};

							pools.Add(factoryPool);
						}

						mission.PacingEventsEnabled = true;
						mission.DisplayName = modules + " " + distribution.DisplayName;
						if (OtherModes.TimedModeOn)
						{
							mission.GeneratorSetting = new KMGeneratorSetting()
							{
								ComponentPools = pools,
								TimeLimit = TwitchPlaySettings.data.TimeModeStartingTime * 60,
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
						IRCConnection.Instance.SendMessage("Reward for completing bomb: " + rewardPoints);
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
					IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.ProfileCommandDisabled, userNickName);
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
								if (ProfileHelper.Add(filename)) IRCConnection.Instance.SendMessage("Enabled profile: {0}.", profileString);
								else IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.ProfileActionUseless, profileString, "enabled");
							}
							else
							{
								if (ProfileHelper.Remove(filename)) IRCConnection.Instance.SendMessage("Disabled profile: {0}.", profileString);
								else IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.ProfileActionUseless, profileString, "disabled");
							}
						}
						else
						{
							IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.ProfileNotWhitelisted, split.Skip(2).Join());
						}
						break;
					case "enabled":
					case "enabledlist":
						IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.ProfileListEnabled, ProfileHelper.Profiles.Select(str => str.Replace('_', ' ')).Intersect(profileList).DefaultIfEmpty("None").Join(", "));
						break;
					case "list":
					case "all":
						IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.ProfileListAll, profileList.Join(", "));
						break;
				}
				break;

			case "holdables":
				string[] holdables = HoldableCommanders.Where(x => !(x.Handler is UnsupportedHoldableHandler)).Select(x => $"!{x.ID}").ToArray();
				IRCConnection.Instance.SendMessage($"The following holdables are present: {string.Join(", ", holdables)}");
				break;
		}

		foreach (KMHoldableCommander commander in HoldableCommanders)
		{
			if (string.IsNullOrEmpty(commander?.ID) || !commander.ID.Equals(split[0])) continue;
			if (textAfter.EqualsAny("help", "manual"))
			{
				commander.Handler.ShowHelp();
				return;
			}
			if (!IsAuthorizedDefuser(userNickName)) return;
			BombMessageResponder.Instance?.DropCurrentBomb();
			_coroutineQueue.AddToQueue(commander.RespondToCommand(userNickName, textAfter));
		}

		if (TwitchPlaySettings.data.GeneralCustomMessages.ContainsKey(text.ToLowerInvariant()))
		{
			IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.GeneralCustomMessages[text.ToLowerInvariant()]);
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
				IRCConnectionManagerHoldable.TwitchPlaysDataRefreshed = true;
				IRCConnection.Instance.SendMessage("Data reloaded");
			}
			else if (text.Equals("enabletwitchplays", StringComparison.InvariantCultureIgnoreCase))
			{
				IRCConnection.Instance.SendMessage("Twitch Plays Enabled");
				TwitchPlaySettings.data.EnableTwitchPlaysMode = true;
				TwitchPlaySettings.WriteDataToFile();
				EnableDisableInput();
			}
			else if (text.Equals("disabletwitchplays", StringComparison.InvariantCultureIgnoreCase))
			{
				IRCConnection.Instance.SendMessage("Twitch Plays Disabled");
				TwitchPlaySettings.data.EnableTwitchPlaysMode = false;
				TwitchPlaySettings.WriteDataToFile();
				EnableDisableInput();
			}
			else if (text.Equals("enableinteractivemode", StringComparison.InvariantCultureIgnoreCase))
			{
				IRCConnection.Instance.SendMessage("Interactive Mode Enabled");
				TwitchPlaySettings.data.EnableInteractiveMode = true;
				TwitchPlaySettings.WriteDataToFile();
				EnableDisableInput();
			}
			else if (text.Equals("disableinteractivemode", StringComparison.InvariantCultureIgnoreCase))
			{
				IRCConnection.Instance.SendMessage("Interactive Mode Disabled");
				TwitchPlaySettings.data.EnableInteractiveMode = false;
				TwitchPlaySettings.WriteDataToFile();
				EnableDisableInput();
			}
			else if (text.Equals("solveunsupportedmodules", StringComparison.InvariantCultureIgnoreCase))
			{
				IRCConnection.Instance.SendMessage("Solving unsupported modules.");
				TwitchComponentHandle.SolveUnsupportedModules();
			}
			else if (text.Equals("removesolvebasedmodules", StringComparison.InvariantCultureIgnoreCase))
			{
				IRCConnection.Instance.SendMessage("Removing Solve based modules");
				TwitchComponentHandle.RemoveSolveBasedModules();
			}
			else if (text.Equals("silencemode", StringComparison.InvariantCultureIgnoreCase))
			{
				IRCConnection.Instance.ToggleSilenceMode();
			}

		}

		//As of now, Debugging commands are streamer only, apart from issue command as person, which is superuser and above.
		if (!TwitchPlaySettings.data.EnableDebuggingCommands || !UserAccess.HasAccess(userNickName, AccessLevel.SuperUser, true)) return;
		if (text.RegexMatch(out Match sayasMatch, @"^(?:issue|say|mimic) ?commands?(?: ?as)? (\S+) (.+)"))
		{
			//Do not allow issuing commands as someone with higher access levels than yourself.
			if (UserAccess.HighestAccessLevel(userNickName) < UserAccess.HighestAccessLevel(sayasMatch.Groups[2].Value)) return;
			IRCConnection.Instance.OnMessageReceived.Invoke(sayasMatch.Groups[1].Value, userColorCode, sayasMatch.Groups[2].Value);
		}
		if (!UserAccess.HasAccess(userNickName, AccessLevel.Streamer)) return;
		if (text.Equals("secondary camera"))
		{
			GameRoom.ToggleCamera(false);
		}
		if (text.Equals("main camera"))
		{
			GameRoom.ToggleCamera(true);
		}
		
		bool cameraChanged = false;
		bool cameraChangeAttempted = false;
		if (text.RegexMatch(out match, "move ?camera ?x (-?[0-9]+(?:\\.[0-9]+)*)"))
		{
			GameRoom.MoveCamera(new Vector3(float.Parse(match.Groups[1].Value), 0, 0));
			cameraChanged = !GameRoom.IsMainCamera;
			cameraChangeAttempted = true;
		}
		if (text.RegexMatch(out match, "move ?camera ?y (-?[0-9]+(?:\\.[0-9]+)*)"))
		{
			GameRoom.MoveCamera(new Vector3(0, float.Parse(match.Groups[1].Value), 0));
			cameraChanged = !GameRoom.IsMainCamera;
			cameraChangeAttempted = true;
		}
		if (text.RegexMatch(out match, "move ?camera ?z (-?[0-9]+(?:\\.[0-9]+)*)"))
		{
			GameRoom.MoveCamera(new Vector3(0, 0, float.Parse(match.Groups[1].Value)));
			cameraChanged = !GameRoom.IsMainCamera;
			cameraChangeAttempted = true;
		}

		if (text.RegexMatch(out match, "rotate ?camera ?x (-?[0-9]+(?:\\.[0-9]+)*)"))
		{
			GameRoom.RotateCamera(new Vector3(float.Parse(match.Groups[1].Value), 0, 0));
			cameraChanged = !GameRoom.IsMainCamera;
			cameraChangeAttempted = true;
		}
		if (text.RegexMatch(out match, "rotate ?camera ?y (-?[0-9]+(?:\\.[0-9]+)*)"))
		{
			GameRoom.RotateCamera(new Vector3(0, float.Parse(match.Groups[1].Value), 0));
			cameraChanged = !GameRoom.IsMainCamera;
			cameraChangeAttempted = true;
		}
		if (text.RegexMatch(out match, "rotate ?camera ?z (-?[0-9]+(?:\\.[0-9]+)*)"))
		{
			GameRoom.RotateCamera(new Vector3(0, 0, float.Parse(match.Groups[1].Value)));
			cameraChanged = !GameRoom.IsMainCamera;
			cameraChangeAttempted = true;
		}

		if (text.RegexMatch("reset ?camera"))
		{
			cameraChanged = true;
			GameRoom.ResetCamera();
		}

		if (cameraChangeAttempted && !cameraChanged)
		{
			IRCConnection.Instance.SendMessage("Please switch to the secondary camera using \"!secondary camera\" before attempting to move it.");
		}
		if (cameraChanged)
		{
			Transform camera = GameRoom.SecondaryCamera.transform;

			DebugHelper.Log($"Camera Position = {Math.Round(camera.localPosition.x, 3)},{Math.Round(camera.localPosition.y, 3)},{Math.Round(camera.localPosition.z, 3)}");
			DebugHelper.Log($"Camera Euler Angles = {Math.Round(camera.localEulerAngles.x, 3)},{Math.Round(camera.localEulerAngles.y, 3)},{Math.Round(camera.localEulerAngles.z, 3)}");
			IRCConnection.Instance.SendMessage($"Camera Position = {Math.Round(camera.localPosition.x, 3)},{Math.Round(camera.localPosition.y, 3)},{Math.Round(camera.localPosition.z, 3)}, Camera Euler Angles = {Math.Round(camera.localEulerAngles.x, 3)},{Math.Round(camera.localEulerAngles.y, 3)},{Math.Round(camera.localEulerAngles.z, 3)}");
		}
	}

	private IEnumerator ReturnToSetup(string userNickName, string text)
	{
		IRCConnection.Instance.OnMessageReceived.Invoke(userNickName, null, "!back");
		yield return new WaitUntil(() => CurrentState == KMGameInfo.State.Setup);
		IRCConnection.Instance.OnMessageReceived.Invoke(userNickName, null, text);
	}

	private void EnableDisableInput()
	{
		if (!BombMessageResponder.EnableDisableInput()) return;

		if(TwitchComponentHandle.SolveUnsupportedModules(true))
			IRCConnection.Instance.SendMessage("Some modules were automatically solved to prevent problems with defusing this bomb.");

		if (TwitchComponentHandle.UnsupportedModulesPresent())
			IRCConnection.Instance.SendMessage("There are some remaining modules that can still be solved using !<id> solve.");
	}

}

public class ModuleDistributions
{
	public string DisplayName;
	public float Vanilla;
	public float Modded;
	public int MinModules;
	public int MaxModules;
	public bool Enabled = true;
	public bool Hidden = false;
}