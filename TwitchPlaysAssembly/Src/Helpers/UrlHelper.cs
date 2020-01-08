using System;
using System.IO;
using System.Linq;
using UnityEngine;

public class UrlHelper : MonoBehaviour
{
	public static UrlHelper Instance;

	private void Awake() => Instance = this;

	public void ChangeMode(bool toShort)
	{
		TwitchPlaySettings.data.LogUploaderShortUrls = toShort;
		TwitchPlaySettings.WriteDataToFile();
	}

	public bool ToggleMode()
	{
		TwitchPlaySettings.data.LogUploaderShortUrls = !TwitchPlaySettings.data.LogUploaderShortUrls;
		TwitchPlaySettings.WriteDataToFile();
		return TwitchPlaySettings.data.LogUploaderShortUrls;
	}

	public string LogAnalyserFor(string url) => string.Format(TwitchPlaySettings.data.AnalyzerUrl + "#url={0}", url);

	public string CommandReference => TwitchPlaySettings.data.LogUploaderShortUrls ? "https://goo.gl/rQUH8y" : "https://github.com/samfun123/KtaneTwitchPlays/wiki/Commands";

	public string ManualFor(string module, string type = "html", bool useVanillaRuleModifier = false) => string.Format(TwitchPlaySettings.data.RepositoryUrl + "{0}/{1}.{2}{3}", type.ToUpper(), NameToUrl(module), type, (useVanillaRuleModifier && type.Equals("html")) ? $"#{VanillaRuleModifier.GetRuleSeed()}" : "");

	private string NameToUrl(string name) => Uri.EscapeDataString(Uri.UnescapeDataString(name).Replace("'", "’").Split(InvalidCharacters).Join("")).Replace("*", "%2A");

	private static readonly char[] InvalidCharacters = Path.GetInvalidFileNameChars().Where(c => c != '*').ToArray();
}
