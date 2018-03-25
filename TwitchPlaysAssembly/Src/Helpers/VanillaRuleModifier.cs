using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VanillaRuleModifier
{
	private static GameObject _gameObject;

	private static IDictionary<string, object> Properties
	{
		get
		{
			return _gameObject == null
				? null
				: _gameObject.GetComponent<IDictionary<string, object>>();
		}
	}

	public static IEnumerator Refresh()
	{
		_gameObject = GameObject.Find("VanillaRuleModifierProperties");
		for (var i = 0; i < 120 && _gameObject == null; i++)
		{
			yield return null;
			_gameObject = GameObject.Find("VanillaRuleModifierProperties");
		}
	}

	public static int GetRuleSeed()
	{
		return (Properties != null && Properties.TryGetValue(RuleSeed, out object value))
			? (int) value
			: 1;
	}

	public static void SetRuleSeed(int seed, bool saveSettings = false)
	{
		if (Properties != null && Properties.ContainsKey(RuleSeed))
			Properties[RuleSeed] = new object[] {seed, saveSettings};
	}

	public static string GetRuleManualDirectory()
	{
		return (Properties != null && Properties.TryGetValue(GetRuleManual, out object value))
			? (string) value
			: null;
	}

	public static bool IsSeedVanilla()
	{
		return (Properties == null || !Properties.TryGetValue(SeedIsVanilla, out object value)) || (bool) value;
	}

	public static bool IsSeedModded()
	{
		return (Properties != null && Properties.TryGetValue(SeedIsModded, out object value)) && (bool) value;
	}

	public static bool Installed()
	{
		return Properties != null;
	}

	public static int GetModuleRuleSeed(string moduleType)
	{
		object value = 1;
		var result = (Properties != null && Properties.TryGetValue($"RuleSeedModifier_{moduleType}", out value));
		return result ? (int)value : 1;
	}

	private const string RuleSeed = "RuleSeed";
	private const string SeedIsVanilla = "IsSeedVanilla";
	private const string SeedIsModded = "IsSeedModded";
	private const string GetRuleManual = "GetRuleManual";
}