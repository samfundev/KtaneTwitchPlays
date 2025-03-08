using System.Collections;

[ModuleID("tripleVision")]
public class TripleVisionComponentSolver : CommandComponentSolver
{
	public TripleVisionComponentSolver(TwitchModule module) :
		base(module, "tripleVision", "!{0} press <coord> [Presses the panel at the specified coordinate] | Valid coordinates are A1-H8 with letters as column and numbers as row")
	{
	}

	[Command("press ([a-h][1-8])")]
	private IEnumerator Press(string value)
	{
		yield return null;
		int letIndex = value[0].ToIndex();
		yield return Click(value[1].ToIndex() * 8 + letIndex);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		yield return Click(_component.GetValue<int>("goal"));
	}
}