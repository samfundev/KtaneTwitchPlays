using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BridgeComponentSolver : ReflectionComponentSolver
{
	public BridgeComponentSolver(TwitchModule module) :
		base(module, "Bridge", "!{0} bid <lvl><deno> [Makes a bid with the specified level and denominator] | !{0} pass/nt [Presses the Pass or NT button]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (command.Equals("pass"))
		{
			yield return null;
			yield return Click(7, 0);
		}
		else if (command.Equals("nt"))
		{
			yield return null;
			yield return Click(14, 0);
		}
		else if (command.StartsWith("bid ") && split.Length == 2)
		{
			if (split[1].Length < 2 || split[1].Length > 3) yield break;
			if (!int.TryParse(split[1][0].ToString(), out int check)) yield break;
			if (!check.InRange(1, 7)) yield break;
			string[] validDenos = { "c", "d", "h", "s", "nt" };
			if (!validDenos.Contains(split[1].Substring(1))) yield break;

			yield return null;
			yield return Click(check - 1);
			yield return Click(Array.IndexOf(validDenos, split[1].Substring(1)) + 10, 0);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		while (!_component.GetValue<bool>("_lightsOn")) yield return true;
		List<int> targetBtns = new List<int>();
		for (int i = 0; i < _component.GetValue<List<int>>("targetButtonSequence").Count; i++)
			targetBtns.Add(_component.GetValue<List<int>>("targetButtonSequence")[i]);
		for (int i = 0; i < targetBtns.Count; i++)
			yield return Click(targetBtns[i] <= 7 ? targetBtns[i] : targetBtns[i] + 2);
	}
}