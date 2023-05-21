using System.Collections;
using UnityEngine;

public class ForgetFractalComponentSolver : ReflectionComponentSolver
{
	public ForgetFractalComponentSolver(TwitchModule module) :
		base(module, "ForgetFractalModule", "!{0} screen/display [Presses the screen/display] | !{0} a2 g d3 ? [Sets the cell at A2 to green and D3 to ? (letter is column, number is row)]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (command.EqualsAny("screen", "display"))
		{
			if (_component.GetValue<int>("_state") == 0 || _component.GetValue<int>("_state") == 1)
			{
				yield return "sendtochaterror The screen/display cannot be interacted with yet!";
				yield break;
			}
			yield return null;
			yield return "multiple strikes";
			yield return Click(0, 0);
			yield return "end multiple strikes";
		}
		else if (split.Length % 2 == 0)
		{
			for (int i = 0; i < split.Length; i++)
			{
				if (i % 2 == 0)
				{
					if (split[i].Length != 2)
						yield break;
					if (!split[i][0].EqualsAny('a', 'b', 'c', 'd', 'e', 'f', 'g', 'h') || !split[i][1].EqualsAny('1', '2', '3', '4'))
						yield break;
				}
				else if (!split[i].EqualsAny("k", "r", "g", "b", "y", "m", "?"))
					yield break;
			}
			if (_component.GetValue<int>("_state") != 3)
			{
				yield return "sendtochaterror You must be in submit mode to do this!";
				yield break;
			}
			yield return null;
			for (int i = 0; i < split.Length; i += 2)
			{
				int index = "1234".IndexOf(split[i][1]) * 8 + "abcdefgh".IndexOf(split[i][0]);
				while (_component.GetValue<object[]>("_cells")[_btnPositions[index]].GetValue<Color>("Color") != _colors["krgbym?".IndexOf(split[i + 1][0])])
					yield return Click(_btnPositions[index] + 1);
			}
		}
	}

	private readonly Color[] _colors = { Color.black, Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan };
	private readonly int[] _btnPositions = { 0, 1, 4, 5, 16, 17, 20, 21, 2, 3, 6, 7, 18, 19, 22, 23, 8, 9, 12, 13, 24, 25, 28, 29, 10, 11, 14, 15, 26, 27, 30, 31 };
}