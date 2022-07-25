using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

public class TripleVisionComponentSolver : CommandComponentSolver
{
	public TripleVisionComponentSolver(TwitchModule module) :
		base(module, "tripleVision", "!{0} press <coord> [Presses the panel at the specified coordinate] | Valid coordinates are A1-H8 with letters as column and numbers as row")
	{
		_buttons = (KMSelectable[]) ButtonsField.GetValue(_component);
	}

	private IEnumerator Press(CommandParser _)
	{
		_.Literal("press");
		_.Regex("[a-h][1-8]", out Match match);

		if (match.Success)
		{
			yield return null;
			char[] letters = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' };
			int letIndex = Array.IndexOf(letters, match.Groups[0].Value[0]);
			yield return DoInteractionClick(_buttons[(int.Parse(match.Groups[0].Value[1].ToString()) - 1) * 8 + letIndex]);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		yield return DoInteractionClick(_buttons[SolutionValue]);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("tripleVision");
	private static readonly FieldInfo ButtonsField = ComponentType.GetField("gridSel", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo SolutionValueField = ComponentType.GetField("goal", BindingFlags.NonPublic | BindingFlags.Instance);

	private readonly KMSelectable[] _buttons;

	private int SolutionValue => (int) SolutionValueField.GetValue(_component);
}