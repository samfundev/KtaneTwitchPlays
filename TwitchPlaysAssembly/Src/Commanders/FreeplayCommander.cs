using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using Assets.Scripts.Settings;
using UnityEngine;

public class FreeplayCommander : ICommandResponder
{
	#region Constructors
	static FreeplayCommander()
	{
		_maxModuleField = typeof(FreeplayDevice).GetField("maxModules", BindingFlags.NonPublic | BindingFlags.Instance);
	}

	public FreeplayCommander(FreeplayDevice freeplayDevice)
	{
		Instance = this;
		FreeplayDevice = freeplayDevice;
		Selectable = FreeplayDevice.GetComponent<Selectable>();
		SelectableChildren = Selectable.Children;
		FloatingHoldable = FreeplayDevice.GetComponent<FloatingHoldable>();
		SelectableManager = KTInputManager.Instance.SelectableManager;
	}
	#endregion

	#region Interface Implementation

	public IEnumerator RespondToCommand(string userNickName, string message, ICommandResponseNotifier responseNotifier, bool isWhisper = false)
	{
		IEnumerator respond = FreeplayRespondToCommand(userNickName, message, responseNotifier);
		bool result;
		do
		{
			try
			{
				result = respond.MoveNext();
			}
			catch (Exception ex)
			{
				DebugHelper.LogException(ex);
				result = false;
			}
			if (result)
				yield return respond.Current;
		} while (result);
	}

	public IEnumerator FreeplayRespondToCommand(string userNickName, string message, ICommandResponseNotifier responseNotifier)
	{
		message = message.ToLowerInvariant().Trim();

		if (message.EqualsAny("drop", "let go", "put down"))
		{
			IEnumerator drop = LetGoFreeplayDevice();
			while (drop.MoveNext())
				yield return drop.Current;
			yield break;
		}
		else
		{
			IEnumerator holdCoroutine = HoldFreeplayDevice();
			while (holdCoroutine.MoveNext())
			{
				yield return holdCoroutine.Current;
			}
		}

		string changeHoursTo = string.Empty;
		string changeMinutesTo = string.Empty;
		string changeSecondsTo = string.Empty;
		string changeBombsTo = string.Empty;
		string changeModulesTo = string.Empty;
		bool startBomb = false;

		if (message.EqualsAny("needy on", "needy off"))
		{
			if (TwitchPlaySettings.data.EnableFreeplayNeedy || UserAccess.HasAccess(userNickName, AccessLevel.Admin, true) || TwitchPlaySettings.data.AnarchyMode)
			{
				SetNeedy(message.Equals("needy on"));
			}
			else
			{
				IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.FreePlayNeedyDisabled, userNickName);
			}
		}
		else if (message.EqualsAny("hardcore on", "hardcore off"))
		{
			if (TwitchPlaySettings.data.EnableFreeplayHardcore || UserAccess.HasAccess(userNickName, AccessLevel.Admin, true) || TwitchPlaySettings.data.AnarchyMode)
			{
				SetHardcore(message.Equals("hardcore on"));
			}
			else
			{
				IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.FreePlayHardcoreDisabled, userNickName);
			}
		}
		else if (message.EqualsAny("mods only on", "mods only off"))
		{
			if (TwitchPlaySettings.data.EnableFreeplayModsOnly || UserAccess.HasAccess(userNickName, AccessLevel.Admin, true) || TwitchPlaySettings.data.AnarchyMode)
			{
				SetModsOnly(message.Equals("mods only on"));
			}
			else
			{
				IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.FreePlayModsOnlyDisabled, userNickName);
			}
		}
		else if (message.Equals("start"))
		{
			StartBomb();
		}
		else if (message.StartsWith("profile"))
		{
			string profile = message.Remove(0, 8).Trim();

			switch (profile)
			{
				case "single":
				case "solo":
					changeBombsTo = "1";
					changeHoursTo = "0";
					changeMinutesTo = "20";
					changeModulesTo = "11";
					break;

				case "double":
					changeBombsTo = "1";
					changeHoursTo = "0";
					changeMinutesTo = "40";
					changeModulesTo = "23";
					break;

				case "quadruple":
				case "quad":
					changeBombsTo = "1";
					changeHoursTo = "1";
					changeMinutesTo = "20";
					changeModulesTo = "47";
					break;

				case "dual single":
				case "dual solo":
					changeBombsTo = "2";
					changeHoursTo = "0";
					changeMinutesTo = "40";
					changeModulesTo = "11";
					break;

				case "dual double":
					changeBombsTo = "2";
					changeHoursTo = "1";
					changeMinutesTo = "20";
					changeModulesTo = "23";
					break;

				case "dual quadruple":
				case "dual quad":
					changeBombsTo = "2";
					changeHoursTo = "2";
					changeMinutesTo = "40";
					changeModulesTo = "47";
					break;
			}
		}
		else if (message.StartsWith("start"))
		{
			Match timerMatch = Regex.Match(message, "([0-9]):([0-9]{2}):([0-9]{2})");
			if (timerMatch.Success)
			{
				changeHoursTo = timerMatch.Groups[1].Value;
				changeMinutesTo = timerMatch.Groups[2].Value;
				changeSecondsTo = timerMatch.Groups[3].Value;
				message = message.Remove(timerMatch.Index, timerMatch.Length);
			}
			else
			{
				timerMatch = Regex.Match(message, "([0-9]+):([0-9]{2})");
				if (timerMatch.Success)
				{
					changeMinutesTo = timerMatch.Groups[1].Value;
					changeSecondsTo = timerMatch.Groups[2].Value;
					message = message.Remove(timerMatch.Index, timerMatch.Length);
				}
			}

			Match modulesMatch = Regex.Match(message, "[0-9]+");

			while (modulesMatch.Success)
			{
				if (int.TryParse(modulesMatch.Value, out int count))
				{
					if (count <= 2)
					{
						changeBombsTo = modulesMatch.Value;
					}
					else
					{
						changeModulesTo = modulesMatch.Value;
					}

					DebugHelper.Log("[FreeplayCommander] Setting {1} to {0}", modulesMatch.Value,
						count <= 2 ? "bombs" : "modules");
				}
				message = message.Remove(modulesMatch.Index, modulesMatch.Length);
				modulesMatch = Regex.Match(message, "[0-9]+");
			}

			startBomb = true;

			if (TwitchPlaySettings.data.EnableFreeplayHardcore || UserAccess.HasAccess(userNickName, AccessLevel.Admin, true) || TwitchPlaySettings.data.AnarchyMode)
			{
				SetHardcore(message.Contains("hardcore"));
			}
			else if (message.Contains("hardcore") != IsHardcore)
			{
				IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.FreePlayHardcoreDisabled, userNickName);
				startBomb = false;
			}

			if (TwitchPlaySettings.data.EnableFreeplayNeedy || UserAccess.HasAccess(userNickName, AccessLevel.Admin, true) || TwitchPlaySettings.data.AnarchyMode)
			{
				SetNeedy(message.Contains("needy"));
			}
			else if (message.Contains("needy") != HasNeedy)
			{
				IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.FreePlayNeedyDisabled, userNickName);
				startBomb = false;
			}

			if (TwitchPlaySettings.data.EnableFreeplayModsOnly || UserAccess.HasAccess(userNickName, AccessLevel.Admin, true) || TwitchPlaySettings.data.AnarchyMode)
			{
				if (message.Contains("vanilla"))
				{
					SetModsOnly(false);
				}
				else if (message.Contains("mods"))
				{
					SetModsOnly();
				}
			}
			else if (message.Contains("vanilla") || message.Contains("mods"))
			{
				IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.FreePlayModsOnlyDisabled, userNickName);
				startBomb = false;
			}
		}
		else
		{
			Match timerMatch = Regex.Match(message, "^timer? ([0-9]+)(?::([0-9]{2}))(?::([0-9]{2}))$", RegexOptions.IgnoreCase);
			if (timerMatch.Success)
			{
				changeHoursTo = timerMatch.Groups[1].Value;
				changeMinutesTo = timerMatch.Groups[2].Value;
				changeSecondsTo = timerMatch.Groups[3].Value;
			}

			timerMatch = Regex.Match(message, "^timer? ([0-9]+)(?::([0-9]{2}))?$", RegexOptions.IgnoreCase);
			if (timerMatch.Success)
			{
				changeMinutesTo = timerMatch.Groups[1].Value;
				changeSecondsTo = timerMatch.Groups[2].Value;
			}

			Match bombsMatch = Regex.Match(message, "^bombs ([0-9]+)$", RegexOptions.IgnoreCase);
			if (bombsMatch.Success)
			{
				changeBombsTo = bombsMatch.Groups[1].Value;
			}

			Match modulesMatch = Regex.Match(message, "^modules ([0-9]+)$", RegexOptions.IgnoreCase);
			if (modulesMatch.Success)
			{
				changeModulesTo = modulesMatch.Groups[1].Value;
			}
		}

		if (changeMinutesTo != string.Empty)
		{
			IEnumerator setTimerCoroutine = SetBombTimer(changeHoursTo, changeMinutesTo, changeSecondsTo);
			while (setTimerCoroutine.MoveNext())
			{
				yield return setTimerCoroutine.Current;
			}
		}
		if (changeBombsTo != string.Empty)
		{
			IEnumerator setBombsCoroutine = SetBombCount(changeBombsTo);
			while (setBombsCoroutine.MoveNext())
			{
				yield return setBombsCoroutine.Current;
			}
		}
		if (changeModulesTo != string.Empty)
		{
			IEnumerator setModulesCoroutine = SetBombModules(changeModulesTo);
			while (setModulesCoroutine.MoveNext())
			{
				yield return setModulesCoroutine.Current;
			}
		}
		if (startBomb)
		{
			StartBomb();
		}

	}
	#endregion

	#region Helper Methods
	public IEnumerator SetBombTimer(string hours, string mins, string secs)
	{
		DebugHelper.Log("Time parsing section");
		int hoursInt = 0;
		if (!string.IsNullOrEmpty(hours) && !int.TryParse(hours, out hoursInt))
		{
			yield break;
		}

		if (!int.TryParse(mins, out int minutes))
		{
			yield break;
		}

		int seconds = 0;
		if (!string.IsNullOrEmpty(secs) && 
			(!int.TryParse(secs, out seconds) || seconds >= 60) )
		{
			yield break;
		}

		int timeIndex = (hoursInt * 120) + (minutes * 2) + (seconds / 30);
		DebugHelper.Log("Freeplay time doubling section");
		//Double the available free play time. (The doubling stacks with the Multiple bombs module installed)
		float originalMaxTime = FreeplayDevice.MAX_SECONDS_TO_SOLVE;
		int maxModules = (int)_maxModuleField.GetValue(FreeplayDevice);
		int multiplier = MultipleBombs.Installed() ? (MultipleBombs.GetMaximumBombCount() * 2) - 1 : 1;
		float newMaxTime = 600f + ((maxModules - 1) * multiplier * 60);
		FreeplayDevice.MAX_SECONDS_TO_SOLVE = newMaxTime;

		DebugHelper.Log("Freeplay settings reading section");
		FreeplaySettings currentSettings = FreeplayDevice.CurrentSettings;
		float currentTime = currentSettings.Time;
		int currentTimeIndex = Mathf.FloorToInt(currentTime) / 30;
		KeypadButton button = timeIndex > currentTimeIndex ? FreeplayDevice.TimeIncrement : FreeplayDevice.TimeDecrement;
		Selectable buttonSelectable = button.GetComponent<Selectable>();

		DebugHelper.Log("Freeplay time setting section");
		for (int hitCount = 0; hitCount < Mathf.Abs(timeIndex - currentTimeIndex); ++hitCount)
		{
			currentTime = currentSettings.Time;
			SelectObject(buttonSelectable);
			yield return new WaitForSeconds(0.01f);
			if (Mathf.FloorToInt(currentTime) == Mathf.FloorToInt(currentSettings.Time))
				break;
		}

		//Restore original max time, just in case Multiple bombs module was NOT installed, to avoid false detection.
		FreeplayDevice.MAX_SECONDS_TO_SOLVE = originalMaxTime;
	}

	public IEnumerator SetBombCount(string bombs)
	{
		if (!int.TryParse(bombs, out int bombCount))
		{
			yield break;
		}

		if (!MultipleBombs.Installed())
		{
			yield break;
		}

		int currentBombCount = MultipleBombs.GetFreePlayBombCount();
		Selectable buttonSelectable = bombCount > currentBombCount ? SelectableChildren[3] : SelectableChildren[2];

		for (int hitCount = 0; hitCount < Mathf.Abs(bombCount - currentBombCount); ++hitCount)
		{
			int lastBombCount = MultipleBombs.GetFreePlayBombCount();
			SelectObject(buttonSelectable);
			yield return new WaitForSeconds(0.01f);
			if (lastBombCount == MultipleBombs.GetFreePlayBombCount())
				yield break;
		}
	}

	public IEnumerator SetBombModules(string mods)
	{
		if (!int.TryParse(mods, out int moduleCount))
		{
			yield break;
		}

		FreeplaySettings currentSettings = FreeplayDevice.CurrentSettings;
		int currentModuleCount = currentSettings.ModuleCount;
		KeypadButton button = moduleCount > currentModuleCount ? FreeplayDevice.ModuleCountIncrement : FreeplayDevice.ModuleCountDecrement;
		Selectable buttonSelectable = button.GetComponent<Selectable>();

		for (int hitCount = 0; hitCount < Mathf.Abs(moduleCount - currentModuleCount); ++hitCount)
		{
			int lastModuleCount = currentSettings.ModuleCount;
			SelectObject(buttonSelectable);
			yield return new WaitForSeconds(0.01f);
			if (lastModuleCount == currentSettings.ModuleCount)
				yield break;
		}
	}

	public void SetNeedy(bool on = true)
	{
		if (HasNeedy != on)
		{
			ToggleSwitch needyToggle = FreeplayDevice.NeedyToggle;
			SelectObject( needyToggle.GetComponent<Selectable>() );
		}
	}

	public void SetHardcore(bool on = true)
	{
		if (IsHardcore != on)
		{
			ToggleSwitch hardcoreToggle = FreeplayDevice.HardcoreToggle;
			SelectObject( hardcoreToggle.GetComponent<Selectable>() );
		}
	}

	public void SetModsOnly(bool on = true)
	{
		FreeplaySettings currentSettings = FreeplayDevice.CurrentSettings;
		bool onlyMods = currentSettings.OnlyMods;
		if (onlyMods != on)
		{
			ToggleSwitch modsToggle = FreeplayDevice.ModsOnly;
			SelectObject( modsToggle.GetComponent<Selectable>());
		}
	}

	public void StartBomb()
	{
		KeypadButton startButton = FreeplayDevice.StartButton;
		SelectObject( startButton.GetComponent<Selectable>());
	}

	public IEnumerator HoldFreeplayDevice()
	{
		FloatingHoldable.HoldStateEnum holdState = FloatingHoldable.HoldState;

		if (holdState != FloatingHoldable.HoldStateEnum.Held)
		{
			SelectObject(Selectable);

			float holdTime = FloatingHoldable.PickupTime;
			IEnumerator forceRotationCoroutine = ForceHeldRotation(holdTime);
			while (forceRotationCoroutine.MoveNext())
			{
				yield return forceRotationCoroutine.Current;
			}
		}
	}

	public IEnumerator LetGoFreeplayDevice()
	{
		FloatingHoldable.HoldStateEnum holdState = FloatingHoldable.HoldState;
		if (holdState != FloatingHoldable.HoldStateEnum.Held) yield break;
		while (FloatingHoldable.HoldState == FloatingHoldable.HoldStateEnum.Held)
		{
			DeselectObject(Selectable);
			yield return new WaitForSeconds(0.1f);
		}
	}

	private void SelectObject(Selectable selectable)
	{
		selectable.HandleSelect(true);
		SelectableManager.Select(selectable, true);
		SelectableManager.HandleInteract();
		selectable.OnInteractEnded();
	}

	private void DeselectObject(Selectable selectable)
	{
		SelectableManager.HandleCancel();
	}

	private IEnumerator ForceHeldRotation(float duration)
	{
		Transform baseTransform = SelectableManager.GetBaseHeldObjectTransform();

		float initialTime = Time.time;
		while (Time.time - initialTime < duration)
		{
			Quaternion currentRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);

			SelectableManager.SetZSpin(0.0f);
			SelectableManager.SetControlsRotation(baseTransform.rotation * currentRotation);
			SelectableManager.HandleFaceSelection();
			yield return null;
		}

		SelectableManager.SetZSpin(0.0f);
		SelectableManager.SetControlsRotation(baseTransform.rotation * Quaternion.Euler(0.0f, 0.0f, 0.0f));
		SelectableManager.HandleFaceSelection();
	}
	#endregion

	#region Readonly Fields
	public static FreeplayCommander Instance;
	public readonly FreeplayDevice FreeplayDevice = null;
	public readonly Selectable Selectable = null;
	public readonly Selectable[] SelectableChildren = null;
	public readonly FloatingHoldable FloatingHoldable = null;
	private readonly SelectableManager SelectableManager = null;
	public bool HasNeedy { get { FreeplaySettings currentSettings = FreeplayDevice.CurrentSettings; return currentSettings.HasNeedy; }}
	public bool IsHardcore { get { FreeplaySettings currentSettings = FreeplayDevice.CurrentSettings; return currentSettings.IsHardCore; }}
	#endregion

	#region Private Static Fields
	private static FieldInfo _maxModuleField = null;
	#endregion
}
