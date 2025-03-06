using System;
using System.Collections;
using System.Linq;
using KModkit;

[ModuleID("TDSAmogus")]
public class AmogusComponentSolver : ReflectionComponentSolver
{
	public AmogusComponentSolver(TwitchModule module) :
		base(module, "TDSAmogusScript", "!{0} press <pos> [Presses the comic panel in the specified position] | Valid positions are tl, tr, bl, or br")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !command.StartsWith("press ")) yield break;
		string[] positions = new string[] { "tl", "tr", "bl", "br" };
		if (!positions.Contains(split[1])) yield break;

		yield return null;
		yield return Click(Array.IndexOf(positions, split[1]), 0);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		var edgework = Module.BombComponent.GetComponent<KMBombInfo>();
		yield return null;

		yield return Click(Array.IndexOf(_component.GetValue<string[]>("serials"), edgework.GetSerialNumber()));
	}
}