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
        object value;
        return (Properties != null && Properties.TryGetValue(RuleSeed, out value))
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
        object value;
        return (Properties != null && Properties.TryGetValue(GetRuleManual, out value))
            ? (string) value
            : null;
    }

    public static bool IsSeedVanilla()
    {
        object value;
        return (Properties == null || !Properties.TryGetValue(SeedIsVanilla, out value)) || (bool) value;
    }

    public static bool IsSeedModded()
    {
        object value;
        return (Properties != null && Properties.TryGetValue(SeedIsModded, out value)) && (bool) value;
    }

    public static bool Installed()
    {
        return Properties != null;
    }

    private const string RuleSeed = "RuleSeed";
    private const string SeedIsVanilla = "IsSeedVanilla";
    private const string SeedIsModded = "IsSeedModded";
    private const string GetRuleManual = "GetRuleManual";
}