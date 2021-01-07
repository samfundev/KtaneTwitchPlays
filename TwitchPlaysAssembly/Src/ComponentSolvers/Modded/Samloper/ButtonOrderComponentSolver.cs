using System.Collections;

public class ButtonOrderComponentSolver : ReflectionComponentSolver
{
	public ButtonOrderComponentSolver(TwitchModule module) :
		base(module, "SylwiaScript", "!{0} press <btn1> <btn2> [Presses the specified buttons in order] | Valid buttons are 1 and 2")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 3 || !command.StartsWith("press ")) yield break;
		if (!command.Substring(6).EqualsAny("1 2", "2 1")) yield break;

		yield return null;
		yield return Click(int.Parse(split[1]) - 1);
		yield return Click(int.Parse(split[2]) - 1, 0);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		int order = _component.GetValue<int>("order");
		int press = _component.GetValue<int>("press");
		if (press == 0)
			yield return Click(order - 1);
		else if ((press == 1 && order == 2) || (press == 2 && order == 1))
			yield break;
		yield return Click((order - 1) == 0 ? 1 : 0);
	}
}