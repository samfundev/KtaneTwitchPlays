using System.Collections;
using System.Text.RegularExpressions;

public class LogicPlumbingComponentSolver : ReflectionComponentSolver
{
	public LogicPlumbingComponentSolver(TwitchModule module) :
		base(module, "logicPlumbing", "!{0} swap <coord1> <coord2> [Swaps the tiles at the specified coordinates] | !{0} check [Holds the top left button briefly] | Valid coordinates are A1-F6 with letters as column and numbers as row | Swaps can be chained using spaces, commas, or semicolons")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (command.StartsWith("swap "))
		{
			if (split.Length < 3 || split.Length % 2 == 0) yield break;
			for (int i = 1; i < split.Length; i++)
			{
				if (!Regex.IsMatch(split[i], @"^\s*[a-f][1-6]\s*$")) yield break;
			}

			yield return null;
			for (int i = 1; i < split.Length; i++)
			{
				yield return Click(split[i][1].ToIndex() * 6 + split[i][0].ToIndex());
			}
		}
		else if (command.Equals("check"))
		{
			yield return null;
			DoInteractionStart(selectables[36]);
			while (_component.GetValue<int>("waveStep") <= 24) yield return "trycancel";
			DoInteractionEnd(selectables[36]);
			if (_component.GetValue<bool>("solved")) yield return "solve";
		}
	}
}