using System.Collections;
using System.Linq;
using System;

public class HyperactiveNumsComponentSolver : ReflectionComponentSolver
{
	public HyperactiveNumsComponentSolver(TwitchModule module) :
		base(module, "HyperactiveNumbersScript", "!{0} submit <color> <parity> [Presses submit when the middle number that has the specified color and parity]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 3 || !command.StartsWith("submit ")) yield break;
		if (!_colors.Contains(split[1])) yield break;
		if (!split[2].EqualsAny("even", "odd")) yield break;

		yield return null;
		int c = _component.GetValue<int>("c");
		int prevc = c - 1;
		while ((_component.GetValue<int>("displayedNum") % 2 == 0 != split[2].Equals("even")) || _component.GetValue<int>("displayedColorIndex") != Array.IndexOf(_colors, split[1]))
		{
			yield return "trycancel";
			if (c != _component.GetValue<int>("c"))
			{
				prevc = c;
				c = _component.GetValue<int>("c");
			}
			if (prevc == 4 && c == 5)
			{
				yield return "sendtochat Cancelled waiting for submission due to a change in the left and right numbers.";
				yield break;
			}
		}
		yield return Click(0, 0);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		while (true)
		{
			int leftDisplayedNum = _component.GetValue<int>("leftDisplayedNum");
			int rightDisplayedNum = _component.GetValue<int>("rightDisplayedNum");
			bool evenDisplayedNum = _component.GetValue<int>("displayedNum") % 2 == 0;
			int displayedColorIndex = _component.GetValue<int>("displayedColorIndex");

			int solutionColor = leftDisplayedNum % 2 * 2 + rightDisplayedNum % 2;
			bool solutionEven = leftDisplayedNum < 50 == rightDisplayedNum < 50;
			if (displayedColorIndex == solutionColor && evenDisplayedNum == solutionEven)
				break;

			yield return true;
		}

		yield return Click(0, 0);
	}

	private readonly string[] _colors = new string[] { "red", "blue", "green", "yellow" };
}