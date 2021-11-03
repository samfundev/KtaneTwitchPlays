using System.Collections;

public class NeedyWiresComponentSolver : ReflectionComponentSolver
{
	public NeedyWiresComponentSolver(TwitchModule module) :
		base(module, "TDSNeedyWiresScript", "!{0} cut <#> [Cuts the specified wire 1-3 from top to bottom]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !command.StartsWith("cut ")) yield break;
		if (!int.TryParse(split[1], out int check)) yield break;
		if (check < 1 || check > 3) yield break;
		if (!_component.GetValue<bool>("isActive"))
		{
			yield return "sendtochaterror You can't interact with the module right now.";
			yield break;
		}

		yield return null;
		yield return Click(check - 1, 0);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		var needyComponent = Module.BombComponent.GetComponent<NeedyComponent>();

		while (true)
		{
			if (needyComponent.State != NeedyComponent.NeedyStateEnum.Running)
			{
				yield return true;
				continue;
			}

			yield return Click(_component.GetValue<int>("correctWire"));
		}
	}
}