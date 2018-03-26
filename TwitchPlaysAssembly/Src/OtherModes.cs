using System;

public static class OtherModes
{
	private static bool GetMode(string modeName, ref bool currentBomb, ref bool nextBomb)
	{
		if (BombMessageResponder.BombActive)
		{
			return currentBomb;
		}
		if(currentBomb != nextBomb)
			IRCConnection.Instance.SendMessage($"{modeName} is now {(nextBomb ? "Enabled" : "Disabled")}");
		currentBomb = nextBomb;
		return currentBomb;
	}

	private static void SetMode(string modeName, ref bool currentBomb, out bool nextBomb, bool value)
	{
		nextBomb = value;
		if (!BombMessageResponder.BombActive)
		{
			if(currentBomb != value)
				IRCConnection.Instance.SendMessage(value ? $"{modeName} Enabled" : $"{modeName} Disabled");
			currentBomb = value;
		}
		else
		{
			if (value != currentBomb)
				IRCConnection.Instance.SendMessage($"{modeName} is currently {{0}}, it will be {{1}} for next bomb.", currentBomb ? "enabled" : "disabled", value ? "enabled" : "disabled");
			else
				IRCConnection.Instance.SendMessage($"{modeName} is currently {{0}}, it will remain {{0}} for next bomb.", value ? "enabled" : "disabled");
		}
	}

	public static bool ZenModeOn
	{
		get => GetMode("Zen mode", ref _zenModeCurrentBomb, ref _zenModeNextBomb);
		set
		{
			SetMode("Zen mode", ref _zenModeCurrentBomb, out _zenModeNextBomb, value);
			if (!value) return;
			TimedModeOn = false;
			VsModeOn = false;
		}
	}
	private static bool _zenModeCurrentBomb;
	private static bool _zenModeNextBomb;


	public static bool TimedModeOn
	{
		get => GetMode("Time mode", ref _timedModeCurrentBomb, ref _timedModeNextBomb);
		set
		{
			SetMode("Time mode", ref _timedModeCurrentBomb, out _timedModeNextBomb, value);
			if (!value) return;
			VsModeOn = false;
			ZenModeOn = false;
		}
	}
	private static bool _timedModeCurrentBomb = false;
	private static bool _timedModeNextBomb = false;

	public static bool VsModeOn
	{
		get => GetMode("VS mode", ref _vsModeCurrentBomb, ref _vsModeNextBomb);
		set
		{
			SetMode("VS mode", ref _vsModeCurrentBomb, out _vsModeNextBomb, value);
			if (!value) return;
			VsModeOn = false;
			ZenModeOn = false;
		}
	}
	private static bool _vsModeCurrentBomb = false;
	private static bool _vsModeNextBomb = false;

	public static float timedMultiplier = 9;
	public static int teamHealth = 0;
	public static int bossHealth = 0;

	public static void ToggleVsMode()
	{
		VsModeOn = !VsModeOn;
	}

	public static int GetTeamHealth()
	{
		return teamHealth;
	}

	public static bool GetZenModeCurrent()
	{
		return _zenModeCurrentBomb;
	}

	public static bool GetZenModeNext()
	{
		if (BombMessageResponder.BombActive && _zenModeCurrentBomb != _zenModeNextBomb)
		{
			return true;
		} else
		{
			return false;
		}
	}

	public static bool GetTimeModeCurrent()
	{
		return _timedModeCurrentBomb;
	}

	public static bool GetTimeModeNext()
	{
		if (BombMessageResponder.BombActive && _timedModeCurrentBomb != _timedModeNextBomb)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public static bool GetVSModeCurrent()
	{
		return _vsModeCurrentBomb;
	}

	public static bool GetVSModeNext()
	{
		if (BombMessageResponder.BombActive && _vsModeCurrentBomb != _vsModeNextBomb)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public static int GetBossHealth()
	{
		return bossHealth;
	}

	public static int SubtractBossHealth(int damage)
	{
		bossHealth = bossHealth - damage;
		return bossHealth;
	}

	public static int SubtractTeamHealth(int damage)
	{
		teamHealth = teamHealth - damage;
		return teamHealth;
	}

	public static void ToggleZenMode()
	{
		ZenModeOn = !_zenModeNextBomb;
	}

	public static void ToggleTimedMode()
	{
		TimedModeOn = !_timedModeNextBomb;
	}

	public static void RefreshModes()
	{
		bool result = BombMessageResponder.BombActive;
		result |= TimedModeOn;
		result |= ZenModeOn;
		result |= VsModeOn;
		if (result) return;
	}

	public static float GetMultiplier()
	{
		return timedMultiplier;
	}

	public static float GetAdjustedMultiplier()
	{
		return Math.Min(timedMultiplier, TwitchPlaySettings.data.TimeModeMaxMultiplier);
	}

	public static bool DropMultiplier()
	{
		if (timedMultiplier > (TwitchPlaySettings.data.TimeModeMinMultiplier + TwitchPlaySettings.data.TimeModeMultiplierStrikePenalty))
		{
			timedMultiplier = timedMultiplier - TwitchPlaySettings.data.TimeModeMultiplierStrikePenalty;
			return true;
		}
		else
		{
			timedMultiplier = TwitchPlaySettings.data.TimeModeMinMultiplier;
			return false;
		}
	}

	public static void SetMultiplier(float newMultiplier)
	{
		timedMultiplier = newMultiplier;
	}
}
