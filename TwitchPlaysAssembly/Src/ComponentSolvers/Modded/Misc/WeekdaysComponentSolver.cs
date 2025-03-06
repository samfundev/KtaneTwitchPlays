using System;
using System.Collections;
using System.Linq;

[ModuleID("weekDays")]
public class WeekdaysComponentSolver : ReflectionComponentSolver
{
	public WeekdaysComponentSolver(TwitchModule module) :
		base(module, "WeekdaysModule", "!{0} press <weekday> [Presses the button with specified weekday] | Valid weekdays are (mon)day, (tues)day, (wednes)day, (thurs)day, (fri)day, (satur)day, and (sun)day")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !command.StartsWith("press ")) yield break;
		string[] btnLabels = { "mon", "tues", "wednes", "thurs", "fri", "satur", "sun" };
		var day = split[1].Replace("day", "");
		if (!btnLabels.Contains(day)) yield break;

		yield return null;
		yield return Click(Array.IndexOf(btnLabels, day), 0);
	}
}