using System.Collections;

public class PinkArrowsComponentSolver : ReflectionComponentSolver
{
	public PinkArrowsComponentSolver(TwitchModule module) :
		base(module, "pinkArrowsScript", "!{0} up/right/down/left [Presses the specified arrow button] | Words can be substituted as one letter (Ex. right as r)")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (!command.EqualsAny("up", "u", "down", "d", "left", "l", "right", "r")) yield break;

		yield return null;
		const string positionsAbrev = "udlr";
		int index = positionsAbrev.IndexOf(command[0]);
		yield return Click(index, 0);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		int start = _component.GetValue<int>("pressNumber");
		for (int i = start; i < 7; i++)
			yield return Click(buttonPositions[_component.GetValue<int>("correctButton")]);
	}

	private readonly int[] buttonPositions = { 0, 3, 1, 2 };
}