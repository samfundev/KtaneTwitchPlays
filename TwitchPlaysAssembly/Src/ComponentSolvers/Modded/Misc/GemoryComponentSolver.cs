using System;
using System.Collections;

public class GemoryComponentSolver : ReflectionComponentSolver
{
	public GemoryComponentSolver(TwitchModule module) :
		base(module, "Gemory", "!{0} press udlrs [Presses the up, down, left, and right directions and the status light] | !{0} select [Selects the module]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (command.StartsWith("press ") && !_component.GetValue<bool>("Solved"))
		{
			if (split.Length != 2) yield break;
			for (int i = 0; i < split[1].Length; i++)
			{
				if (!split[1][i].EqualsAny('u', 'r', 'd', 'l', 's')) yield break;
			}

			yield return null;
			char[] btns = new char[] { 'u', 'r', 'd', 'l', 's' };
			for (int i = 0; i < split[1].Length; i++)
			{
				yield return Click(Array.IndexOf(btns, split[1][i]));
			}
		}
		else if (command.Equals("select") && _component.GetValue<bool>("Solved"))
		{
			yield return null;
			yield return "solve";
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		while (_component.GetValue<bool>("dummy")) yield return null;
		if (_component.GetValue<bool>("Solved"))
		{
			Module.BombComponent.gameObject.GetComponent<KMSelectable>().OnInteract();
		}
		else
		{
			Module.BombComponent.StartCoroutine(HandleSolve());
			while (!Module.BombComponent.IsSolved)
				yield return true;
		}
	}

	IEnumerator HandleSolve()
	{
		inputAgain:
		string input = _component.GetValue<string>("input");
		string ans = _component.GetValue<string>("owolist");
		for (int i = input.Length; i < ans.Length; i++)
		{
			yield return Click(int.Parse(ans[i].ToString()));
			if (_component.GetValue<bool>("Solved"))
			{
				Module.BombComponent.gameObject.GetComponent<KMSelectable>().OnInteract();
				yield break;
			}
		}
		while (!_component.GetValue<bool>("sequencing"))
		{
			yield return null;
			if (_component.GetValue<bool>("Solved"))
			{
				Module.BombComponent.gameObject.GetComponent<KMSelectable>().OnInteract();
				yield break;
			}
		}
		goto inputAgain;
	}
}