using System;

public enum TwitchPlaysMode
{
	Normal,
	Time,
	VS,
	Zen
}

public static class OtherModes
{
	public static TwitchPlaysMode currentMode = TwitchPlaysMode.Normal;
	public static TwitchPlaysMode nextMode = TwitchPlaysMode.Normal;

	public static string GetName(TwitchPlaysMode mode) { return Enum.GetName(typeof(TwitchPlaysMode), mode); }

	public static bool InMode(TwitchPlaysMode mode) { return currentMode == mode; }

	public static bool Set(TwitchPlaysMode mode, bool state = true)
	{
		if (state == false) mode = TwitchPlaysMode.Normal;

		nextMode = mode;
		if (!BombMessageResponder.BombActive)
		{
			currentMode = mode;
			return true;
		}
		return false;
	}

	public static void Toggle(TwitchPlaysMode mode)
	{
		Set(mode, nextMode != mode);
	}

	public static bool TimeModeOn { get { return InMode(TwitchPlaysMode.Time); } set { Set(TwitchPlaysMode.Time, value); } }
	public static bool VSModeOn { get { return InMode(TwitchPlaysMode.VS); } set { Set(TwitchPlaysMode.VS, value); } }
	public static bool ZenModeOn { get { return InMode(TwitchPlaysMode.Zen); } set { Set(TwitchPlaysMode.Zen, value); } }

	public static float timedMultiplier = 9;
	public static int teamHealth = 0;
	public static int bossHealth = 0;

	public static int GetTeamHealth()
	{
		return teamHealth;
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
	public static void RefreshModes()
	{
		if (!BombMessageResponder.BombActive && currentMode != nextMode)
		{
			currentMode = nextMode;
			IRCConnection.Instance.SendMessage("Mode is now set to: {0}", Enum.GetName(typeof(TwitchPlaysMode), currentMode));
		}
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
