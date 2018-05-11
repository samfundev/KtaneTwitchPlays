using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class TranslatedModuleHelper
{
	private static readonly Dictionary<string, string> ManualCodeAddons = new Dictionary<string, string>()
	{
		{"en", "" },
		{"cu", null },
		{"cs", "%20translated%20full%20(%C4%8De%C5%A1tina)" },
		{"da", "%20translated%20full%20(Dansk)" },
		{"de", "%20translated%20full%20(Deutsch)" },
		{"et", "%20translated%20full%20(eesti)" },
		{"es", "%20translated%20full%20(Espa%C3%B1ol)" },
		{"eo", "%20translated%20full%20(Esperanto)" },
		{"fr", "%20translated%20full%20(Fran%C3%A7ais)" },
		{"he", "%20translated%20full%20(%D7%A2%D7%91%D7%A8%D7%99%D7%AA)" },
		{"ko", "%20translated%20full%20(%ED%95%9C%EA%B5%AD%EC%96%B4)" },
		{"it", "%20translated%20full%20(Italiano)" },
		{"nl", "%20translated%20full%20(Nederlands)" },
		{"jp", "%20translated%20full%20(%E6%97%A5%E6%9C%AC%E8%AA%9E)" },
		{"pl", "%20translated%20full%20(Polski)" },
		{"pt-br", "%20translated%20full%20(Portugu%C3%AAs%20do%20Brasil)" },
		{"ru", "%20translated%20full%20(%D0%A0%D1%83%D1%81%D1%81%D0%BA%D0%B8%D0%B9)" },
		{"fi", "%20translated%20full%20(Suomi)" },
		{"sv", "%20translated%20full%20(Svenska)" },
		{"th", "%20translated%20full%20(%E0%B8%A0%E0%B8%B2%E0%B8%A9%E0%B8%B2%E0%B9%84%E0%B8%97%E0%B8%A2)" },
		{"zh-cn", "%20translated%20full%20(%E7%AE%80%E4%BD%93%E4%B8%AD%E6%96%87)" },
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
		{"jp", " (日本語)" },
		{"pl", " (Polski)" },
		{"pt-br", " (Português do Brasil)" },
		{"ru", " (Русский)" },
		{"fi", " (Suomi)" },
		{"sv", " (Svenska)" },
		{"th", " (ภาษาไทย)" },
		{"zh-cn", " (简体中文)" },
	};

	public static string GetModuleDisplayNameAddon(BombComponent bombComponent)
	{
		try
		{
			if (bombComponent == null || bombComponent.GetComponent<KMModSettings>() == null || !InitialializeSettings()) return " (Unknown)";
			string[] languages = (string[])_getAllCurrentLanguagesMethod.Invoke(null, new object[] { bombComponent.GetComponent<KMModSettings>(), null });
			if (languages == null || languages.Length == 0 || languages[0].Length == 0 || !DisplayNameAddons.TryGetValue(languages[0].ToLowerInvariant(), out string addon)) return " (Unknown)";
			return addon;
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "Could not get the Language of this translated module due to an exception:");
			return " (Unknown)";
		}
	}

	public static string GetManualCodeAddOn(BombComponent bombComponent)
	{
		try
		{
			if (bombComponent == null || bombComponent.GetComponent<KMModSettings>() == null || !InitialializeSettings()) return "";
			string[] languages = (string[]) _getAllCurrentLanguagesMethod.Invoke(null, new object[] {bombComponent.GetComponent<KMModSettings>(), null });
			if (languages == null || languages.Length == 0 || languages[0].Length == 0 || !ManualCodeAddons.TryGetValue(languages[0].ToLowerInvariant(), out string addon)) return "";
			return addon;
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "Could not get the Language of this translated module due to an exception:");
			return "";
		}
	}

	private static bool InitialializeSettings()
	{
		if (_getAllCurrentLanguagesMethod != null) return true;
		_translatedModulesSettingsType = ReflectionHelper.FindType("TranslatedModulesSettings");
		if (_translatedModulesSettingsType == null) return false;
		_getAllCurrentLanguagesMethod = _translatedModulesSettingsType.GetMethod("GetAllCurrentLangIds", BindingFlags.Public | BindingFlags.Static);
		return _getAllCurrentLanguagesMethod != null;
	}

	private static Type _translatedModulesSettingsType = null;
	private static MethodInfo _getAllCurrentLanguagesMethod = null;
}
