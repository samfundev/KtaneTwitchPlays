using System;
using System.Collections;
using System.Linq;

[ModuleID("poisonedGoblets")]
public class PoisonedGobletsComponentSolver : ReflectionComponentSolver
{
	public PoisonedGobletsComponentSolver(TwitchModule module) :
		base(module, "PoisonedGobletsMod", "!{0} cycle [Presses the cycle button] | !{0} press <#> [Presses the specified goblet (1-6) starting from north going clockwise]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (command.Equals("cycle"))
		{
			yield return null;
			yield return Click(6, 0);
		}
		else if (command.StartsWith("press ") && split.Length == 2)
		{
			if (!int.TryParse(split[1], out int check)) yield break;
			if (check < 1 || check > 6) yield break;

			yield return null;
			yield return Click(check - 1, 0);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		int pressAmt = _component.GetValue<int>("cyclePressed");
		int corPressAmt = _component.GetValue<int>("cycleToPress");
		int[] poisonAmt = _component.GetValue<int[]>("poisonAmm");
		for (int i = pressAmt; i < corPressAmt; i++)
			yield return Click(6);
		yield return Click(Array.IndexOf(poisonAmt, poisonAmt.Min()), 0);
	}
}