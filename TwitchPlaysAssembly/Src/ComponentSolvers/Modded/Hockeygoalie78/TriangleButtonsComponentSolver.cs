using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TriangleButtonsComponentSolver : ReflectionComponentSolver
{
	public TriangleButtonsComponentSolver(TwitchModule module) :
		base(module, "TriangleButtons", "!{0} press <pos> [Presses the button in the specified position] | Valid positions are tl, tr, bl, and br")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !command.StartsWith("press ")) yield break;
		if (!buttons.Contains(split[1])) yield break;
		if (!_component.GetValue<bool>("moduleActive"))
		{
			yield return "sendtochaterror You can't interact with the module right now.";
			yield break;
		}

		yield return null;
		yield return Click(Array.IndexOf(buttons, split[1]), 0);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		var needyComponent = Module.BombComponent.GetComponent<NeedyComponent>();

		while (true)
		{
			if (needyComponent.State != NeedyComponent.NeedyStateEnum.Running)
			{
				yield return true;
				continue;
			}

			yield return Click(_component.GetValue<Dictionary<string, int>>("solutions")[_component.GetValue<string>("buttonOrientation")]);
		}
	}

	private readonly string[] buttons = { "tl", "tr", "bl", "br" };
}