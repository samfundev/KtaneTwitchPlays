using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class OtherModes
{
    public static bool timedModeOn = false;
    public static float timedMultiplier = 9;

    public static void toggleTimedMode()
    {
        timedModeOn = !timedModeOn;
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
        if (timedMultiplier > 2.5)
        {
            timedMultiplier = timedMultiplier - 1.5f;
            return true;
        }
        else
        {
            timedMultiplier = 1;
            return false;
        }
    }

    public static void setMultiplier(float newMultiplier)
    {
        timedMultiplier = newMultiplier;
    }
}
