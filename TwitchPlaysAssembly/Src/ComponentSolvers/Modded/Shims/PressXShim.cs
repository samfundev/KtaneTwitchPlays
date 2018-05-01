using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using TwitchPlaysAssembly.ComponentSolvers.Modded.Shims;

public class PressXShim : ComponentSolverShim
{
	public PressXShim(BombCommander bombCommander, BombComponent bombComponent) : base(bombCommander, bombComponent)
	{
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		var match = Regex.Match(inputCommand.ToLowerInvariant(),
			"^(?:press |tap )?(x|y|a|b)(?:(?: at| on)?([0-9: ]+))?$");
		if (!match.Success) yield break;

		int index = "xyab".IndexOf(match.Groups[1].Value, StringComparison.Ordinal);
		if (index < 0) yield break;

		string[] times = match.Groups[2].Value.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

		if (times.Any() && index < 2)
		{
			foreach (string time in times)
			{
				string[] split = time.Split(':');
				if (split.Length <= 4 && split.All(x => int.TryParse(x, out _)))
					continue;

				yield return $"sendtochaterror Badly formatted time {time}. Time should either be in seconds (53) or in full time (1:23:45)";
				yield break;
			}

			yield return null;
			yield return null;
		}

		IEnumerator baseResponder = RespondToCommandUnshimmed(inputCommand);
		while (baseResponder.MoveNext())
		{
			yield return baseResponder.Current;
		}
	}
}
