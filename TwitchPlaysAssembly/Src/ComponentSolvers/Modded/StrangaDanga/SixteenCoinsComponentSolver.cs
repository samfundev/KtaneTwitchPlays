using System.Collections;

public class SixteenCoinsComponentSolver : ReflectionComponentSolver
{
	public SixteenCoinsComponentSolver(TwitchModule module) :
		base(module, "sixteenCoins", "!{0} press <pos> [Presses the coin in the specified position] | Valid positions are 1-16 in reading order")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !command.StartsWith("press ")) yield break;
		if (!int.TryParse(split[1], out _)) yield break;
		if (int.Parse(split[1]) < 1 || int.Parse(split[1]) > 16) yield break;

		yield return null;
		yield return Click(int.Parse(split[1]) - 1, 0);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		yield return Click(_component.GetValue<int>("indexOfTargetCoin"));
	}
}