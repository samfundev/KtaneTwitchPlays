using System;
using System.Collections;
using System.Linq;

[ModuleID("yetAnotherKeypad")]
public class YetAnotherKeypadComponentSolver : ReflectionComponentSolver
{
	public YetAnotherKeypadComponentSolver(TwitchModule module) :
		base(module, "PuzzleMod", "!{0} press <coord1> (coord2)... [Presses the button(s) at the specified coordinate(s)] | Valid coordinates are A1-E5 with letters as column and numbers as row")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (!command.StartsWith("press ")) yield break;
		for (int i = 1; i < split.Length; i++)
		{
			if (!coordinates.Contains(split[i]))
				yield break;
		}

		yield return null;
		for (int i = 1; i < split.Length; i++)
			yield return Click(Array.IndexOf(coordinates, split[i]));
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		bool[,] isOn = _component.GetValue<bool[,]>("isOn");
		bool[,] canBeOn = _component.GetValue<bool[,]>("canBeOn");
		for (int i = 0; i < 5; i++)
			for (int j = 0; j < 5; j++)
				if (isOn[i, j] != canBeOn[i, j])
					yield return Click(i * 5 + j);
	}

	private readonly string[] coordinates = { "a1", "b1", "c1", "d1", "e1", "a2", "b2", "c2", "d2", "e2", "a3", "b3", "c3", "d3", "e3", "a4", "b4", "c4", "d4", "e4", "a5", "b5", "c5", "d5", "e5" };
}