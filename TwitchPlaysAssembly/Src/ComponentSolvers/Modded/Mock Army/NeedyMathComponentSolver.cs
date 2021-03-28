using System.Collections;
using System.Collections.Generic;

public class NeedyMathComponentSolver : ReflectionComponentSolver
{
	public NeedyMathComponentSolver(TwitchModule module)
		: base(module, "NeedyMathModule", "Submit an answer with !{0} submit -47.")
	{
		LogSelectables();

		buttonMap = new Dictionary<string, int>()
		{
			{ "1", 0 },
			{ "2", 1 },
			{ "3", 2 },
			{ "0", 3 },
			{ "4", 4 },
			{ "5", 5 },
			{ "6", 6 },
			{ "-", 7 },
			{ "7", 8 },
			{ "8", 9 },
			{ "9", 10 },
			{ "Enter", 11 },
		};
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || split[0] != "submit")
			yield break;

		if (!int.TryParse(split[1], out _))
			yield break;

		yield return null;

		foreach (char character in split[1])
		{
			yield return Click(character);
		}

		yield return Click("Enter");
	}
}
