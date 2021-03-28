using System;
using System.Collections.Generic;
using System.Reflection;

public static class TranslatedModuleHelper
{
	private static readonly Dictionary<string, string> ManualCodeAddons = new Dictionary<string, string>
	{
		{"en", "" },
		{"cu", null },
		{"cs", "%20translated%20(%C4%8De%C5%A1tina%20%E2%80%94%20*) p%C5%99elo%C5%BEen%C3%BD%20modul" },
		{"da", "%20translated%20(Dansk%20%E2%80%94%20*)" },
		{"de", "%20translated%20(Deutsch%20%E2%80%94%20*)" },
		{"et", "%20translated%20(eesti%20%E2%80%94%20*)" },
		{"es", "%20translated%20(Espa%C3%B1ol%20%E2%80%94%20*)" },
		{"eo", "%20translated%20(Esperanto%20%E2%80%94%20*)" },
		{"fr", "%20translated%20(Fran%C3%A7ais%20%E2%80%94%20*)" },
		{"he", "%20translated%20(%D7%A2%D7%91%D7%A8%D7%99%D7%AA%20%E2%80%94%20*)" },
		{"ko", "%20translated%20(%ED%95%9C%EA%B5%AD%EC%96%B4%20%E2%80%94%20*)" },
		{"it", "%20translated%20(Italiano%20%E2%80%94%20*)" },
		{"nl", "%20translated%20(Nederlands%20%E2%80%94%20*)" },
		{"no", "%20translated%20(Norsk%20%E2%80%94%20*)" },
		{"jp", "%20translated%20(%E6%97%A5%E6%9C%AC%E8%AA%9E%20%E2%80%94%20*)" },
		{"pl", "%20translated%20(Polski%20%E2%80%94%20*)" },
		{"pt-br", "%20translated%20(Portugu%C3%AAs%20do%20Brasil%20%E2%80%94%20*)" },
		{"ru", "%20translated%20(%D0%A0%D1%83%D1%81%D1%81%D0%BA%D0%B8%D0%B9%20%E2%80%94%20*)" },
		{"fi", "%20translated%20(Suomi%20%E2%80%94%20*)" },
		{"sv", "%20translated%20(Svenska%20%E2%80%94%20*)" },
		{"th", "%20translated%20(%E0%B8%A0%E0%B8%B2%E0%B8%A9%E0%B8%B2%E0%B9%84%E0%B8%97%E0%B8%A2%20%E2%80%94%20*)" },
		{"zh-cn", "%20translated%20(%E7%AE%80%E4%BD%93%E4%B8%AD%E6%96%87%20%E2%80%94%20*)" },
	};

	private static readonly Dictionary<string, string> DisplayNameAddons = new Dictionary<string, string>()
	{
		{"en", " (English)" },
		{"cu", " (Custom)" },
		{"cs", " (čeština)" },
		{"da", " (Dansk)" },
		{"de", " (Deutsch)" },
		{"et", " (eesti)" },
		{"es", " (Español)" },
		{"eo", " (Esperanto)" },
		{"fr", " (Français)" },
		{"he", " (עברית)" },
		{"ko", " (한국어)" },
		{"it", " (Italiano)" },
		{"nl", " (Nederlands)" },
		{"no", " (Norsk)" },
		{"jp", " (日本語)" },
		{"pl", " (Polski)" },
		{"pt-br", " (Português do Brasil)" },
		{"ru", " (Русский)" },
		{"fi", " (Suomi)" },
		{"sv", " (Svenska)" },
		{"th", " (ภาษาไทย)" },
		{"zh-cn", " (简体中文)" },
	};

	public static string GetModuleDisplayNameAddon(UnityEngine.Component component, Type componentType)
	{
		try
		{
			string languageCode = GetLanguageCode(component, componentType);
			return languageCode == null || !DisplayNameAddons.TryGetValue(languageCode, out string code) ? "" : code;
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "Could not get the Language of this translated module due to an exception:");
			return " (Unknown)";
		}
	}

	public static string GetManualCodeAddOn(UnityEngine.Component component, Type componentType)
	{
		try
		{
			string languageCode = GetLanguageCode(component, componentType);
			return languageCode == null || !ManualCodeAddons.TryGetValue(languageCode, out string code) ? "" : code;
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "Could not get the Language of this translated module due to an exception:");
			return "";
		}
	}

	public static string GetLanguageCode(UnityEngine.Component component, Type componentType)
	{
		try
		{
			FieldInfo langField = componentType.GetField("lang", BindingFlags.NonPublic | BindingFlags.Instance);
			object langObject = langField?.GetValue(component);
			FieldInfo languageField = langObject?.GetType().GetField("languageId", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			return (string) languageField?.GetValue(langObject);
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "Could not get the Language of this translated module due to an exception:");
			return "";
		}
	}
}
