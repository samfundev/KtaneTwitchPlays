using System.Collections;
using System.Linq;

public class BinaryComponentSolver : ReflectionComponentSolver
{
	public BinaryComponentSolver(TwitchModule module) :
		base(module, "Binary", "!{0} submit 01 [submits the code 01]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length >= 2 && split[0].StartsWith("submit"))
		{
			var code = split.Skip(1).Join("");
			if (code.Any(letter => !letter.EqualsAny('0', '1')))
				yield break;

			yield return Click(2);

			foreach (var letter in code)
				yield return Click(letter.ToIndex() + 1);

			yield return Click(3);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		var solutions = new[] { "010001100110100101101110011010010111001101101000", "011000110110000101101110011000110110010101101100", "0101001101101111011011000111011001100101", "010001000110100101110011011000010111001001101101", "010011100110111101110100010011100110111101110100" };

		yield return RespondToCommandInternal("submit " + solutions[_component.GetValue<int>("te") - 1]);
	}
}
