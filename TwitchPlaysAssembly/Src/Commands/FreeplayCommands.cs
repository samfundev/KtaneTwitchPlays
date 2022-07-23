using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using Assets.Scripts.Settings;
using UnityEngine;

/// <summary>Commands for the freeplay briefcase.</summary>
/// <prefix>freeplay </prefix>
public static class FreeplayCommands
{
	#region Commands

	/// <name>Needy</name>
	/// <syntax>needy on\nneedy off</syntax>
	/// <summary>Enables or disables the needy switch.</summary>
	[Command(@"needy(?: +(on)| +off)")]
	public static void Needy(FloatingHoldable holdable, [Group(1)] bool on, string user, bool isWhisper) => SetSetting(holdable, on, user, isWhisper, s => s.HasNeedy, s => s.NeedyToggle, TwitchPlaySettings.data.EnableFreeplayNeedy, TwitchPlaySettings.data.FreePlayNeedyDisabled);
	/// <name>Hardcore</name>
	/// <syntax>hardcore on\nhardcore off</syntax>
	/// <summary>Enables or disables the hardcore switch.</summary>
	[Command(@"hardcore(?: +(on)| +off)")]
	public static void Hardcore(FloatingHoldable holdable, [Group(1)] bool on, string user, bool isWhisper) => SetSetting(holdable, on, user, isWhisper, s => s.IsHardCore, s => s.HardcoreToggle, TwitchPlaySettings.data.EnableFreeplayHardcore, TwitchPlaySettings.data.FreePlayHardcoreDisabled);
	/// <name>Mods Only</name>
	/// <syntax>mods only\nmods only off</syntax>
	/// <summary>Enables or disables the mods only.</summary>
	[Command(@"mods ?only(?: +(on)| +off)")]
	public static void ModsOnly(FloatingHoldable holdable, [Group(1)] bool on, string user, bool isWhisper) => SetSetting(holdable, on, user, isWhisper, s => s.OnlyMods, s => s.ModsOnly, TwitchPlaySettings.data.EnableFreeplayModsOnly, TwitchPlaySettings.data.FreePlayModsOnlyDisabled);

	/// <name>Set Time</name>
	/// <syntax>time (hours):[minutes]:[seconds]</syntax>
	/// <summary>Sets the amount of time the bomb will have.</summary>
	[Command(@"timer? +(\d+):(\d{1,3}):(\d{2})")]
	public static IEnumerator ChangeTimerHours(FloatingHoldable holdable, [Group(1)] int hours, [Group(2)] int minutes, [Group(3)] int seconds) => SetBombTimer(holdable, hours, minutes, seconds);
	[Command(@"timer? +(\d{1,3}):(\d{2})")]
	public static IEnumerator ChangeTimer(FloatingHoldable holdable, [Group(1)] int minutes, [Group(2)] int seconds) => SetBombTimer(holdable, 0, minutes, seconds);

	/// <name>Set Bombs</name>
	/// <syntax>bombs [bombs]</syntax>
	/// <summary>Sets the number of bombs.</summary>
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

	/// <name>Set Modules</name>
	/// <syntax>modules [modules]</syntax>
	/// <summary>Sets the number of modules each bomb will have.</summary>
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

	/// <name>Start</name>
	/// <syntax>start</syntax>
	/// <summary>Start the game.</summary>
	[Command(@"start")]
	public static void Start(FloatingHoldable holdable) => holdable.GetComponent<FreeplayDevice>().StartButton.GetComponent<Selectable>().Trigger();

	/// <name>Advanced Set / Start</name>
	/// <syntax>set [parameters]\nstart [parameters]</syntax>
	/// <summary>Sets or starts a bomb with a bunch of parameters. Combine any of the following to set the bomb parameters. (hours):[minutes]:[seconds], [#] bombs, [#] modules, hardcore, modsonly, needy.</summary>
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
		//Double the available free play time. (The doubling stacks with the Multiple bombs module installed)
		float originalMaxTime = FreeplayDevice.MAX_SECONDS_TO_SOLVE;
		int maxModules = (int) _maxModuleField.GetValue(device);
		int multiplier = MultipleBombs.Installed() ? (MultipleBombs.GetMaximumBombCount() * 2) - 1 : 1;
		float newMaxTime = 600f + (maxModules - 1) * multiplier * 60;
		FreeplayDevice.MAX_SECONDS_TO_SOLVE = newMaxTime;

		float currentTime = device.CurrentSettings.Time;
		int currentTimeIndex = Mathf.FloorToInt(currentTime) / 30;
		KeypadButton button = timeIndex > currentTimeIndex ? device.TimeIncrement : device.TimeDecrement;
		Selectable buttonSelectable = button.GetComponent<Selectable>();

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
		if (settingAllowed || !on || UserAccess.HasAccess(user, AccessLevel.Admin, true) || TwitchPlaySettings.data.AnarchyMode)
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
	private static readonly FieldInfo _maxModuleField = typeof(FreeplayDevice).GetField("maxModules", BindingFlags.NonPublic | BindingFlags.Instance);
	#endregion
}
