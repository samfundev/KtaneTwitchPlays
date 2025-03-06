using System.Collections;
using System.Collections.Generic;

[ModuleID("GreenWires")]
public class GreenWiresComponentSolver : ReflectionComponentSolver
{
	public GreenWiresComponentSolver(TwitchModule module) :
		base(module, "GreenWires", "!{0} cut <1-7> [Cuts the specified wire]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !command.StartsWith("cut ")) yield break;
		if (!int.TryParse(split[1], out int check)) yield break;
		if (check < 1 || check > 7) yield break;

		yield return null;
		yield return Click(check - 1, 0);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		List<int> validWires = _component.GetValue<List<int>>("cutable_wires");
		int choice = UnityEngine.Random.Range(0, validWires.Count);
		yield return Click(validWires[choice]);
	}
}