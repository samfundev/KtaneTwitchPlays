using System.Text.RegularExpressions;
using UnityEngine;

public class UrlHelper : MonoBehaviour
{
	public static UrlHelper Instance;

	private void Awake()
	{
		Instance = this;
	}

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

	private string Escape(string toEscape)
	{
		return Regex.Replace(toEscape, @"[^\w%]", m => "%" + ((int)m.Value[0]).ToString("X2"));
	}

	public string LogAnalyser => "https://ktane.timwi.de/More/Logfile%20Analyzer.html";

	public string LogAnalyserFor(string url)
	{
		return string.Format(LogAnalyser + "#url={0}", url);
	}

	public string CommandReference => TwitchPlaySettings.data.LogUploaderShortUrls ? "https://goo.gl/rQUH8y" : "https://github.com/samfun123/KtaneTwitchPlays/wiki/Commands";

	public string ManualFor(string module, string type = "html", bool useVanillaRuleModifier = false)
	{
		if (useVanillaRuleModifier && VanillaRuleModifier.GetRuleSeed() != 1)
		{
			return $"{Repository}manual/{Escape(module)}.html?VanillaRuleSeed={VanillaRuleModifier.GetRuleSeed()}";
		}

		return string.Format(Repository + "{0}/{1}.{2}", type.ToUpper(), Escape(module), type);
	}

	public string VanillaManual = "http://www.bombmanual.com/";
	public string Repository = "https://ktane.timwi.de/";
}
