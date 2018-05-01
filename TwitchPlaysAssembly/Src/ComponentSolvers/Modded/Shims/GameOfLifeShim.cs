using System.Collections;
using TwitchPlaysAssembly.ComponentSolvers.Modded.Shims;

public class GameOfLifeShim : ComponentSolverShim
{
	public GameOfLifeShim(BombCommander bombCommander, BombComponent bombComponent) : base(bombCommander, bombComponent, "GameOfLifeSimple")
	{

	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		var send = RespondToCommandUnshimmed(inputCommand);
		if (!inputCommand.ToLowerInvariant().Trim().EqualsAny("submit", "reset"))
		{
			var split = inputCommand.Split(' ');
			foreach (string set in split)
			{
				if (set.Length != 2) yield break;
			}
		}
		while (send.MoveNext()) yield return send.Current;
	}
}
