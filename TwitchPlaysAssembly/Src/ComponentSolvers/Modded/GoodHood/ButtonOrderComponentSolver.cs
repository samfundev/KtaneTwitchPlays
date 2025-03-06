using System.Collections;

[ModuleID("buttonOrder")]
public class ButtonOrderComponentSolver : ReflectionComponentSolver
{
	public ButtonOrderComponentSolver(TwitchModule module) :
		base(module, "ButtonOrder", "!{0} press <btn1> <btn2> [Presses the specified buttons in order] | Valid buttons are 1 and 2")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 3 || !command.StartsWith("press ")) yield break;
		if (!command.Substring(6).EqualsAny("1 2", "2 1", "1 1", "2 2")) yield break;

		yield return null;
		yield return Click(int.Parse(split[1]) - 1);
		yield return Click(int.Parse(split[2]) - 1, 0);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		int[] cur = _component.GetValue<int[]>("yourpresses");
		int[] ans = _component.GetValue<int[]>("correctpresses");
		if (cur[0] == 0 && cur[1] == 0)
			yield return Click(ans[0] - 1);
		if ((cur[0] != 0 && ans[0] != cur[0]) || (cur[1] != 0 && ans[1] != cur[1]))
			yield break;
		yield return Click(ans[1] - 1, 0);
	}
}