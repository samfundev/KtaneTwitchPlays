using System.Collections;
using UnityEngine;

public class BinaryButtonsComponentSolver : ReflectionComponentSolver
{
	public BinaryButtonsComponentSolver(TwitchModule module) :
		base(module, "BinaryButtons", "!{0} submit <#####> [Submits the specified 5-digit binary number]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !command.StartsWith("submit ")) yield break;
		if (split[1].Length != 5) yield break;
		for (int i = 0; i < 5; i++)
		{
			if (split[1][i] != '0' && split[1][i] != '1')
				yield break;
		}

		yield return null;
		TextMesh[] texts = _component.GetValue<TextMesh[]>("Texts");
		for (int i = 0; i < 5; i++)
		{
			if (split[1][i].ToString() != texts[i].text)
				yield return Click(i);
		}
		yield return Click(7, 0);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		int[] cur = _component.GetValue<int[]>("yourpresses");
		int[] ans = _component.GetValue<int[]>("correctpresses");
		for (int i = 0; i < 5; i++)
		{
			if (ans[i] != cur[i])
				yield return Click(i);
		}
		yield return Click(7, 0);
	}
}