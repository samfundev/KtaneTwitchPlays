using System;
using System.Collections;
using System.Linq;

[ModuleID("CactusPConundrum")]
public class CactiConundrumComponentSolver : ReflectionComponentSolver
{
	public CactiConundrumComponentSolver(TwitchModule module) :
		base(module, "conundramScript", "!{0} press <jeeves/hammer/chicken/cactus/shark/spoon> [Presses the jeeves, hammer, chicken, cactus, shark, or spoon button]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !command.StartsWith("press ")) yield break;
		string[] btnLabels = { "jeeves", "hammer", "chicken", "shark", "spoon", "cactus" };
		if (!btnLabels.Contains(split[1])) yield break;

		yield return null;
		yield return Click(Array.IndexOf(btnLabels, split[1]), 0);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		int[] btnArr = { 0, 1, 2, 5, 3, 4 };
		int start = _component.GetValue<int>("Stage");
		for (int i = start; i < 3; i++)
			yield return Click(btnArr[_component.GetValue<int>("correctBtn")]);
	}
}