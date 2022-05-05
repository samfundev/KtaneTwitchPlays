using System;
using System.Collections.Generic;
using System.Reflection;

public static class TranslatedModuleHelper
{
	private static readonly Dictionary<string, string> ManualCodeAddons = new Dictionary<string, string>
	{
		{"cs", " translated (čeština — *) přeložený modul" },
		{"da", " translated (Dansk — *)" },
		{"de", " translated (Deutsch — *)" },
		{"et", " translated (eesti — *)" },
		{"es", " translated (Español — *)" },
		{"eo", " translated (Esperanto — *)" },
		{"fr", " translated (Français — *)" },
		{"he", " translated (עברית — *)" },
		{"ko", " translated (한국어 — *)" },
		{"it", " translated (Italiano — *)" },
		{"nl", " translated (Nederlands — *)" },
		{"no", " translated (Norsk — *)" },
		{"jp", " translated (日本語 — *)" },
		{"pl", " translated (Polski — *)" },
		{"pt-br", " translated (Português do Brasil — *)" },
		{"ru", " translated (Русский — *)" },
		{"fi", " translated (Suomi — *)" },
		{"sv", " translated (Svenska — *)" },
		{"th", " translated (ภาษาไทย — *)" },
		{"zh-cn", " translated (简体中文 — *)" },
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

	public static string GetManualCodeAddOn(string languageCode)
	{
		return !ManualCodeAddons.TryGetValue(languageCode ?? "", out string addon) ? "" : addon;
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
