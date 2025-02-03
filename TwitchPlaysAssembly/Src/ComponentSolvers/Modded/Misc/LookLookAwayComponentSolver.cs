using System;
using System.Collections;
using System.Linq;
using UnityEngine;

[ModuleID("lookLookAway")]
public class LookLookAwayComponentSolver : ReflectionComponentSolver
{
	public LookLookAwayComponentSolver(TwitchModule module) :
		base(module, "lookLookAwayScript", "!{0} submit <U/UR/R/DR/D/DL/L/UL> [Highlights the module and then unhighlights the module when the specified direction is displayed] | !{0} toggle [Presses the toggle button] | Direction submissions can be chained using spaces, commas, or semicolons")
	{
		ModuleSelectable = _component.GetValue<KMSelectable>("ModulePlate");
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (command.StartsWith("submit "))
		{
			string[] dirs = { "u", "ur", "r", "dr", "d", "dl", "l", "ul" };
			for (int i = 1; i < split.Length; i++)
			{
				if (!dirs.Contains(split[i])) yield break;
			}

			yield return null;
			for (int i = 1; i < split.Length; i++)
			{
				DoInteractionHighlight(ModuleSelectable);
				yield return new WaitForSeconds(.05f);
				int target = Array.IndexOf(dirs, split[i]);
				while (_component.GetValue<int>("_currentDirection") != target) yield return null;
				DoInteractionEnd(ModuleSelectable);
				yield return new WaitForSeconds(.05f);
				if (_component.GetValue<bool>("moduleSolved"))
				{
					yield return "solve";
					break;
				}
			}
		}
		else if (command.Equals("toggle"))
		{
			yield return null;
			yield return Click(0, 0);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		IList current = _component.GetValue<IList>("_SubmissionSequence");
		IList answer = _component.GetValue<IList>("_correctDirection");
		int inputs = current.Count;
		for (int i = 0; i < inputs; i++)
		{
			if (current[i] != answer[i])
			{
				yield return Click(0);
				yield return Click(0);
				inputs = 0;
				break;
			}
		}
		for (int i = inputs; i < 8; i++)
		{
			DoInteractionHighlight(ModuleSelectable);
			yield return new WaitForSeconds(.05f);
			while (_component.GetValue<int>("_currentDirection") != (int) answer[i]) yield return true;
			DoInteractionEnd(ModuleSelectable);
			yield return new WaitForSeconds(.05f);
		}
		while (!_component.GetValue<TextMesh>("ScreenArrow").text.Equals("<>")) yield return true;
	}

	private readonly KMSelectable ModuleSelectable;
}