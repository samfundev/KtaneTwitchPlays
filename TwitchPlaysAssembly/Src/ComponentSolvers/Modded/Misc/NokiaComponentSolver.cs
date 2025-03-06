using System.Collections;
using UnityEngine;

[ModuleID("GL_nokiaModule")]
public class NokiaComponentSolver : ReflectionComponentSolver
{
	public NokiaComponentSolver(TwitchModule module) :
		base(module, "NokiaModule", "!{0} type <code> [Types in the specified code] | !{0} submit/send [Presses the green button] | !{0} clear/delete [Presses the red button]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (command.EqualsAny("submit", "send"))
		{
			yield return null;
			yield return Click(9, 0);
		}
		else if (command.EqualsAny("clear", "delete"))
		{
			yield return null;
			yield return Click(11, 0);
		}
		else if (command.StartsWith("type "))
		{
			if (split.Length != 2) yield break;
			if (!int.TryParse(split[1], out int check)) yield break;
			if (check < 0) yield break;

			yield return null;
			for (int i = 0; i < split[1].Length; i++)
			{
				if (split[1][i] == '0')
					yield return Click(10);
				else
					yield return Click(int.Parse(split[1][i].ToString()) - 1);
			}
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		string answer = _component.GetValue<int>("correctCode").ToString();
		string input = _component.GetValue<TextMesh>("typingText").text;
		if (!answer.StartsWith(input))
		{
			if (!input.Equals("Type in..."))
				yield return Click(11);
			input = "";
		}
		int start = input.Length;
		for (int i = start; i < 6; i++)
		{
			if (answer[i] == '0')
				yield return Click(10);
			else
				yield return Click(int.Parse(answer[i].ToString()) - 1);
		}
		yield return Click(9);
	}
}