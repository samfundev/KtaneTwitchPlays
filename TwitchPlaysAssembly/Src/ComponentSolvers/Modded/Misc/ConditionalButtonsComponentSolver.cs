using System;
using System.Collections;
using System.Collections.Generic;

[ModuleID("conditionalButtons")]
public class ConditionalButtonsComponentSolver : ReflectionComponentSolver
{
	public ConditionalButtonsComponentSolver(TwitchModule module) :
		base(module, "conditionalButtons", "!{0} press <p1> (p2)... [Presses the button(s) in the specified position(s)] | Valid positions are tl, tm, tr, bl, bm, br, or 1-6 in reading order")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length < 2 || !command.StartsWith("press ")) yield break;
		for (int i = 1; i < split.Length; i++)
		{
			if (!split[i].EqualsAny("tl", "tm", "tr", "bl", "bm", "br", "1", "2", "3", "4", "5", "6")) yield break;
		}

		yield return null;
		string[] positions = new string[] { "tl", "tm", "tr", "bl", "bm", "br" };
		string[] positions2 = new string[] { "1", "2", "3", "4", "5", "6" };
		for (int i = 1; i < split.Length; i++)
		{
			int index = Array.IndexOf(positions, split[i]);
			if (index == -1)
				index = Array.IndexOf(positions2, split[i]);
			yield return Click(index);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		List<KMSelectable> buttonsToPress = _component.GetValue<List<KMSelectable>>("ButtonsToPress");
		int count = buttonsToPress.Count;
		for (int i = 0; i < count; i++)
		{
			yield return Click(_component.GetValue<List<KMSelectable>>("Buttons").IndexOf(buttonsToPress[0]));
		}
	}
}