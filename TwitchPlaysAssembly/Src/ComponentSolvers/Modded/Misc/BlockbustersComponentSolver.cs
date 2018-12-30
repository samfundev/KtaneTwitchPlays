using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;

public class BlockbustersComponentSolver : ComponentSolver
{
	public BlockbustersComponentSolver(TwitchModule module) :
		base(module)
	{
		selectables = Module.BombComponent.GetComponent<KMSelectable>().Children;
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Press a tile using !{0} 2E. Tiles are specified by column then row.");
	}

	int CharacterToIndex(char character) => character >= 'a' ? character - 'a' : character - '1';

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = Regex.Replace(inputCommand, @"(\W|_|^(press|submit|click|answer))", "");
		if (inputCommand.Length != 2) yield break;

		int column = CharacterToIndex(inputCommand[0]);
		int row = CharacterToIndex(inputCommand[1]);

		if (column.InRange(0, 4) && row.InRange(0, 4) && (row < 4 || column % 2 == 1))
		{
			yield return null;
			yield return DoInteractionClick(selectables[Enumerable.Range(0, column).Select(n => (n % 2 == 0) ? 4 : 5).Sum() + row]);
		}
	}

	private readonly KMSelectable[] selectables;
}
