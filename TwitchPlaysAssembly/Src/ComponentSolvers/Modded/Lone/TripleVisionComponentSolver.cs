using System;
using System.Collections;
using System.Text.RegularExpressions;

public class TripleVisionComponentSolver : CommandComponentSolver
{
	public TripleVisionComponentSolver(TwitchModule module) :
		base(module, "tripleVision", "!{0} press <coord> [Presses the panel at the specified coordinate] | Valid coordinates are A1-H8 with letters as column and numbers as row")
	{
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
			yield return Click((int.Parse(match.Groups[0].Value[1].ToString()) - 1) * 8 + letIndex);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		yield return Click(_component.GetValue<int>("goal"));
	}
}