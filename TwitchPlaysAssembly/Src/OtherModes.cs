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

	public static string GetName(TwitchPlaysMode mode) => Enum.GetName(typeof(TwitchPlaysMode), mode);

	public static bool InMode(TwitchPlaysMode mode) => currentMode == mode;

	private static KMGameInfo.State _state = KMGameInfo.State.Transitioning;

	public static bool Set(TwitchPlaysMode mode, bool state = true)
	{
		if (!state) mode = TwitchPlaysMode.Normal;

		nextMode = mode;
		if (_state != KMGameInfo.State.PostGame && _state != KMGameInfo.State.Setup) return false;

		currentMode = mode;
		return true;
	}

	public static void Toggle(TwitchPlaysMode mode) => Set(mode, nextMode != mode);

	public static bool TimeModeOn { get => InMode(TwitchPlaysMode.Time); set => Set(TwitchPlaysMode.Time, value); }
	public static bool VSModeOn { get => InMode(TwitchPlaysMode.VS); set => Set(TwitchPlaysMode.VS, value); }
	public static bool ZenModeOn { get => InMode(TwitchPlaysMode.Zen); set => Set(TwitchPlaysMode.Zen, value); }

	public static float timedMultiplier = 9;
	public static int teamHealth = 0;
	public static int bossHealth = 0;

	public static int GetTeamHealth() => teamHealth;

	public static int GetBossHealth() => bossHealth;

	public static int SubtractBossHealth(int damage)
	{
		bossHealth -= damage;
		return bossHealth;
	}

	public static int SubtractTeamHealth(int damage)
	{
		teamHealth -= damage;
		return teamHealth;
	}
	public static void RefreshModes(KMGameInfo.State state)
	{
		_state = state;

		if ((_state != KMGameInfo.State.PostGame && _state != KMGameInfo.State.Setup) || currentMode == nextMode) return;

		currentMode = nextMode;
		IRCConnection.SendMessage("Mode is now set to: {0}", Enum.GetName(typeof(TwitchPlaysMode), currentMode));
	}

	public static float GetMultiplier() => timedMultiplier;

	public static float GetAdjustedMultiplier() => Math.Min(timedMultiplier, TwitchPlaySettings.data.TimeModeMaxMultiplier);

	public static bool DropMultiplier()
	{
		if (timedMultiplier > (TwitchPlaySettings.data.TimeModeMinMultiplier + TwitchPlaySettings.data.TimeModeMultiplierStrikePenalty))
		{
			timedMultiplier -= TwitchPlaySettings.data.TimeModeMultiplierStrikePenalty;
			return true;
		}
		else
		{
			timedMultiplier = TwitchPlaySettings.data.TimeModeMinMultiplier;
			return false;
		}
	}

	public static void SetMultiplier(float newMultiplier) => timedMultiplier = newMultiplier;
}
