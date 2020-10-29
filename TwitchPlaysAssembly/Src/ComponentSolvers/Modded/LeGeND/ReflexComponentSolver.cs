using System.Collections;

public class ReflexComponentSolver : ReflectionComponentSolver
{
	public ReflexComponentSolver(TwitchModule module) :
		base(module, "ReflexModuleScript", "!{0} press <pos> [Presses the button when the cycling LED is in the specified position] | Valid positions are 1-7 from left to right")
	{
		mod = module;
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !command.StartsWith("press")) yield break;
		if (!int.TryParse(split[1], out _)) yield break;
		if (int.Parse(split[1]) < 1 || int.Parse(split[1]) > 7) yield break;

		yield return null;
		while (_component.GetValue<int>("currentLight") != int.Parse(split[1]) - 1) yield return "trycancel";
		yield return Click(0, 0);
		if (_component.GetValue<bool>("moduleSolved"))
			yield return "solve";
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		int start = _component.GetValue<int>("currentStage");
		for (int i = start; i < 7; i++)
		{
			while (_component.GetValue<int>("currentLight") != _component.GetValue<int>("selectedLight")) yield return true;
			yield return Click(0, 0);
		}

		while (!mod.Solved) yield return true;
	}

	private readonly TwitchModule mod;
}