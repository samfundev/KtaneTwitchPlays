public static class OtherModes
{
    public static bool timedModeOn = false;
    public static float timedMultiplier = 9;

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

    public static void toggleTimedMode()
    {
        if (BombMessageResponder.BombActive) return;
        timedModeOn = !timedModeOn;
    }

    public static bool vsModeCheck()
    {
        return vsModeOn;
    }

    public static bool timedModeCheck()
    {
        return timedModeOn;
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
