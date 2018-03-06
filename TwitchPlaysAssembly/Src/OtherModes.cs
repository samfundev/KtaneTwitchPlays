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

	public static bool zenModeOn
	{
		get => GetMode("Zen mode", ref _zenModeCurrentBomb, ref _zenModeNextBomb);
		set
		{
			SetMode("Zen mode", ref _zenModeCurrentBomb, out _zenModeNextBomb, value);
			if (!value) return;
			timedModeOn = false;
			vsModeOn = false;
		}
	}
	private static bool _zenModeCurrentBomb;
	private static bool _zenModeNextBomb;


	public static bool timedModeOn
	{
		get => GetMode("Time mode", ref _timedModeCurrentBomb, ref _timedModeNextBomb);
		set
		{
			SetMode("Time mode", ref _timedModeCurrentBomb, out _timedModeNextBomb, value);
			if (!value) return;
			vsModeOn = false;
			zenModeOn = false;
		}
	}
	private static bool _timedModeCurrentBomb = false;
	private static bool _timedModeNextBomb = false;

	public static bool vsModeOn
	{
		get => GetMode("VS mode", ref _vsModeCurrentBomb, ref _vsModeNextBomb);
		set
		{
			SetMode("VS mode", ref _vsModeCurrentBomb, out _vsModeNextBomb, value);
			if (!value) return;
			vsModeOn = false;
			zenModeOn = false;
		}
	}
	private static bool _vsModeCurrentBomb = false;
	private static bool _vsModeNextBomb = false;

	public static float timedMultiplier = 9;
    public static int teamHealth = 0;
    public static int bossHealth = 0;

    public static void toggleVsMode()
    {
        vsModeOn = !vsModeOn;
    }

    public static int getTeamHealth()
    {
        return teamHealth;
    }

    public static int getBossHealth()
    {
        return bossHealth;
    }

    public static int subtractBossHealth(int damage)
    {
        bossHealth = bossHealth - damage;
        return bossHealth;
    }

    public static int subtractTeamHealth(int damage)
    {
        teamHealth = teamHealth - damage;
        return teamHealth;
    }

	public static void toggleZenMode()
	{
		zenModeOn = !_zenModeNextBomb;
	}

    public static void toggleTimedMode()
    {
	    timedModeOn = !_timedModeNextBomb;
	}

    public static bool vsModeCheck()
    {
        return vsModeOn;
    }

	public static void RefreshModes()
	{
		bool result = BombMessageResponder.BombActive;
		result |= timedModeOn;
		result |= zenModeOn;
		result |= vsModeOn;
		if (result) return;
	}

    public static float getMultiplier()
    {
        return timedMultiplier;
    }

    public static bool dropMultiplier()
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

    public static void setMultiplier(float newMultiplier)
    {
        timedMultiplier = newMultiplier;
    }
}
