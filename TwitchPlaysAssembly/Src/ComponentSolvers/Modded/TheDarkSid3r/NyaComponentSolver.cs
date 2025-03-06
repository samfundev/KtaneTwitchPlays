using System;
using System.Collections;
using System.Linq;

[ModuleID("TDSNya")]
public class NyaComponentSolver : ReflectionComponentSolver
{
	public NyaComponentSolver(TwitchModule module) :
		base(module, "TDSNyaScript", "!{0} press <nya/nah> [Presses the nya or nah button]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !command.StartsWith("press ")) yield break;
		string[] positions = new string[] { "nya", "nah" };
		if (!positions.Contains(split[1])) yield break;

		yield return null;
		yield return Click(Array.IndexOf(positions, split[1]), 0);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		if (_component.GetValue<bool>("isCat"))
			yield return Click(0);
		else
			yield return Click(1);
	}
}