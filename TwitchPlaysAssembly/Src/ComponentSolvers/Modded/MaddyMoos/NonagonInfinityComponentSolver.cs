using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;

[ModuleID("nonagonInfinity")]
public class NonagonInfinityComponentSolver : CommandComponentSolver
{
	public NonagonInfinityComponentSolver(TwitchModule module) :
		base(module, "NonagonInfinity", "!{0} press <l1> <l2> [Presses the button with label 'l1' then 'l2']")
	{
	}

	private IEnumerator Press(CommandParser _)
	{
		_.Literal("press");
		_.Regex("([a-z]) ([a-z])", out Match match);

		string l1 = match.Groups[1].Value.ToUpperInvariant();
		string l2 = match.Groups[2].Value.ToUpperInvariant();
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