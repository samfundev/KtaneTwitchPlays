using System.Collections;

public class GatekeeperComponentSolver : ReflectionComponentSolver
{
	public GatekeeperComponentSolver(TwitchModule module) :
		base(module, "Gatekeeper", "!{0} press <pos> [Presses the button in the specified position] | Valid positions are left(l), middle(m), or right(r)")
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

		while (!_component.GetValue<bool>("isActivated")) yield return true;
		yield return Click(_component.GetValue<int>("correctButton"));
	}
}