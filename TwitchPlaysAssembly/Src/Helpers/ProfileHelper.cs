using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

static class ProfileHelper
{
	static readonly string modSelectorConfig = Path.Combine(Application.persistentDataPath, "modSelectorConfig.json");

	public static List<string> Profiles
	{
		get => JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(modSelectorConfig));
		set => File.WriteAllText(modSelectorConfig, JsonConvert.SerializeObject(value));
	}

	public static string GetProperProfileName(string profile) => TwitchPlaySettings.data.ProfileWhitelist.FirstOrDefault(str => str.ToLowerInvariant() == profile);

	public static string GetProfileFileName(string profile) => GetProperProfileName(profile).Replace(' ', '_');

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
