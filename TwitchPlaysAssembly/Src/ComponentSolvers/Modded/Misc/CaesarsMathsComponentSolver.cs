using System.Collections;
using UnityEngine;

public class CaesarsMathsComponentSolver : ReflectionComponentSolver
{
	public CaesarsMathsComponentSolver(TwitchModule module) :
		base(module, "caesarsMathsScript", "!{0} press <pos> [Presses the button in the specified position] | Valid positions are left(l), middle(m), or right(r)")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !command.StartsWith("press ")) yield break;
		if (!split[1].EqualsAny("left", "l", "middle", "m", "right", "r")) yield break;

		yield return null;
		const string positionsAbrev = "lmr";
		int index = positionsAbrev.IndexOf(split[1][0]);
		yield return Click(index, 0);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		string correctAnswer = _component.GetValue<int>("correctAnswer").ToString();
		for (int i = 0; i < 3; i++)
		{
			if (_component.GetValue<KMSelectable[]>("myButtons")[i].GetComponentInChildren<TextMesh>().text == correctAnswer)
				yield return Click(i);
		}
	}
}