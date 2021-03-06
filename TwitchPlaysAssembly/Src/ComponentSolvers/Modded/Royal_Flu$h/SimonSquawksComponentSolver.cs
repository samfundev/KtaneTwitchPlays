﻿using System.Collections;
using System.Linq;
using UnityEngine;

public class SimonSquawksComponentSolver : ReflectionComponentSolver
{
	public SimonSquawksComponentSolver(TwitchModule module) :
		base(module, "SimonSquawksScript", "!{0} press <colour> <colour> [Presses the buttons with the specified colours] | Valid colours are blue, cyan, green, orange, purple, red, white, and yellow")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 3 || !command.StartsWith("press ")) yield break;
		string[] colours = _component.GetValue<string[]>("colourNameOptions");
		if (!colours.Contains(split[1]) || !colours.Contains(split[2])) yield break;
		if (!_component.GetValue<bool>("active"))
		{
			yield return "sendtochaterror You can't interact with the module right now.";
			yield break;
		}

		yield return null;
		object[] buttons = _component.GetValue<object[]>("buttons");
		string[] btnColours = new string[] { split[1], split[2] };
		for (int i = 0; i < 2; i++)
		{
			for (int j = 0; j < buttons.Length; j++)
			{
				if (buttons[j].GetValue<string>("lightColour") == btnColours[i])
				{
					buttons[j].GetValue<KMSelectable>("selectable").OnInteract();
					yield return new WaitForSeconds(0.1f);
					break;
				}
			}
		}
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

			while (!_component.GetValue<bool>("active")) { yield return null; }
			yield return RespondToCommandInternal("press " + _component.GetValue<string[]>("solutionLog")[0] + " " + _component.GetValue<string[]>("solutionLog")[1]);
		}
	}
}