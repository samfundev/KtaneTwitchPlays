using System.Collections;

public class AbstractSequencesComponentSolver : ReflectionComponentSolver
{
	public AbstractSequencesComponentSolver(TwitchModule module) :
		base(module, "abstractSequencesScript", "!{0} press <p1> (p2)... [Presses the numbered button(s) in the specified position(s)] | !{0} submit [Presses the submit button] | Valid positions are 1-16 in reading order")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (!command.Equals("submit") && !command.StartsWith("press ")) yield break;
		if (command.Equals("submit"))
		{
			if (!_component.GetValue<bool>("canClickAgain")) yield break;

			yield return null;
			yield return "solve";
			yield return "strike";
			yield return Click(16, 0);
		}
		else if (command.StartsWith("press "))
		{
			for (int i = 1; i < split.Length; i++)
			{
				if (!split[i].EqualsAny("1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16")) yield break;
			}
			if (!_component.GetValue<bool>("canClickAgain")) yield break;

			yield return null;
			for (int i = 1; i < split.Length; i++)
				yield return Click(int.Parse(split[i]) - 1);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		while (!_component.GetValue<bool>("canClickAgain")) yield return true;
		var bombInfo = _component.GetValue<KMBombInfo>("BombInfo");
		ArrayList finalSeq = _component.GetValue<ArrayList>("finalSequence");
		int[] btnNums = _component.GetValue<int[]>("buttonNumbers");
		ArrayList answer = new ArrayList();
		for (int i = 0; i < 16; i++)
		{
			if (!finalSeq.Contains(btnNums[i]))
				answer.Add(btnNums[i]);
		}
		bool waiter = false;
		answer.Sort();
		if ((int) bombInfo.GetTime() / 60 % 2 == 1)
		{
			answer.Reverse();
			waiter = true;
		}
		for (int i = 0; i < answer.Count; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				if (btnNums[j] == int.Parse(answer[i].ToString()))
				{
					yield return Click(j);
					break;
				}
			}
		}
		while (!((int) bombInfo.GetTime() / 60 % 2 == (waiter ? 1 : 0))) yield return true;
		yield return Click(16, 0);
		while (!_component.GetValue<bool>("moduleSolved")) yield return null;
	}
}