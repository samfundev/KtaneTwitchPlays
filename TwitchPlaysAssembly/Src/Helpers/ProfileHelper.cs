using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;
using static Repository;

static class ProfileHelper
{
	static readonly string modSelectorConfig = Path.Combine(Application.persistentDataPath, "modSelectorConfig.json");
	public static readonly string ProfileFolder = Path.Combine(Application.persistentDataPath, "ModProfiles");

	static Type ProfileManagerType;
	static MethodInfo ReloadActiveConfigurationMethod;

	public static void ReloadActiveConfiguration()
	{
		ProfileManagerType = ProfileManagerType ?? ReflectionHelper.FindType("ProfileManager");
		ReloadActiveConfigurationMethod = ReloadActiveConfigurationMethod ?? ProfileManagerType?.GetMethod("ReloadActiveConfiguration", BindingFlags.Public | BindingFlags.Static);
		ReloadActiveConfigurationMethod?.Invoke(null, null);
		TwitchGame.RetryAllowed = false;
	}

	public static List<string> Profiles
	{
		get => JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(modSelectorConfig));
		set
		{
			File.WriteAllText(modSelectorConfig, JsonConvert.SerializeObject(value));
			ReloadActiveConfiguration();
		}
	}

	public static string GetProperProfileName(string profile) => TwitchPlaySettings.data.ProfileWhitelist.Find(str => string.Equals(str, profile, StringComparison.InvariantCultureIgnoreCase));

	public static bool Enable(string profile)
	{
		List<string> temp = Profiles;
		if (temp.Contains(profile)) return false;
		temp.Add(profile);
		Profiles = temp;

		TwitchGame.RetryAllowed = false;
		return true;
	}

	public static bool Disable(string profile)
	{
		List<string> temp = Profiles;
		if (!temp.Remove(profile)) return false;
		Profiles = temp;

		TwitchGame.RetryAllowed = false;
		return true;
	}

	public static void Write(string profile, object modules)
	{
		if (!Directory.Exists(ProfileFolder)) return;

		File.WriteAllText(GetPath(profile), SettingsConverter.Serialize(
			new Dictionary<string, object>()
			{
				{ "DisabledList", modules },
				{ "Operation", 1 }
			}
		));
	}

	public static void Delete(string profile)
	{
		if (!Directory.Exists(ProfileFolder)) return;

		File.Delete(GetPath(profile));
	}

	public static IEnumerator LoadAutoProfiles()
	{
		yield return null;
		yield return LoadData();
		var modules = Modules.Where(x => (x.Type == "Needy" || x.Type == "Regular") && x.TwitchPlays != null).ToList();
		var parsedNeedyModules = new List<Tuple<string, List<string>>>();
		foreach (var needyModule in modules.Where(x => x.Type == "Needy"))
		{
			var info = ComponentSolverFactory.GetDefaultInformation(needyModule.ModuleID, false);
			if (info == null)
				continue;
			var scoreParts = info.scoreString.Trim().Split(' ').ToList();
			if (scoreParts[0] == "UN")
			{
				scoreParts.RemoveAt(0);
			}

			parsedNeedyModules.Add(new Tuple<string, List<string>>(info.moduleID, scoreParts));
		}

		var activationBasedMods = new List<Tuple<string, double>>();
		var needyProfiles = new Dictionary<string, HashSet<string>>();

		foreach (var module in parsedNeedyModules)
		{
			string s;
			switch (module.Second.Count)
			{
				case 1 when module.Second[0] == "0":
					s = "No0";
					break;
				case 1:
					s = "NoStaticNeedies";
					break;
				case 2 when module.Second[0] == "D":
					activationBasedMods.Add(new Tuple<string, double>(module.First, double.Parse(module.Second[1])));
					continue;
				case 2 when module.Second[0] == "T":
					s = "NoTimeNeedies";
					break;
				default:
					s = "NoOtherNeedies";
					break;
			}

			if (!needyProfiles.ContainsKey(s))
			{
				needyProfiles.Add(s, new HashSet<string> { module.First });
				continue;
			}
			needyProfiles[s].Add(module.First);
		}

		foreach (var mod in activationBasedMods)
		{
			if (mod.Second == 0)
			{
				if (!needyProfiles.ContainsKey("No0"))
					needyProfiles.Add("No0", new HashSet<string> { mod.First });
				else
					needyProfiles["No0"].Add(mod.First);
				continue;
			}
			var score = Math.Floor(mod.Second);
			var scoreString = $"No{(score == 0 ? score + "+" : score >= 10 ? "10+" : score.ToString())}";

			if (!needyProfiles.ContainsKey(scoreString))
				needyProfiles.Add(scoreString, new HashSet<string> { mod.First });
			else
				needyProfiles[scoreString].Add(mod.First);
		}

		var solvableProfiles = new Dictionary<string, HashSet<string>>
		{
			{"NoBossModules", new HashSet<string>() },
			{"NoPseudoNeedy", new HashSet<string>() },
		};
		foreach (var module in modules.Where(x => x.Type == "Regular"))
		{
			var info = ComponentSolverFactory.GetDefaultInformation(module.ModuleID, false);
			if (info == null)
				continue;

			if (info.moduleID.IsBossMod())
				solvableProfiles["NoBossModules"].Add(info.moduleID);
			if (info.moduleID.ModHasQuirk("PseudoNeedy"))
				solvableProfiles["NoPseudoNeedy"].Add(info.moduleID);
		}

		var profilesPath = Path.Combine(Application.persistentDataPath, "ModProfiles");
		if (Directory.Exists(profilesPath))
		{
			foreach (var profile in needyProfiles)
				Write(profile.Key, profile.Value);
			foreach (var profile in solvableProfiles)
				Write(profile.Key, profile.Value);

			foreach (var profile in needyProfiles.Where(x => !TwitchPlaySettings.data.ProfileWhitelist.Contains(x.Key)))
				TwitchPlaySettings.data.ProfileWhitelist.Add(profile.Key);
			foreach (var profile in solvableProfiles.Where(x => !TwitchPlaySettings.data.ProfileWhitelist.Contains(x.Key)))
				TwitchPlaySettings.data.ProfileWhitelist.Add(profile.Key);

			var profileWhitelist = TwitchPlaySettings.data.ProfileWhitelist.Where(x =>
					new Regex(@"^No(?:(?:[0-9]0?\+?)|(?:StaticNeedies)|(?:TimeNeedies)|(?:OtherNeedies))$").Match(x)
						.Success)
				.ToList();
			foreach (var profile in profileWhitelist)
			{
				if (!needyProfiles.ContainsKey(profile))
				{
					var path = Path.Combine(profilesPath, $"{profile}.json");
					File.Delete(path);
					TwitchPlaySettings.data.ProfileWhitelist.Remove(profile);
				}
			}

			TwitchPlaySettings.WriteDataToFile();
			DebugHelper.Log("Auto Profiles loaded successfully!");
		}
		else
		{
			DebugHelper.LogError("Could not find ProfilesPath!");
		}
	}

	public static bool SetState(string profileName, string module, bool state)
	{
		var profile = GetProfile(profileName);
		var success = false;

		if (state && (!profile.EnabledList.Contains(module) || profile.DisabledList.Contains(module)))
		{
			success |= profile.EnabledList.Add(module);
			success |= profile.DisabledList.Remove(module);
		}
		else if (!state && (profile.EnabledList.Contains(module) || !profile.DisabledList.Contains(module)))
		{
			success |= profile.EnabledList.Remove(module);
			success |= profile.DisabledList.Add(module);
		}

		if (success)
		{
			File.WriteAllText(GetPath(profileName), JsonConvert.SerializeObject(profile, Formatting.Indented));
			ReloadActiveConfiguration();
		}

		return success;
	}

	public static Profile GetProfile(string profileName) => JsonConvert.DeserializeObject<Profile>(File.ReadAllText(GetPath(profileName)));

	public static string GetPath(string profileName) => Path.Combine(ProfileFolder, $"{profileName.Replace(' ', '_')}.json");

	public class Profile
	{
#pragma warning disable CS0649
		public HashSet<string> DisabledList;
		public HashSet<string> EnabledList = new HashSet<string>();
		public int Operation;
#pragma warning restore CS0649
	}
}
