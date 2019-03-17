using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Assets.Scripts.Mods;
using Newtonsoft.Json;
using UnityEngine;

using Random = UnityEngine.Random;

static class GlobalCommands
{
	[Command(@"(manual|help)")]
	public static void Help(string user, bool isWhisper)
	{
		string[] alphabet = new string[26] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
		string[] randomCodes =
		{
			TwitchPlaySettings.data.EnableLetterCodes ? alphabet[Random.Range(0, alphabet.Length)] + alphabet[Random.Range(0, alphabet.Length)] : Random.Range(1, 100).ToString(),
			TwitchPlaySettings.data.EnableLetterCodes ? alphabet[Random.Range(0, alphabet.Length)] + alphabet[Random.Range(0, alphabet.Length)] : Random.Range(1, 100).ToString()
		};

		IRCConnection.SendMessage(string.Format("!{0} manual [link to module {0}'s manual] | Go to {1} to get manuals for KTaNE", randomCodes[0], TwitchPlaySettings.data.RepositoryUrl), user, !isWhisper);
		IRCConnection.SendMessage(string.Format("!{0} help [commands for module {0}] | Go to {1} to get the command reference for TP:KTaNE (multiple pages, see the menu on the right)", randomCodes[1], UrlHelper.Instance.CommandReference), user, !isWhisper);
	}

	[Command(@"bonus(?:score|points) (\S+) (-?[0-9]+)", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void BonusPoints([Group(1)] string targetPlayer, [Group(2)] int bonus, string user)
	{
		IRCConnection.SendMessageFormat(TwitchPlaySettings.data.GiveBonusPoints, targetPlayer, bonus, user);
		Leaderboard.Instance.AddScore(targetPlayer, new Color(.31f, .31f, .31f), bonus);
	}

	[Command(@"bonussolves? (\S+) (-?[0-9]+)", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void BonusSolves([Group(1)] string targetPlayer, [Group(2)] int bonus, string user)
	{
		IRCConnection.SendMessageFormat(TwitchPlaySettings.data.GiveBonusSolves, targetPlayer, bonus, user);
		Leaderboard.Instance.AddSolve(targetPlayer, new Color(.31f, .31f, .31f), bonus);
	}

	[Command(@"bonusstrikes? (\S+) (-?[0-9]+)", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void BonusStrikes([Group(1)] string targetPlayer, [Group(2)] int bonus, string user)
	{
		IRCConnection.SendMessageFormat(TwitchPlaySettings.data.GiveBonusStrikes, targetPlayer, bonus, user);
		Leaderboard.Instance.AddStrike(targetPlayer, new Color(.31f, .31f, .31f), bonus);
	}

	[Command(@"reward (-?[0-9]+)", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void SetReward([Group(1)] int reward) => TwitchPlaySettings.SetRewardBonus(reward);

	[Command(@"bonusreward (-?[0-9]+)", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void AddReward([Group(1)] int reward) => TwitchPlaySettings.AddRewardBonus(reward);

	[Command(@"timemode( *(on)| *off)?")]
	public static void TimeMode([Group(1)] bool any, [Group(2)] bool on, string user, bool isWhisper) => SetGameMode(TwitchPlaysMode.Time, !any, on, user, isWhisper, TwitchPlaySettings.data.EnableTimeModeForEveryone, TwitchPlaySettings.data.TimeModeCommandDisabled);
	[Command(@"zenmode( *(on)| *off)?")]
	public static void ZenMode([Group(1)] bool any, [Group(2)] bool on, string user, bool isWhisper) => SetGameMode(TwitchPlaysMode.Zen, !any, on, user, isWhisper, TwitchPlaySettings.data.EnableZenModeForEveryone, TwitchPlaySettings.data.ZenModeCommandDisabled);

	[Command(@"modes?")]
	public static void ShowMode(string user, bool isWhisper)
	{
		IRCConnection.SendMessage(string.Format("{0} mode is currently enabled. The next round is set to {1} mode.", OtherModes.GetName(OtherModes.currentMode), OtherModes.GetName(OtherModes.nextMode)), user, !isWhisper);
		if (TwitchPlaySettings.data.AnarchyMode)
			IRCConnection.SendMessage("We are currently in anarchy mode.", user, !isWhisper);
	}

	[Command(@"resetusers? +(.+)", AccessLevel.Admin, AccessLevel.Admin)]
	public static void ResetUser([Group(1)] string parameters, string user, bool isWhisper)
	{
		foreach (string userRaw in parameters.Split(';'))
		{
			string usertrimmed = userRaw.Trim();
			Leaderboard.Instance.GetRank(usertrimmed, out var entry);
			Leaderboard.Instance.GetSoloRank(usertrimmed, out var soloEntry);
			if (entry == null && soloEntry == null)
			{
				IRCConnection.SendMessage($"User {usertrimmed} was not found or has already been reset", user, !isWhisper);
				continue;
			}
			if (entry != null)
				Leaderboard.Instance.DeleteEntry(entry);
			if (soloEntry != null)
				Leaderboard.Instance.DeleteSoloEntry(soloEntry);
			IRCConnection.SendMessage($"User {usertrimmed} has been reset", userRaw, !isWhisper);
		}
	}

	[Command(@"rank")]
	public static void OwnRank(string user, bool isWhisper) { Leaderboard.Instance.GetRank(user, out var entry); ShowRank(entry, user, user, isWhisper); }

	[Command(@"rank solo (\d+)")]
	public static void SoloRank([Group(1)] int desiredRank, string user, bool isWhisper)
	{
		Leaderboard.Instance.GetSoloRank(desiredRank, out var entry);
		var entries = new List<Leaderboard.LeaderboardEntry>() { entry };
		if (Leaderboard.Instance.IsSoloDuplicate(entry, out var soloDuplicates))
			entries.AddRange(soloDuplicates);
		ShowRank(entries, user, user, isWhisper, numeric: true);
	}

	[Command(@"rank solo (?!\d+$)(.*)")]
	public static void SoloRankByUser([Group(1)] string desiredUser, string user, bool isWhisper) { Leaderboard.Instance.GetSoloRank(desiredUser, out var entry); ShowRank(entry, desiredUser, user, isWhisper); }

	[Command(@"rank (\d+)")]
	public static void Rank([Group(1)] int desiredRank, string user, bool isWhisper)
	{
		Leaderboard.Instance.GetRank(desiredRank, out var entry);
		var entries = new List<Leaderboard.LeaderboardEntry>() { entry };
		if (Leaderboard.Instance.IsDuplicate(entry, out var duplicates))
			entries.AddRange(duplicates);
		ShowRank(entries, user, user, isWhisper, numeric: true);
	}

	[Command(@"rank (?!\d+$)(.*)")]
	public static void RankByUser([Group(1)] string desiredUser, string user, bool isWhisper) { Leaderboard.Instance.GetRank(desiredUser, out var entry); ShowRank(entry, desiredUser, user, isWhisper); }

	[Command(@"(log|analysis)")]
	public static void Log() => LogUploader.Instance.PostToChat(LogUploader.Instance.previousUrl, "Analysis for the previous bomb: {0}");

	[Command("(log|analysis)now", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void LogNow(string user, bool isWhisper) => LogUploader.Instance.GetAnalyzerUrl(url => IRCConnection.SendMessage(url, user, !isWhisper));

	[Command(@"shorturl")]
	public static void ShortURL(string user, bool isWhisper) => IRCConnection.SendMessage(string.Format((UrlHelper.Instance.ToggleMode()) ? "Enabling shortened URLs" : "Disabling shortened URLs"), user, !isWhisper);

	[Command(@"(?:builddate|version)")]
	public static void BuildDate(string user, bool isWhisper)
	{
		string path = ModManager.Instance.GetEnabledModPaths(ModInfo.ModSourceEnum.Local)
						.FirstOrDefault(x => Directory.GetFiles(x, "TwitchPlaysAssembly.dll").Any()) ??
					ModManager.Instance.GetEnabledModPaths(ModInfo.ModSourceEnum.SteamWorkshop)
						.First(x => Directory.GetFiles(x, "TwitchPlaysAssembly.dll").Any());
		DateTime date = GetBuildDateTime(Path.Combine(path, "TwitchPlaysAssembly.dll"));
		IRCConnection.SendMessage($"Date and time this version of TP was built: {date:yyyy-MM-dd HH:mm:ss} UTC", user, !isWhisper);
	}

	[Command(@"(?:read|write|change|set) *settings? +(\S+)", AccessLevel.Mod, AccessLevel.Mod)]
	public static void ReadSetting([Group(1)] string settingName, string user, bool isWhisper) => IRCConnection.SendMessage(TwitchPlaySettings.GetSetting(settingName), user, !isWhisper);

	[Command(@"(?:write|change|set) *settings? +(\S+) +(.+)", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void WriteSetting([Group(1)] string settingName, [Group(2)] string newValue, string user, bool isWhisper)
	{
		var result = TwitchPlaySettings.ChangeSetting(settingName, newValue);
		IRCConnection.SendMessage(result.Second, user, !isWhisper);
		if (result.First)
			TwitchPlaySettings.WriteDataToFile();
	}

	[Command(@"read *module *(help(?: *message)?|manual(?: *code)?|score|points|statuslight|(?:camera *|module *)?pin *allowed|strike(?: *penalty)|colou?r|(?:valid *)?commands) +(.+)")]
	public static void ReadModuleInformation([Group(1)] string command, [Group(2)] string parameter, string user, bool isWhisper)
	{
		var modules = ComponentSolverFactory.GetModuleInformation().Where(x => x.moduleDisplayName.ContainsIgnoreCase(parameter)).ToList();
		switch (modules.Count)
		{
			case 0:
				modules = ComponentSolverFactory.GetModuleInformation().Where(x => x.moduleID.ContainsIgnoreCase(parameter)).ToList();
				if (modules.Count == 1) goto case 1;
				if (modules.Count > 1)
				{
					var onemoduleID = modules.Where(x => x.moduleID.EqualsIgnoreCase(parameter)).ToList();
					if (onemoduleID.Count == 1)
					{
						modules = onemoduleID;
						goto case 1;
					}
					goto default;
				}

				IRCConnection.SendMessage($@"Sorry, there were no modules with the name “{parameter}”.", user, !isWhisper);
				break;

			case 1:
				var moduleName = $"“{modules[0].moduleDisplayName}” ({modules[0].moduleID})";
				switch (command.ToLowerInvariant())
				{
					case "help":
					case "helpmessage":
					case "help message":
						IRCConnection.SendMessage($"Module {moduleName} help message: {modules[0].helpText}", user, !isWhisper);
						break;
					case "manual":
					case "manualcode":
					case "manual code":
						IRCConnection.SendMessage($"Module {moduleName} manual code: {(string.IsNullOrEmpty(modules[0].manualCode) ? modules[0].moduleDisplayName : modules[0].manualCode)}", user, !isWhisper);
						break;
					case "points":
					case "score":
						IRCConnection.SendMessage($"Module {moduleName} score: {modules[0].moduleScore}", user, !isWhisper);
						break;
					case "statuslight":
						if (modules[0].statusLightDown && modules[0].statusLightLeft)
							IRCConnection.SendMessage($"Module {moduleName} status light position: bottom left", user, !isWhisper);
						else if (modules[0].statusLightDown)
							IRCConnection.SendMessage($"Module {moduleName} status light position: bottom right", user, !isWhisper);
						else if (modules[0].statusLightLeft)
							IRCConnection.SendMessage($"Module {moduleName} status light position: top left", user, !isWhisper);
						else
							IRCConnection.SendMessage($"Module {moduleName} status light position: top right", user, !isWhisper);
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
						IRCConnection.SendMessage($"Module {moduleName} pinning always allowed: {(modules[0].CameraPinningAlwaysAllowed ? "Yes" : "No")}", user, !isWhisper);
						break;
					case "strike":
					case "strikepenalty":
					case "strike penalty":
						IRCConnection.SendMessage($"Module {moduleName} strike penalty: {modules[0].strikePenalty}", user, !isWhisper);
						break;
					case "color":
					case "colour":
						var moduleColor = JsonConvert.SerializeObject(TwitchPlaySettings.data.UnclaimedColor, Formatting.None, new ColorConverter());
						if (modules[0].unclaimedColor != new Color())
							moduleColor = JsonConvert.SerializeObject(modules[0].unclaimedColor, Formatting.None, new ColorConverter());
						IRCConnection.SendMessage($"Module {moduleName} unclaimed color: {moduleColor}", user, !isWhisper);
						break;
					case "commands":
					case "valid commands":
					case "validcommands":
						IRCConnection.SendMessage($"Module {moduleName} valid commands: {modules[0].validCommands}", user, !isWhisper);
						break;
				}
				break;

			default:
				var oneModule = modules.Where(x => x.moduleDisplayName.EqualsIgnoreCase(parameter)).ToList();
				if (oneModule.Count == 1)
				{
					modules = oneModule;
					goto case 1;
				}

				IRCConnection.SendMessage($"Sorry, there is more than one module matching your search term. They are: {modules.Select(x => $"“{x.moduleDisplayName}” ({x.moduleID})").Join(", ")}", user, !isWhisper);
				break;
		}
	}

	[Command(@"(?:write|change|set) *module *(help(?: *message)?|manual(?: *code)?|score|points|statuslight|(?:camera *|module *)?pin *allowed|strike(?: *penalty)|colou?r) +(.+);(.*)", AccessLevel.Admin, AccessLevel.Admin)]
	public static void WriteModuleInformation([Group(1)] string command, [Group(2)] string search, [Group(3)] string changeTo, string user, bool isWhisper)
	{
		var modules = ComponentSolverFactory.GetModuleInformation().Where(x => x.moduleDisplayName.ContainsIgnoreCase(search)).ToList();
		switch (modules.Count)
		{
			case 0:
				modules = ComponentSolverFactory.GetModuleInformation().Where(x => x.moduleID.ContainsIgnoreCase(search)).ToList();
				if (modules.Count == 1)
					goto case 1;
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

				IRCConnection.SendMessage($"Sorry, there were no modules with the name “{search}”.", user, !isWhisper);
				break;

			case 1:
				var module = modules[0];
				var moduleName = $"“{module.moduleDisplayName}” ({module.moduleID})";
				var defaultModule = ComponentSolverFactory.GetDefaultInformation(module.moduleID);
				switch (command.ToLowerInvariant())
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
						IRCConnection.SendMessage($"Module {moduleName} help message changed to: {module.helpText}", user, !isWhisper);
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

						IRCConnection.SendMessage($"Module {moduleName} manual code changed to: {(string.IsNullOrEmpty(module.manualCode) ? module.moduleDisplayName : module.manualCode)}", user, !isWhisper);
						break;
					case "points":
					case "score":
						module.moduleScore = !int.TryParse(changeTo, out int moduleScore) ? defaultModule.moduleScore : moduleScore;
						module.moduleScoreOverride = true;
						IRCConnection.SendMessage($"Module {moduleName} score changed to: {module.moduleScore}", user, !isWhisper);
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
							IRCConnection.SendMessage($"Module {moduleName} status light position changed to: Bottom Left", user, !isWhisper);
						else if (module.statusLightDown)
							IRCConnection.SendMessage($"Module {moduleName} status light position changed to: Bottom Right", user, !isWhisper);
						else if (module.statusLightLeft)
							IRCConnection.SendMessage($"Module {moduleName} status light position changed to: Top Left", user, !isWhisper);
						else
							IRCConnection.SendMessage($"Module {moduleName} status light position changed to: Top Right", user, !isWhisper);
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
						IRCConnection.SendMessage($"Module {moduleName} Module pinning always allowed changed to: {(modules[0].CameraPinningAlwaysAllowed ? "Yes" : "No")}", user, !isWhisper);
						break;
					case "strike":
					case "strikepenalty":
					case "strike penalty":
						module.strikePenalty = !int.TryParse(changeTo, out int strikePenalty) ? defaultModule.strikePenalty : -strikePenalty;
						IRCConnection.SendMessage($"Module {moduleName} Strike Penalty changed to: {modules[0].strikePenalty}", user, !isWhisper);
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

						IRCConnection.SendMessage($"Module {moduleName} Unclaimed color changed to: {moduleColor}", user, !isWhisper);
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

				IRCConnection.SendMessage($"Sorry, there is more than one module matching your search term. They are: {modules.Select(x => $"“{x.moduleDisplayName}” ({x.moduleID})").Join(", ")}", user, !isWhisper);
				break;
		}
	}

	[Command(@"(?:erase|remove|reset) ?settings? (\S+)", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void ResetSetting([Group(1)] string parameter, string user, bool isWhisper)
	{
		var result = TwitchPlaySettings.ResetSettingToDefault(parameter);
		IRCConnection.SendMessage($"{result.Second}", user, !isWhisper);
		if (result.First)
			TwitchPlaySettings.WriteDataToFile();
	}

	[Command(@"timeout +(\S+) +(\d+) +(.+)")]
	public static void BanUser([Group(1)] string userToBan, [Group(2)] int banTimeout, [Group(3)] string reason, string user, bool isWhisper) => UserAccess.TimeoutUser(userToBan, user, reason, banTimeout, isWhisper);
	[Command(@"timeout +(\S+) +(\d+)")]
	public static void BanUserForNoReason([Group(1)] string userToBan, [Group(2)] int banTimeout, string user, bool isWhisper) => UserAccess.TimeoutUser(userToBan, user, null, banTimeout, isWhisper);
	[Command(@"ban +(\S+) +(.+)")]
	public static void BanUser([Group(1)] string userToBan, [Group(2)] string reason, string user, bool isWhisper) => UserAccess.BanUser(userToBan, user, reason, isWhisper);
	[Command(@"ban +(\S+)")]
	public static void BanUserForNoReason([Group(1)] string userToBan, string user, bool isWhisper) => UserAccess.BanUser(userToBan, user, null, isWhisper);
	[Command(@"unban +(\S+)")]
	public static void UnbanUser([Group(1)] string userToUnban, string user, bool isWhisper) => UserAccess.UnbanUser(userToUnban, user, isWhisper);
	[Command(@"(isbanned|banstats|bandata) +(\S+)", AccessLevel.Mod, AccessLevel.Mod)]
	public static void IsBanned([Group(1)] string usersToCheck, string user, bool isWhisper)
	{
		bool found = false;
		var bandata = UserAccess.GetBans();
		foreach (string person in usersToCheck.Split(';'))
		{
			string adjperson = person.Trim();
			if (bandata.Keys.Contains(adjperson))
			{
				bandata.TryGetValue(adjperson, out var value);
				if (double.IsPositiveInfinity(value.BanExpiry))
					IRCConnection.SendMessage($"User: {adjperson}, banned by: {value.BannedBy}{(string.IsNullOrEmpty(value.BannedReason) ? $", for the follow reason: {value.BannedReason}." : ".")} This ban is permanent.", user, !isWhisper);
				else
					IRCConnection.SendMessage($"User: {adjperson}, banned by: {value.BannedBy}{(string.IsNullOrEmpty(value.BannedReason) ? $", for the follow reason: {value.BannedReason}." : ".")} Ban duration left: {value.BanExpiry - DateTime.Now.TotalSeconds()}.", user, !isWhisper);
				found = true;
			}
		}
		if (!found)
			IRCConnection.SendMessage("The specified user has no ban data.", user, !isWhisper);
	}

	[Command(@"(add|remove) +(\S+) +(.+)", AccessLevel.Mod, AccessLevel.Mod)]
	public static void AddRemoveRole([Group(1)] string command, [Group(2)] string targetUser, [Group(3)] string roles, string user, bool isWhisper)
	{
		bool stepdown = command.Equals("remove", StringComparison.InvariantCultureIgnoreCase) && targetUser.Equals(user, StringComparison.InvariantCultureIgnoreCase);
		if (!stepdown && !UserAccess.HasAccess(user, AccessLevel.Mod, true))
			return;

		var level = AccessLevel.User;
		foreach (string lvl in roles.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
		{
			switch (lvl)
			{
				case "mod":
				case "moderator":
					level |= (stepdown || UserAccess.HasAccess(user, AccessLevel.SuperUser, true)) ? AccessLevel.Mod : AccessLevel.User;
					break;
				case "admin":
				case "administrator":
					level |= (stepdown || UserAccess.HasAccess(user, AccessLevel.SuperUser, true)) ? AccessLevel.Admin : AccessLevel.User;
					break;
				case "superadmin":
				case "superuser":
				case "super-user":
				case "super-admin":
				case "super-mod":
				case "supermod":
					level |= (stepdown || UserAccess.HasAccess(user, AccessLevel.SuperUser, true)) ? AccessLevel.SuperUser : AccessLevel.User;
					break;

				case "defuser":
					level |= AccessLevel.Defuser;
					break;

				case "no-points":
				case "no-score":
				case "noscore":
				case "nopoints":
					level |= UserAccess.HasAccess(user, AccessLevel.Mod, true) ? AccessLevel.NoPoints : AccessLevel.User;
					break;
			}
		}

		if (level == AccessLevel.User)
			return;

		if (command.EqualsIgnoreCase("add"))
		{
			UserAccess.AddUser(targetUser, level);
			IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.AddedUserPower, level, targetUser), user, !isWhisper);
		}
		else
		{
			UserAccess.RemoveUser(targetUser, level);
			IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.RemoveUserPower, level, targetUser), user, !isWhisper);
		}
		UserAccess.WriteAccessList();
	}

	[Command(@"(tpmods|moderators)")]
	public static void Moderators(string user, bool isWhisper)
	{
		if (!TwitchPlaySettings.data.EnableModeratorsCommand)
		{
			IRCConnection.SendMessage("The moderators command has been disabled.", user, !isWhisper);
			return;
		}
		KeyValuePair<string, AccessLevel>[] moderators = UserAccess.GetUsers().Where(x => !string.IsNullOrEmpty(x.Key) && x.Key != "_usernickname1" && x.Key != "_usernickname2" && x.Key != (TwitchPlaySettings.data.TwitchPlaysDebugUsername.StartsWith("_") ? TwitchPlaySettings.data.TwitchPlaysDebugUsername.ToLowerInvariant() : "_" + TwitchPlaySettings.data.TwitchPlaysDebugUsername.ToLowerInvariant())).ToArray();
		string finalMessage = "Current moderators: ";

		string[] streamers = moderators.Where(x => UserAccess.HighestAccessLevel(x.Key) == AccessLevel.Streamer).OrderBy(x => x.Key).Select(x => x.Key).ToArray();
		string[] superusers = moderators.Where(x => UserAccess.HighestAccessLevel(x.Key) == AccessLevel.SuperUser).OrderBy(x => x.Key).Select(x => x.Key).ToArray();
		string[] administrators = moderators.Where(x => UserAccess.HighestAccessLevel(x.Key) == AccessLevel.Admin).OrderBy(x => x.Key).Select(x => x.Key).ToArray();
		string[] mods = moderators.Where(x => UserAccess.HighestAccessLevel(x.Key) == AccessLevel.Mod).OrderBy(x => x.Key).Select(x => x.Key).ToArray();

		if (streamers.Any())
			finalMessage += $"Streamers: {streamers.Join(", ")}{(superusers.Any() || administrators.Any() || mods.Any() ? " - " : "")}";
		if (superusers.Any())
			finalMessage += $"Super Users: {superusers.Join(", ")}{(administrators.Any() || mods.Any() ? " - " : "")}";
		if (administrators.Any())
			finalMessage += $"Administrators: {administrators.Join(", ")}{(mods.Any() ? " - " : "")}";
		if (mods.Any())
			finalMessage += $"Moderators: {mods.Join(", ")}";

		IRCConnection.SendMessage(finalMessage, user, !isWhisper);
	}

	[Command(@"(getaccess|accessstats|accessdata) +(.+)", AccessLevel.Mod, AccessLevel.Mod)]
	public static void GetAccess([Group(2)] string targetUsers, string user, bool isWhisper)
	{
		foreach (string person in targetUsers.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
			IRCConnection.SendMessage(string.Format("User {0} access level: {1}", person, UserAccess.LevelToString(UserAccess.HighestAccessLevel(person))), user, !isWhisper);
	}

	[Command(@"run")]
	public static void RunHelp()
	{
		string[] validDistributions = TwitchPlaySettings.data.ModDistributions.Where(x => x.Value.Enabled && !x.Value.Hidden).Select(x => x.Key).ToArray();
		IRCConnection.SendMessage(validDistributions.Any()
			? $"Usage: !run <module_count> <distribution>. Valid distributions are {validDistributions.Join(", ")}"
			: "Sorry, !run has been disabled.");
	}

	[Command(@"run *zen")]
	public static IEnumerator RunZen(string user, bool isWhisper, KMGameInfo inf) => RunWrapper(user, isWhisper, () =>
	{
		if (!TwitchPlaySettings.data.ModDistributions.TryGetValue("zen", out var zenModeDistribution))
		{
			zenModeDistribution = new ModuleDistributions { Vanilla = 0.5f, Modded = 0.5f, DisplayName = "Zen Mode", MinModules = 1, MaxModules = GetMaximumModules(inf, 18), Hidden = true };
			zenModeDistribution.MinModules = 1;
			zenModeDistribution.MaxModules = GetMaximumModules(inf, 18);
			zenModeDistribution.Hidden = true;
			TwitchPlaySettings.data.ModDistributions["zen"] = zenModeDistribution;
		}
		return RunDistribution(user, zenModeDistribution.MaxModules, inf, zenModeDistribution);
	});

	[Command(@"run +(.*) +(\d+)")]
	public static IEnumerator RunSpecific(string user, bool isWhisper, [Group(1)] string distributionName, [Group(2)] int modules, KMGameInfo inf) => RunSpecific(user, isWhisper, modules, distributionName, inf);
	[Command(@"run +(\d+) +(.*)")]
	public static IEnumerator RunSpecific(string user, bool isWhisper, [Group(1)] int modules, [Group(2)] string distributionName, KMGameInfo inf) => RunWrapper(user, isWhisper, () =>
	{
		if (!TwitchPlaySettings.data.ModDistributions.TryGetValue(distributionName, out var distribution))
		{
			IRCConnection.SendMessageFormat($"Sorry, there is no distribution called \"{0}\".", distributionName);
			return null;
		}
		return RunDistribution(user, modules, inf, distribution);
	});

	[Command(@"run +(?!.* +\d+$|\d+ +.*$)(.+)")]
	public static IEnumerator RunMission(string user, bool isWhisper, [Group(1)] string textAfter, KMGameInfo inf) => RunWrapper(user, isWhisper, () =>
	{
		string missionID = null;
		string failureMessage = null;
		if (UserAccess.HasAccess(user, AccessLevel.Mod, true))
			missionID = ResolveMissionID(inf, textAfter, out failureMessage);

		if (missionID == null && TwitchPlaySettings.data.CustomMissions.ContainsKey(textAfter))
			missionID = ResolveMissionID(inf, TwitchPlaySettings.data.CustomMissions[textAfter], out failureMessage);

		if (missionID == null)
		{
			IRCConnection.SendMessage(failureMessage);
			return null;
		}

		return RunMissionCoroutine(missionID);
	});

	[Command(@"runraw +(.+)", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static IEnumerator RunRaw([Group(1)] string missionName) => RunMissionCoroutine(missionName);

	[Command(@"runrawseed +(\d+) +(.+)", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static IEnumerator RunRawSeed([Group(1)] string seed, [Group(2)] string missionName) => RunMissionCoroutine(missionName, seed);

	[Command(@"profiles? help")]
	public static void ProfileHelp(string user, bool isWhisper) =>
		IRCConnection.SendMessage("Enable a profile using: !profile enable <name>. Disable a profile: !profile disable <name>. List the enabled profiles: !profile enabled. List all profiles: !profile list.", user, !isWhisper);

	[Command(@"profiles? +(?:enable|add|activate) +(.+)")]
	public static void ProfileEnable([Group(1)] string profileName, string user, bool isWhisper) => ProfileWrapper(profileName, user, isWhisper, (filename, profileString) =>
	{
		IRCConnection.SendMessage(ProfileHelper.Add(filename) ?
			$"Enabled profile: {profileString}." :
			string.Format(TwitchPlaySettings.data.ProfileActionUseless, profileString, "enabled"), user, !isWhisper);
	});

	[Command(@"profiles? +(?:disable|remove|deactivate) +(.+)")]
	public static void ProfileDisable([Group(1)] string profileName, string user, bool isWhisper) => ProfileWrapper(profileName, user, isWhisper, (filename, profileString) =>
	{
		IRCConnection.SendMessage(ProfileHelper.Remove(filename) ?
			$"Disabled profile: {profileString}." :
			string.Format(TwitchPlaySettings.data.ProfileActionUseless, profileString, "disabled"), user, !isWhisper);
	});

	[Command(@"profiles? +enabled(?:list)?")]
	public static void ProfilesListEnabled(string user, bool isWhisper) => IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.ProfileListEnabled, ProfileHelper.Profiles.Select(str => str.Replace('_', ' ')).Intersect(TwitchPlaySettings.data.ProfileWhitelist).DefaultIfEmpty("(none)").Join(", ")), user, !isWhisper);

	[Command(@"profiles? +(?:list|all)?")]
	public static void ProfilesListAll(string user, bool isWhisper) => IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.ProfileListAll, TwitchPlaySettings.data.ProfileWhitelist.Join(", ")), user, !isWhisper);

	[Command(@"holdables")]
	public static void Holdables(string user, bool isWhisper) => IRCConnection.SendMessage("The following holdables are present: {0}", user, !isWhisper, TwitchPlaysService.Instance.Holdables.Keys.Select(x => $"!{x}").Join(", "));

	[Command(@"disablemods", AccessLevel.Streamer, AccessLevel.Streamer)]
	public static void DisableModerators()
	{
		UserAccess.ModeratorsEnabled = false;
		IRCConnection.SendMessage("All moderators temporarily disabled.");
	}
	[Command(@"enablemods", AccessLevel.Streamer, AccessLevel.Streamer)]
	public static void EnableModerators()
	{
		UserAccess.ModeratorsEnabled = true;
		IRCConnection.SendMessage("All moderators restored.");
	}

	[Command("reloaddata", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void ReloadData(string user, bool isWhisper)
	{
		bool streamer = UserAccess.HasAccess(user, AccessLevel.Streamer);
		bool superuser = UserAccess.HasAccess(user, AccessLevel.SuperUser);

		ModuleData.LoadDataFromFile();
		TwitchPlaySettings.LoadDataFromFile();
		UserAccess.LoadAccessList();

		if (streamer)
			UserAccess.AddUser(user, AccessLevel.Streamer);
		if (superuser)
			UserAccess.AddUser(user, AccessLevel.SuperUser);

		IRCConnectionManagerHoldable.TwitchPlaysDataRefreshed = true;
		IRCConnection.SendMessage("Data reloaded", user, !isWhisper);
	}

	[Command(@"silencemode", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void SilenceMode() => IRCConnection.ToggleSilenceMode();

	[Command(@"elevator")]
	public static void Elevator() => TPElevatorSwitch.Instance?.ReportState();

	[Command(@"elevator (on|off|flip|toggle|switch|press|push)")]
	public static IEnumerator Elevator([Group(1)] string command)
	{
		if (TPElevatorSwitch.Instance == null || TPElevatorSwitch.Instance.ElevatorSwitch == null || !TPElevatorSwitch.Instance.ElevatorSwitch.gameObject.activeInHierarchy)
			return null;

		var on = TPElevatorSwitch.IsON;
		switch (command)
		{
			case "on" when !TPElevatorSwitch.IsON:
			case "off" when TPElevatorSwitch.IsON:
			case "flip":
			case "toggle":
			case "switch":
			case "press":
			case "push":
				on = !on;
				break;
			case "on":
			case "off":
				TPElevatorSwitch.Instance.ReportState();
				return null;
		}

		return TPElevatorSwitch.Instance.ToggleSetupRoomElevatorSwitch(on);
	}

	[Command("(?:restart|reboot)(?:game)?", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void RestartGame()
	{
		// WARNING: This code is a bit hacky, make sure to test this with and without the game running in Steam if you modify it.

		if (SteamManager.Initialized) // If the game was launched through Steam, we have to relaunch it through Steam.
		{
			Process.Start("steam://rungameid/341800");
			Process.GetCurrentProcess().Kill(); // HACK: Steam doesn't like two instances of the game running but using Kill() seems to be fast enough that Steam doesn't notice.
		}
		else
		{
			// The game can only normally have one instance open because the boot.config file has the single-instance argument in it.
			// To get around that we'll remove the argument from the file and then replace the original contents after the second instance launches.

			string bootConfigPath = Path.Combine(Application.dataPath, "boot.config");
			string originalContents = File.ReadAllText(bootConfigPath);
			File.WriteAllText(bootConfigPath, originalContents.Replace("single-instance=", ""));

			Process
				.Start(Process.GetCurrentProcess().MainModule.FileName)
				.WaitForInputIdle(); // Wait until the game is accepting input so we don't put back the original contents too early.

			File.WriteAllText(bootConfigPath, originalContents);

			Application.Quit();
		}
	}

	[Command("(?:quit|end)(?:game)?", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void QuitGame() => SceneManager.Instance.QuitGame();

	[Command(@"leaderboard reset", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void ResetLeaderboard(string user, bool isWhisper)
	{
		Leaderboard.Instance.ResetLeaderboard();
		IRCConnection.SendMessage("Leaderboard Reset.", user, !isWhisper);
	}

	[Command(@"(?:issue|say|mimic) ?commands?(?: ?as)? (\S+) (.+)", AccessLevel.SuperUser, AccessLevel.SuperUser)]
	public static void Mimic([Group(1)] string targetPlayer, [Group(2)] string newMessage, Message message)
	{
		if (message.IsWhisper)
		{
			IRCConnection.SendMessage($"Sorry {message.UserNickName}, issuing commands as other users is not allowed in whispers", message.UserNickName, false);
			return;
		}

		if (UserAccess.HighestAccessLevel(message.UserNickName) < UserAccess.HighestAccessLevel(targetPlayer))
		{
			IRCConnection.SendMessage($"Sorry {message.UserNickName}, you may not issue commands as {targetPlayer}");
			return;
		}

		IRCConnection.ReceiveMessage(targetPlayer, message.UserColorCode, newMessage);
	}

	//As of now, Debugging commands are streamer only, apart from issue command as person, and reset leaderboard, which are superuser and above.
	[Command("whispertest", AccessLevel.SuperUser, AccessLevel.SuperUser), DebuggingOnly]
	public static void WhisperTest(string user)
	{
		IRCConnection.SendMessage("Test successful", user, false);
	}

	[Command("secondary camera", AccessLevel.Streamer, AccessLevel.Streamer), DebuggingOnly]
	public static void EnableSecondaryCamera()
	{
		GameRoom.ToggleCamera(false);
	}

	[Command("main camera", AccessLevel.Streamer, AccessLevel.Streamer), DebuggingOnly]
	public static void EnableMainCamera()
	{
		GameRoom.ToggleCamera(true);
	}

	[Command(@"(move|rotate) ?camera ?([xyz]) (-?[0-9]+(?:\\.[0-9]+)*)", AccessLevel.Streamer, AccessLevel.Streamer), DebuggingOnly]
	public static void ChangeCamera([Group(1)] string action, [Group(2)] string axis, [Group(3)] float number, string user, bool isWhisper)
	{
		if (GameRoom.IsMainCamera)
		{
			IRCConnection.SendMessage("Please switch to the secondary camera using \"!secondary camera\" before attempting to move it.", user, !isWhisper);
			return;
		}

		Vector3 vector = new Vector3();
		switch (axis)
		{
			case "x": vector = new Vector3(number, 0, 0); break;
			case "y": vector = new Vector3(0, number, 0); break;
			case "z": vector = new Vector3(0, 0, number); break;
		}

		switch (action)
		{
			case "move": GameRoom.MoveCamera(vector); break;
			case "rotate": GameRoom.RotateCamera(vector); break;
		}

		CameraChanged(user, isWhisper);
	}

	[Command("reset ?camera", AccessLevel.Streamer, AccessLevel.Streamer), DebuggingOnly]
	public static void ResetCamera(string user, bool isWhisper)
	{
		GameRoom.ResetCamera();
		CameraChanged(user, isWhisper);
	}

	[Command(null)]
	public static bool DefaultCommand(string cmd, string user, bool isWhisper)
	{
		if (!TwitchPlaySettings.data.GeneralCustomMessages.ContainsKey(cmd.ToLowerInvariant()))
			return
				TwitchPlaySettings.data.IgnoreCommands
					.Contains(cmd.ToLowerInvariant()); //Ignore the command if it's in IgnoreCommands
		IRCConnection.SendMessage(TwitchPlaySettings.data.GeneralCustomMessages[cmd.ToLowerInvariant()], user, !isWhisper);
		return true;
	}

	#region Private methods
	private static void SetGameMode(TwitchPlaysMode mode, bool toggle, bool on, string user, bool isWhisper, bool enabledForEveryone, string disabledMessage)
	{
		if (!UserAccess.HasAccess(user, AccessLevel.Mod, true) && !enabledForEveryone && !TwitchPlaySettings.data.AnarchyMode)
		{
			IRCConnection.SendMessage(string.Format(disabledMessage, user), user, !isWhisper);
			return;
		}

		if (toggle)
			OtherModes.Toggle(mode);
		else
			OtherModes.Set(mode, on);
		IRCConnection.SendMessage($"{OtherModes.GetName(OtherModes.nextMode)} mode will be enabled next round.", user, !isWhisper);
	}

	private static void ShowRank(Leaderboard.LeaderboardEntry entry, string targetUser, string user, bool isWhisper, bool numeric = false) => ShowRank(entry == null ? null : new[] { entry }, targetUser, user, isWhisper, numeric);

	private static void ShowRank(IList<Leaderboard.LeaderboardEntry> entries, string targetUser, string user, bool isWhisper, bool numeric = false)
	{
		if (entries == null && numeric)
			IRCConnection.SendMessage(TwitchPlaySettings.data.RankTooLow, user, !isWhisper);
		else if (entries == null)
			IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.DoYouEvenPlayBro, targetUser), user, !isWhisper);
		else
		{
			foreach (var entry in entries)
			{
				string txtSolver = "";
				string txtSolo = "";
				if (entry.TotalSoloClears > 0)
				{
					var recordTimeSpan = TimeSpan.FromSeconds(entry.RecordSoloTime);
					txtSolver = TwitchPlaySettings.data.SolverAndSolo;
					txtSolo = string.Format(TwitchPlaySettings.data.SoloRankQuery, entry.SoloRank, (int) recordTimeSpan.TotalMinutes, recordTimeSpan.Seconds);
				}
				IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.RankQuery, entry.UserName, entry.Rank, entry.SolveCount, entry.StrikeCount, txtSolver, txtSolo, entry.SolveScore), user, !isWhisper);
			}
		}
	}

	private static int GetMaximumModules(KMGameInfo inf, int maxAllowed = int.MaxValue) => Math.Min(TPElevatorSwitch.IsON ? 54 : inf.GetMaximumBombModules(), maxAllowed);

	private static DateTime GetBuildDateTime(string path)
	{
		const int c_PeHeaderOffset = 60;
		const int c_LinkerTimestampOffset = 8;

		byte[] buffer = new byte[2048];

		using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
			stream.Read(buffer, 0, 2048);

		int offset = BitConverter.ToInt32(buffer, c_PeHeaderOffset);
		int secondsSince1970 = BitConverter.ToInt32(buffer, offset + c_LinkerTimestampOffset);
		DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		DateTime linkTimeUtc = epoch.AddSeconds(secondsSince1970);

		return linkTimeUtc;
	}

	private static string ResolveMissionID(KMGameInfo inf, string targetID, out string failureMessage)
	{
		failureMessage = null;
		var missions = ModManager.Instance.ModMissions;

		var mission = missions.FirstOrDefault(x => Regex.IsMatch(x.name, "mod_.+_" + Regex.Escape(targetID), RegexOptions.CultureInvariant | RegexOptions.IgnoreCase));
		if (mission == null)
		{
			failureMessage = $"Unable to find a mission with an ID of “{targetID}”.";
			return null;
		}

		var availableMods = inf.GetAvailableModuleInfo().Where(x => x.IsMod).Select(y => y.ModuleId).ToList();
		if (MultipleBombs.Installed())
			availableMods.Add("Multiple Bombs");
		var missingMods = new HashSet<string>();
		var modules = ComponentSolverFactory.GetModuleInformation().ToList();

		var generatorSetting = mission.GeneratorSetting;
		var componentPools = generatorSetting.ComponentPools;
		int moduleCount = 0;
		foreach (var componentPool in componentPools)
		{
			moduleCount += componentPool.Count;
			var modTypes = componentPool.ModTypes;
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
		if (moduleCount > GetMaximumModules(inf))
		{
			failureMessage = TPElevatorSwitch.IsON
				? $"Mission “{targetID}” was found; however, this mission has too many modules to use in the elevator."
				: $"Mission “{targetID}” was found; however, there is no bomb case with at least {moduleCount} modules currently installed and enabled.";
			return null;
		}

		return mission.name;
	}

	private static IEnumerator RunWrapper(string user, bool isWhisper, Func<IEnumerator> action)
	{
		if (TwitchPlaysService.Instance.CurrentState != KMGameInfo.State.PostGame && TwitchPlaysService.Instance.CurrentState != KMGameInfo.State.Setup)
		{
			IRCConnection.SendMessage("You can't use the !run command right now.");
			return null;
		}

		if (!((TwitchPlaySettings.data.EnableRunCommand && (TwitchPlaySettings.data.EnableTwitchPlaysMode || UserAccess.HasAccess(user, AccessLevel.Defuser, true))) || UserAccess.HasAccess(user, AccessLevel.Mod, true) || TwitchPlaySettings.data.AnarchyMode) || isWhisper)
		{
			IRCConnection.SendMessageFormat(TwitchPlaySettings.data.RunCommandDisabled, user);
			return null;
		}
		return action();
	}

	private static void ProfileWrapper(string profileName, string user, bool isWhisper, Action<string, string> action)
	{
		var profileString = ProfileHelper.GetProperProfileName(profileName);
		if (TwitchPlaySettings.data.ProfileWhitelist.Contains(profileString))
			action(profileString.Replace(' ', '_'), profileString);
		else
			IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.ProfileNotWhitelisted, profileName), user, !isWhisper);
	}

	private static IEnumerator RunDistribution(string user, int modules, KMGameInfo inf, ModuleDistributions distribution)
	{
		if (!distribution.Enabled && !UserAccess.HasAccess(user, AccessLevel.Mod) && !TwitchPlaySettings.data.AnarchyMode)
		{
			IRCConnection.SendMessageFormat("Sorry, distribution \"{0}\" is disabled", distribution.DisplayName);
			return null;
		}

		if (modules < distribution.MinModules)
		{
			IRCConnection.SendMessage($"Sorry, the minimum number of modules for \"{distribution.DisplayName}\" is {distribution.MinModules}.");
			return null;
		}

		int maxModules = GetMaximumModules(inf, distribution.MaxModules);
		if (modules > maxModules)
		{
			if (modules > distribution.MaxModules)
				IRCConnection.SendMessage($"Sorry, the maximum number of modules for {distribution.DisplayName} is {distribution.MaxModules}.");
			else
				IRCConnection.SendMessage($"Sorry, the maximum number of modules is \"{maxModules}\".");
			return null;
		}

		int vanillaModules = Mathf.FloorToInt(modules * distribution.Vanilla);
		int moddedModules = Mathf.FloorToInt(modules * distribution.Modded);
		int bothModules = modules - moddedModules - vanillaModules;

		var mission = ScriptableObject.CreateInstance<KMMission>();
		var pools = new List<KMComponentPool>
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
		if (FactoryRoomAPI.Installed() && OtherModes.ZenModeOn)
			pools.Add(new KMComponentPool { Count = 8, ModTypes = new List<string> { "Factory Mode" } });

		mission.PacingEventsEnabled = true;
		mission.DisplayName = modules + " " + distribution.DisplayName;
		mission.GeneratorSetting = OtherModes.TimeModeOn
			? new KMGeneratorSetting()
			{
				ComponentPools = pools,
				TimeLimit = TwitchPlaySettings.data.TimeModeStartingTime * 60,
				NumStrikes = 9
			}
			: new KMGeneratorSetting()
			{
				ComponentPools = pools,
				TimeLimit = (120 * modules) - (60 * vanillaModules),
				NumStrikes = Math.Max(3, modules / 12)
			};

		int rewardPoints = (5 * modules) - (3 * vanillaModules);
		TwitchPlaySettings.SetRewardBonus(rewardPoints);
		IRCConnection.SendMessage("Reward for completing bomb: " + rewardPoints);
		return RunMissionCoroutine(mission);
	}

	private static IEnumerator RunMissionCoroutine(KMMission mission, string seed = "-1")
	{
		if (TwitchPlaysService.Instance.CurrentState == KMGameInfo.State.PostGame)
		{
			// Press the “back” button
			var e = PostGameCommands.Continue();
			while (e.MoveNext())
				yield return e;

			// Wait until we’re back in the setup room
			yield return new WaitUntil(() => TwitchPlaysService.Instance.CurrentState == KMGameInfo.State.Setup);
		}

		TwitchPlaysService.Instance.GetComponent<KMGameCommands>().StartMission(mission, seed);
		OtherModes.RefreshModes(KMGameInfo.State.Transitioning);
	}

	private static IEnumerator RunMissionCoroutine(string missionId, string seed = "-1")
	{
		if (TwitchPlaysService.Instance.CurrentState == KMGameInfo.State.PostGame)
		{
			// Press the “back” button
			var e = PostGameCommands.Continue();
			while (e.MoveNext())
				yield return e;

			// Wait until we’re back in the setup room
			yield return new WaitUntil(() => TwitchPlaysService.Instance.CurrentState == KMGameInfo.State.Setup);
		}

		TwitchPlaysService.Instance.GetComponent<KMGameCommands>().StartMission(missionId, seed);
		OtherModes.RefreshModes(KMGameInfo.State.Transitioning);
	}

	private static void CameraChanged(string user, bool isWhisper)
	{
		Transform camera = GameRoom.SecondaryCamera.transform;

		DebugHelper.Log($"Camera Position = {Math.Round(camera.localPosition.x, 3)},{Math.Round(camera.localPosition.y, 3)},{Math.Round(camera.localPosition.z, 3)}");
		DebugHelper.Log($"Camera Euler Angles = {Math.Round(camera.localEulerAngles.x, 3)},{Math.Round(camera.localEulerAngles.y, 3)},{Math.Round(camera.localEulerAngles.z, 3)}");
		IRCConnection.SendMessage($"Camera Position = {Math.Round(camera.localPosition.x, 3)},{Math.Round(camera.localPosition.y, 3)},{Math.Round(camera.localPosition.z, 3)}, Camera Euler Angles = {Math.Round(camera.localEulerAngles.x, 3)},{Math.Round(camera.localEulerAngles.y, 3)},{Math.Round(camera.localEulerAngles.z, 3)}", user, !isWhisper);
	}
	#endregion
}
