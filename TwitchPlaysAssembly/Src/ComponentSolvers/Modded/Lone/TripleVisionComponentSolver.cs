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

		yield return null;
		int letIndex = match.Groups[0].Value[0].ToIndex();
		yield return Click(match.Groups[0].Value[1].ToIndex() * 8 + letIndex);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		yield return Click(_component.GetValue<int>("goal"));
	}
}