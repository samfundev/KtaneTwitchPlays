using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using UnityEngine;

public class StateOfAggregationComponentSolver : ReflectionComponentSolver
{
	public StateOfAggregationComponentSolver(TwitchModule module) :
		base(module, "StateOfAggregation", "Use '!{0} submit <chemical group>, <temperature>' to submit your answer. You may shorten chemical groups to the first few letters as long as they can be uniquely identified. To show all temperatures in the temperature display, use '!{0} cycle temp'.")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (Regex.IsMatch(command, @"^cycle *(?:temps?)?$"))
		{
			yield return null;

			int cycleLength = _component.GetValue<string[]>("displayedTemps").Length;
			for (int i = 0; i < cycleLength; i++)
			{
				yield return "trywaitcancel 2.0 the temperature cycle was cancelled";
				yield return Click(5);
			}
		}

		Match mt;
		if ((mt = Regex.Match(command, @"^submit +([a-zA-Z ]*)[, ]+(\-?[0-9\.]+)(?:(\u00B0?[cC]|\u2103)|(\u00B0?[fF]|\u2109)|([kK]))$")).Success)
		{
			// Capture group 1: Chemical name
			string targetCG = mt.Groups[1].ToString().Replace(" ","").ToLower();
			string[] allCGs = _component.GetValue<string[]>("groups");

			List<string> matchedCGs = allCGs.Where(g => g.ToLower().Replace(" ","").StartsWith(targetCG)).ToList();
			if (matchedCGs.Count() == 0)
				yield return $"sendtochaterror!hf No chemical group matches \"{mt.Groups[1].ToString()}\".";
			else if (matchedCGs.Count() > 1)
				yield return $"sendtochaterror!hf Multiple chemical groups match \"{mt.Groups[1].ToString()}\": {matchedCGs.Join(", ")}";

			// Capture group 2: Temperature
			// Groups 3, 4, 5 detect Celsius, Farenheit, and Kelvin respectively.
			string targetTemp;
			string[] allTemps = _component.GetValue<string[]>("displayedTemps");

			if (!float.TryParse(mt.Groups[2].ToString(), out _))
				yield break; // Ignore invalid numbers, but don't actually use the float representation
			if (mt.Groups[3].Success)      targetTemp = $"{mt.Groups[2]}\u00B0C";
			else if (mt.Groups[4].Success) targetTemp = $"{mt.Groups[2]}\u00B0F";
			else if (mt.Groups[5].Success) targetTemp = $"{mt.Groups[2]}K";
			else                           yield break;

			// We're executing the command at this point -- it's an unsubmittable penalty if the temperature is not present
			yield return null;

			int currentIndex = _component.GetValue<int>("groupCounter");
			int targetIndex = Array.IndexOf(allCGs, matchedCGs[0]);
			yield return SelectIndex(currentIndex, targetIndex, allCGs.Length, selectables[2], selectables[0]);

			currentIndex = _component.GetValue<int>("tempCounter");
			targetIndex = Array.IndexOf(allTemps, targetTemp);
			if (targetIndex == -1) // Not on the module
			{
				for (int i = 0; i < allTemps.Length; ++i)
					yield return Click(5);
				yield return "unsubmittablepenalty";
				yield break;
			}
			yield return SelectIndex(currentIndex, targetIndex, allTemps.Length, selectables[5], selectables[3]);
			yield return Click(7);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		yield return RespondToCommandInternal($"submit {_component.GetValue<string[]>("groups")[_component.GetValue<int>("correctGroup")]}, {_component.GetValue<string[]>("displayedTemps")[_component.GetValue<int>("correctTempIndex")]}");
	}
}