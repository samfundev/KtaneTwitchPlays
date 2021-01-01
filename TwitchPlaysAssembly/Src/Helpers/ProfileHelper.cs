using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

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

	public static bool Add(string profile)
	{
		List<string> temp = Profiles;
		if (temp.Contains(profile)) return false;
		temp.Add(profile);
		Profiles = temp;

		TwitchGame.RetryAllowed = false;
		return true;
	}

	public static bool Remove(string profile)
	{
		List<string> temp = Profiles;
		if (!temp.Remove(profile)) return false;
		Profiles = temp;

		TwitchGame.RetryAllowed = false;
		return true;
	}

	public static bool SetState(string profilePath, string module, bool state)
	{
		var profile = GetProfile(profilePath);
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
			File.WriteAllText(profilePath, JsonConvert.SerializeObject(profile, Formatting.Indented));
			ReloadActiveConfiguration();
		}

		return success;
	}

	public static Profile GetProfile(string profilePath) => JsonConvert.DeserializeObject<Profile>(File.ReadAllText(profilePath));

	public class Profile
	{
#pragma warning disable CS0649
		public HashSet<string> DisabledList;
		public HashSet<string> EnabledList = new HashSet<string>();
		public int Operation;
#pragma warning restore CS0649
	}
}
