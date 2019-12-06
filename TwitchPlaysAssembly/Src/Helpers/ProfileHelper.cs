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

	static Type ProfileManagerType;
	static MethodInfo ReloadActiveConfigurationMethod;

	public static void ReloadActiveConfiguration()
	{
		ProfileManagerType ??= ReflectionHelper.FindType("ProfileManager");
		ReloadActiveConfigurationMethod ??= ProfileManagerType?.GetMethod("ReloadActiveConfiguration", BindingFlags.Public | BindingFlags.Static);
		ReloadActiveConfigurationMethod?.Invoke(null, null);
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

	public static string GetProperProfileName(string profile) => TwitchPlaySettings.data.ProfileWhitelist.FirstOrDefault(str => string.Equals(str, profile, StringComparison.InvariantCultureIgnoreCase));

	public static bool Add(string profile)
	{
		List<string> temp = Profiles;
		if (temp.Contains(profile)) return false;
		temp.Add(profile);
		Profiles = temp;

		return true;
	}

	public static bool Remove(string profile)
	{
		List<string> temp = Profiles;
		if (!temp.Remove(profile)) return false;
		Profiles = temp;

		return true;
	}
}
