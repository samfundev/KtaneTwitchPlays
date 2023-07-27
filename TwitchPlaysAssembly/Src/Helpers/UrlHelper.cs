using System;
using System.IO;
using System.Linq;
using UnityEngine;

public class UrlHelper : MonoBehaviour
{
	public static UrlHelper Instance;

	private void Awake() => Instance = this;

	public static void ChangeMode(bool toShort)
	{
		TwitchPlaySettings.data.LogUploaderShortUrls = toShort;
		TwitchPlaySettings.WriteDataToFile();
	}

	public static bool ToggleMode()
	{
		TwitchPlaySettings.data.LogUploaderShortUrls = !TwitchPlaySettings.data.LogUploaderShortUrls;
		TwitchPlaySettings.WriteDataToFile();
		return TwitchPlaySettings.data.LogUploaderShortUrls;
	}

	public static string LogAnalyserFor(string url) => string.Format(TwitchPlaySettings.data.AnalyzerUrl + "#url={0}", url);

	public static string CommandReference => TwitchPlaySettings.data.LogUploaderShortUrls ? "https://tinyurl.com/v3twx5a" : "https://samfundev.github.io/KtaneTwitchPlays";

	public static string ManualFor(string module, string type = "html", bool useVanillaRuleModifier = false) => string.Format(TwitchPlaySettings.data.RepositoryUrl + "{0}/{1}.{2}{3}", type.ToUpper(), NameToUrl(module), type, (useVanillaRuleModifier && type.Equals("html")) ? $"#{VanillaRuleModifier.GetRuleSeed()}" : "");

	public static string MissionLink(string mission) => "https://bombs.samfun.dev/mission/" + TwitchUrlEscape(mission);

	private static string TwitchUrlEscape(string name) => Uri.EscapeDataString(Uri.UnescapeDataString(name)).Replace("*", "%2A").Replace("!", "%21");
	
	private static string NameToUrl(string name) => Uri.EscapeDataString(Uri.UnescapeDataString(name).Replace("'", "’").Split(InvalidCharacters).Join("")).Replace("*", "%2A").Replace("!", "%21");

	private static readonly char[] InvalidCharacters = Path.GetInvalidFileNameChars().Where(c => c != '*').ToArray();
}
