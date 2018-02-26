public static class OtherModes
{
	public static bool zenModeOn
	{
		get
		{
			if (BombMessageResponder.BombActive)
			{
				return _zenModeCurrentBomb;
			}
			if (_zenModeCurrentBomb != _zenModeNextBomb)
				IRCConnection.Instance.SendMessage("Zen mode is now {0}", _zenModeNextBomb ? "Enabled" : "Disabled");
			_zenModeCurrentBomb = _zenModeNextBomb;
			return _zenModeCurrentBomb;
		}
		set
		{
			_zenModeNextBomb = value;
			if (!BombMessageResponder.BombActive)
			{
				_zenModeCurrentBomb = value;
				IRCConnection.Instance.SendMessage(value ? "Zen Mode Enabled" : "Zen Mode Disabled");
			}
			else
			{
				if (value != _zenModeCurrentBomb)
					IRCConnection.Instance.SendMessage("Zen mode is currently {0}, it will be {1} for next bomb.", _zenModeCurrentBomb ? "enabled" : "disabled", value ? "enabled" : "disabled");
				else
					IRCConnection.Instance.SendMessage("Zen mode is currently {0}, it will remain {0} for next bomb.", value ? "enabled" : "disabled");
			}
		}
	}
	private static bool _zenModeCurrentBomb;
	private static bool _zenModeNextBomb;


	public static bool timedModeOn
	{
		get
		{
			if (BombMessageResponder.BombActive)
			{
				return _timedModeCurrentBomb;
			}
			if (_timedModeCurrentBomb != _timedModeNextBomb)
				IRCConnection.Instance.SendMessage("Time mode is now {0}", _timedModeNextBomb ? "Enabled" : "Disabled");
			_timedModeCurrentBomb = _timedModeNextBomb;
			return _timedModeCurrentBomb;
		}
		set
		{
			_timedModeNextBomb = value;
			if (!BombMessageResponder.BombActive)
			{
				_timedModeCurrentBomb = value;
				IRCConnection.Instance.SendMessage(value ? "Time Mode Enabled" : "Time Mode Disabled");
			}
			else
			{
				if (value != _timedModeCurrentBomb)
					IRCConnection.Instance.SendMessage("Time mode is currently {0}, it will be {1} for next bomb.", _timedModeCurrentBomb ? "enabled" : "disabled", value ? "enabled" : "disabled");
				else
					IRCConnection.Instance.SendMessage("Time mode is currently {0}, it will remain {0} for next bomb.", value ? "enabled" : "disabled");
			}
		}
	}
    public static float timedMultiplier = 9;

	private static bool _timedModeCurrentBomb = false;
	private static bool _timedModeNextBomb = false;

    public static bool vsModeOn = false;
    public static int teamHealth = 0;
    public static int bossHealth = 0;

    public static void toggleVsMode()
    {
        if (BombMessageResponder.BombActive) return;
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

	public static void RefreshTimeMode()
	{
		if (BombMessageResponder.BombActive || timedModeOn) return;
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
