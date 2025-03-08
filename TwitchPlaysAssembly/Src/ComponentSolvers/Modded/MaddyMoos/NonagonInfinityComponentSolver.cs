using System;
using System.Collections;
using System.Linq;

[ModuleID("nonagonInfinity")]
public class NonagonInfinityComponentSolver : CommandComponentSolver
{
	public NonagonInfinityComponentSolver(TwitchModule module) :
		base(module, "NonagonInfinity", "!{0} press <l1> <l2> [Presses the button with label 'l1' then 'l2']")
	{
	}

	[Command("press ([a-z]) ([a-z])")]
	private IEnumerator Press(string l1, string l2)
	{
		l1 = l1.ToUpperInvariant();
		l2 = l2.ToUpperInvariant();
		string[] buttonlabels = _component.GetValue<string[]>("buttonlabels");
		if (!buttonlabels.Contains(l1) || !buttonlabels.Contains(l2))
			yield break;

		yield return null;
		yield return Click(Array.IndexOf(buttonlabels, l1));
		yield return Click(Array.IndexOf(buttonlabels, l2));
		if (_component.GetValue<string>("input") == _component.GetValue<string>("solve"))
			yield return "solve";
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		string input = _component.GetValue<string>("input");
		string solve = _component.GetValue<string>("solve");
		string[] buttonlabels = _component.GetValue<string[]>("buttonlabels");
		if (input.Length == 1 && input[0] != solve[0])
			yield break;
		for (int i = input.Length; i < 2; i++)
			yield return Click(Array.IndexOf(buttonlabels, solve[i].ToString()));
		while (!Module.BombComponent.IsSolved) yield return true;
	}
}