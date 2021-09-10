using System;
using System.Collections;

public class StrikeSolveComponentSolver : ReflectionComponentSolver
{
	public StrikeSolveComponentSolver(TwitchModule module) :
		base(module, "strikeSolveScript", "!{0} press <strike/solve> [Presses the strike or solve button]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !command.StartsWith("press ")) yield break;
		if (!split[1].EqualsAny("strike", "solve")) yield break;

		yield return null;
		string[] btns = new string[] { "solve", "strike" };
		yield return Click(Array.IndexOf(btns, split[1]), 0);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		string[] btns = new string[] { "Solve", "Strike" };
		string correctButton = _component.GetValue<string>("buttonToPress");
		yield return Click(correctButton == "any" ? UnityEngine.Random.Range(0, 2) : Array.IndexOf(btns, correctButton));
	}
}