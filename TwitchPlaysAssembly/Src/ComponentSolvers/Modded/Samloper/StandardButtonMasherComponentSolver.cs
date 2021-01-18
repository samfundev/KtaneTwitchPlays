using System.Collections;

public class StandardButtonMasherComponentSolver : ReflectionComponentSolver
{
	public StandardButtonMasherComponentSolver(TwitchModule module) :
		base(module, "standardButtonMasher", "!{0} submit <##> [Presses the push button '##' times and presses submit]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !command.StartsWith("submit ")) yield break;
		if (!int.TryParse(split[1], out int check)) yield break;
		if (check < 0 || check > 99) yield break;

		yield return null;
		for (int i = 0; i < check; i++)
			yield return Click(0);
		yield return Click(1, 0);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		int current = _component.GetValue<int>("number");
		int goal = _component.GetValue<int>("correctSubmit");
		if (current > goal)
			yield break;
		while (current < goal)
		{
			yield return Click(0);
			current++;
		}
		yield return Click(1, 0);
	}
}