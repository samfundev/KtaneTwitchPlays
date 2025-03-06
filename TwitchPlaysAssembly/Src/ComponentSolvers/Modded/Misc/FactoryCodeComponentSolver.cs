using System.Collections;

[ModuleID("factoryCode")]
public class FactoryCodeComponentSolver : ReflectionComponentSolver
{
	public FactoryCodeComponentSolver(TwitchModule module) :
		base(module, "factoryCodeScript", "!{0} submit <code> [Submits the specified defusal code]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !command.StartsWith("submit ")) yield break;
		if (split[1].Length != 4) yield break;
		string allChars = _component.GetValue<string>("clueChars").ToLower();
		for (int i = 0; i < 4; i++)
		{
			if (!allChars.Contains(split[1][i].ToString()))
				yield break;
		}

		yield return null;
		int[] positions = _component.GetValue<int[]>("posArray");
		for (int i = 0; i < 4; i++)
			yield return SelectIndex(positions[i], allChars.IndexOf(split[1][i]), allChars.Length, _component.GetValue<KMSelectable>("btn" + i + "_1"), _component.GetValue<KMSelectable>("btn" + i + "_0"));
		yield return Click(8, 0);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		string allChars = _component.GetValue<string>("clueChars");
		string correctAnswer = _component.GetValue<string>("answer");
		int[] positions = _component.GetValue<int[]>("posArray");
		for (int i = 0; i < 4; i++)
			yield return SelectIndex(positions[i], allChars.IndexOf(correctAnswer[i]), allChars.Length, _component.GetValue<KMSelectable>("btn" + i + "_1"), _component.GetValue<KMSelectable>("btn" + i + "_0"));
		yield return Click(8, 0);
	}
}