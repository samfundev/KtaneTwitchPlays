using System.Collections;
using System.Linq;
using System;
using UnityEngine;

public class StateOfAggregationComponentSolver : ReflectionComponentSolver
{
	public StateOfAggregationComponentSolver(TwitchModule module) :
		base(module, "StateOfAggregation", "!{0} cycle temp [Cycles through all temperatures in the temperature display] | !{0} submit <group> <temp> [Submits the specified chemical group and temperature]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (!command.Equals("cycle temp") && !command.StartsWith("submit")) yield break;
		if (command.StartsWith("submit"))
		{
			if (split.Length < 3) yield break;
			string[] groups = new string[_component.GetValue<string[]>("groups").Length];
			for (int i = 0; i < groups.Length; i++)
				groups[i] = _component.GetValue<string[]>("groups")[i].Replace(" ", "").ToLower();
			string group = "";
			for (int i = 1; i < split.Length - 1; i++)
				group += split[i];
			if (!groups.Contains(group)) yield break;

			string[] temps = new string[_component.GetValue<string[]>("displayedTemps").Length];
			for (int i = 0; i < temps.Length; i++)
				temps[i] = _component.GetValue<string[]>("displayedTemps")[i].ToLower();
			if (!temps.Contains(split[split.Length - 1])) yield break;

			yield return null;
			int current = _component.GetValue<int>("groupCounter");
			yield return SelectIndex(current, Array.IndexOf(groups, group), groups.Length, selectables[1], selectables[0]);

			current = _component.GetValue<int>("tempCounter");
			yield return SelectIndex(current, Array.IndexOf(temps, split[split.Length - 1]), temps.Length, selectables[2], selectables[3]);

			yield return Click(4, 0);
		}
		else if (command.Equals("cycle temp"))
		{
			yield return null;
			string[] displayedTemps =  _component.GetValue<string[]>("displayedTemps");
			for (int i = 0; i < displayedTemps.Length; i++)
			{
				yield return "trycancel";
				yield return new WaitForSeconds(2f);
				yield return Click(3, 0);
			}
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		yield return RespondToCommandInternal($"submit {_component.GetValue<string[]>("groups")[_component.GetValue<int>("correctGroup")]} {_component.GetValue<string[]>("displayedTemps")[_component.GetValue<int>("correctTempIndex")]}");
	}
}