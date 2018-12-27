using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using Assets.Scripts.Settings;
using UnityEngine;

public static class FreeplayCommands
{
	#region Commands

	[Command(@"needy(?: +(on)| +off)", AccessLevel.Admin, accessLevelAnarchy: AccessLevel.Defuser)]
	public static void Needy(FloatingHoldable holdable, [Group(1)] bool on, string user, bool isWhisper) => SetSetting(holdable, on, user, isWhisper, s => s.HasNeedy, s => s.NeedyToggle, TwitchPlaySettings.data.EnableFreeplayNeedy, TwitchPlaySettings.data.FreePlayNeedyDisabled);
	[Command(@"hardcore(?: +(on)| +off)", AccessLevel.Admin, accessLevelAnarchy: AccessLevel.Defuser)]
	public static void Hardcore(FloatingHoldable holdable, [Group(1)] bool on, string user, bool isWhisper) => SetSetting(holdable, on, user, isWhisper, s => s.IsHardCore, s => s.HardcoreToggle, TwitchPlaySettings.data.EnableFreeplayHardcore, TwitchPlaySettings.data.FreePlayHardcoreDisabled);
	[Command(@"mods ?only(?: +(on)| +off)", AccessLevel.Admin, accessLevelAnarchy: AccessLevel.Defuser)]
	public static void ModsOnly(FloatingHoldable holdable, [Group(1)] bool on, string user, bool isWhisper) => SetSetting(holdable, on, user, isWhisper, s => s.OnlyMods, s => s.ModsOnly, TwitchPlaySettings.data.EnableFreeplayModsOnly, TwitchPlaySettings.data.FreePlayModsOnlyDisabled);

	[Command(@"timer? +(\d+):(\d{1,3}):(\d{2})")]
	public static IEnumerator ChangeTimerHours(FloatingHoldable holdable, [Group(1)] int hours, [Group(2)] int minutes, [Group(3)] int seconds) => SetBombTimer(holdable, hours, minutes, seconds);
	[Command(@"timer? +(\d{1,3}):(\d{2})")]
	public static IEnumerator ChangeTimer(FloatingHoldable holdable, [Group(1)] int minutes, [Group(2)] int seconds) => SetBombTimer(holdable, 0, minutes, seconds);

	[Command(@"bombs +(\d+)")]
	public static IEnumerator ChangeBombCount(FloatingHoldable holdable, [Group(1)] int bombCount)
	{
		if (!MultipleBombs.Installed())
			yield break;

		int currentBombCount = MultipleBombs.GetFreePlayBombCount();
		var buttonSelectable = holdable.GetComponent<Selectable>().Children[bombCount > currentBombCount ? 3 : 2];

		for (int hitCount = 0; hitCount < Mathf.Abs(bombCount - currentBombCount); ++hitCount)
		{
			int lastBombCount = MultipleBombs.GetFreePlayBombCount();
			buttonSelectable.Trigger();
			yield return new WaitForSeconds(0.01f);
			// Stop here if we hit a maximum or minimum
			if (lastBombCount == MultipleBombs.GetFreePlayBombCount())
				yield break;
		}
	}

	[Command(@"modules +(\d+)")]
	public static IEnumerator ChangeModuleCount(FloatingHoldable holdable, [Group(1)] int moduleCount)
	{
		var device = holdable.GetComponent<FreeplayDevice>();
		int currentModuleCount = device.CurrentSettings.ModuleCount;
		var button = (moduleCount > currentModuleCount ? device.ModuleCountIncrement : device.ModuleCountDecrement).GetComponent<Selectable>();

		for (int hitCount = 0; hitCount < Mathf.Abs(moduleCount - currentModuleCount); ++hitCount)
		{
			int lastModuleCount = device.CurrentSettings.ModuleCount;
			button.Trigger();
			yield return new WaitForSeconds(0.01f);
			if (lastModuleCount == device.CurrentSettings.ModuleCount)
				yield break;
		}
	}

	[Command(@"start")]
	public static void Start(FloatingHoldable holdable) => holdable.GetComponent<FreeplayDevice>().StartButton.GetComponent<Selectable>().Trigger();

	[Command(@"(set|start) +(.*)")]
	public static IEnumerator StartAdvanced(FloatingHoldable holdable, [Group(1)] string command, [Group(2)] string parameters, string user, bool isWhisper)
	{
		if (parameters.RegexMatch(out Match m, @"(\d):(\d{1,3}):(\d{2})"))
		{
			var e = SetBombTimer(holdable, int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value));
			while (e.MoveNext())
				yield return e.Current;
		}
		else if (parameters.RegexMatch(out m, @"(\d{1,3}):(\d{2})"))
		{
			var e = SetBombTimer(holdable, 0, int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value));
			while (e.MoveNext())
				yield return e.Current;
		}

		if (MultipleBombs.Installed() && parameters.RegexMatch(out m, @"(\d+) +bombs"))
		{
			var e = ChangeBombCount(holdable, int.Parse(m.Groups[1].Value));
			while (e.MoveNext())
				yield return e.Current;
		}
		if (parameters.RegexMatch(out m, @"(\d+) +modules"))
		{
			var e = ChangeModuleCount(holdable, int.Parse(m.Groups[1].Value));
			while (e.MoveNext())
				yield return e.Current;
		}

		yield return null;
		SetSetting(holdable, parameters.Contains("hardcore"), user, isWhisper, s => s.IsHardCore, s => s.HardcoreToggle, TwitchPlaySettings.data.EnableFreeplayHardcore, TwitchPlaySettings.data.FreePlayHardcoreDisabled);
		yield return null;
		SetSetting(holdable, parameters.Contains("modsonly"), user, isWhisper, s => s.OnlyMods, s => s.ModsOnly, TwitchPlaySettings.data.EnableFreeplayModsOnly, TwitchPlaySettings.data.FreePlayModsOnlyDisabled);
		yield return null;
		SetSetting(holdable, parameters.Contains("needy") || parameters.Contains("needies"), user, isWhisper, s => s.HasNeedy, s => s.NeedyToggle, TwitchPlaySettings.data.EnableFreeplayNeedy, TwitchPlaySettings.data.FreePlayNeedyDisabled);
		yield return null;

		if (command.EqualsIgnoreCase("start"))
			holdable.GetComponent<FreeplayDevice>().StartButton.GetComponent<Selectable>().Trigger();
	}
	#endregion

	#region Helper Methods

	private static IEnumerator SetBombTimer(FloatingHoldable holdable, int hours, int minutes, int seconds)
	{
		if (seconds >= 60)
			yield break;

		var device = holdable.GetComponent<FreeplayDevice>();
		int timeIndex = (hours * 120) + (minutes * 2) + (seconds / 30);
		DebugHelper.Log("Freeplay time doubling section");
		//Double the available free play time. (The doubling stacks with the Multiple bombs module installed)
		float originalMaxTime = FreeplayDevice.MAX_SECONDS_TO_SOLVE;
		int maxModules = (int) _maxModuleField.GetValue(device);
		int multiplier = MultipleBombs.Installed() ? (MultipleBombs.GetMaximumBombCount() * 2) - 1 : 1;
		float newMaxTime = 600f + (maxModules - 1) * multiplier * 60;
		FreeplayDevice.MAX_SECONDS_TO_SOLVE = newMaxTime;

		DebugHelper.Log("Freeplay settings reading section");
		float currentTime = device.CurrentSettings.Time;
		int currentTimeIndex = Mathf.FloorToInt(currentTime) / 30;
		KeypadButton button = timeIndex > currentTimeIndex ? device.TimeIncrement : device.TimeDecrement;
		Selectable buttonSelectable = button.GetComponent<Selectable>();

		DebugHelper.Log("Freeplay time setting section");
		for (int hitCount = 0; hitCount < Mathf.Abs(timeIndex - currentTimeIndex); ++hitCount)
		{
			currentTime = device.CurrentSettings.Time;
			buttonSelectable.Trigger();
			yield return new WaitForSeconds(0.01f);
			if (Mathf.FloorToInt(currentTime) == Mathf.FloorToInt(device.CurrentSettings.Time))
				break;
		}

		//Restore original max time, just in case Multiple bombs module was NOT installed, to avoid false detection.
		FreeplayDevice.MAX_SECONDS_TO_SOLVE = originalMaxTime;
	}

	private static void SetSetting(FloatingHoldable holdable, bool on, string user, bool isWhisper, Func<FreeplaySettings, bool> getCurrent, Func<FreeplayDevice, ToggleSwitch> toggle, bool settingAllowed, string disabledMessage)
	{
		if (settingAllowed || !on || UserAccess.HasAccess(user, AccessLevel.Admin, true))
		{
			var device = holdable.GetComponent<FreeplayDevice>();
			if (on != getCurrent(device.CurrentSettings))
				toggle(device).GetComponent<Selectable>().Trigger();
		}
		else
			IRCConnection.SendMessage(string.Format(disabledMessage, user), user, !isWhisper);
	}
	#endregion

	#region private static Static Fields
	private static FieldInfo _maxModuleField = typeof(FreeplayDevice).GetField("maxModules", BindingFlags.NonPublic | BindingFlags.Instance);
	#endregion
}
