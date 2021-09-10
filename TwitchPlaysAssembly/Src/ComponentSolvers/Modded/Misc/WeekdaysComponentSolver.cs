using System;
using System.Collections;

public class WeekdaysComponentSolver : ReflectionComponentSolver
{
	public WeekdaysComponentSolver(TwitchModule module) :
		base(module, "WeekdaysModule", "!{0} press <weekday> [Presses the button with specified weekday] | Valid weekdays are (mon)day, (tues)day, (wednes)day, (thurs)day, (fri)day, (satur)day, and (sun)day")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !command.StartsWith("press ")) yield break;
		if (!split[1].EqualsAny("monday", "mon", "tuesday", "tues", "wednesday", "wednes", "thursday", "thurs", "friday", "fri", "saturday", "satur", "sunday", "sun")) yield break;

		yield return null;
		string[] btnLabels = { "mon", "tues", "wednes", "thurs", "fri", "satur", "sun" };
		yield return Click(Array.IndexOf(btnLabels, split[1].Replace("day", "")), 0);
	}
}