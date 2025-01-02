using System.Collections;
using System.Linq;

public class AlienFilingColorsShim : ReflectionComponentSolverShim
{
	public AlienFilingColorsShim(TwitchModule module)
		: base(module, "AFCScript", "AlienModule")
	{
	}

	protected override IEnumerator RespondShimmed(string[] split, string command)
	{
		if (!command.EqualsAny("colorblind", "colourblind", "cb"))
		{
			foreach (char c in command)
			{
				if (!_validChars.Contains(c))
					yield break;
			}
		}

		yield return RespondUnshimmed(command);
	}

	private readonly char[] _validChars = { '1', '2', '3', '4', '5', '6', '7', '8', ' ', ',', '|', '-' };
}
