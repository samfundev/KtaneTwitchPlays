using System.Collections;

public class AlienFilingColorsComponentSolver : ReflectionComponentSolver
{
	public AlienFilingColorsComponentSolver(TwitchModule module) :
		base(module, "AlienModule", "!{0} press <p1> (p2)... [Presses the button(s) in the specified position(s)] | Valid positions are 1-8 in reading order")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length < 2 || !command.StartsWith("press ")) yield break;
		for (int i = 1; i < split.Length; i++)
		{
			if (!split[i].EqualsAny("1", "2", "3", "4", "5", "6", "7", "8")) yield break;
		}

		yield return null;
		for (int i = 1; i < split.Length; i++)
			yield return Click(int.Parse(split[i]) - 1);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		int start = _component.GetValue<int>("_currentSequenceIndex");
		int[] correct = _component.GetValue<int[]>("_correctCombination");
		for (int i = start; i < 8; i++)
		{
			yield return Click(correct[i]);
		}
	}
}