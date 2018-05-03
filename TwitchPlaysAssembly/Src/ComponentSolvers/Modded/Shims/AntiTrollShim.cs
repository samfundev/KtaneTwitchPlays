using System.Collections;
using System.Collections.Generic;
using TwitchPlaysAssembly.ComponentSolvers.Modded.Shims;

public class AntiTrollShim : ComponentSolverShim
{
	public AntiTrollShim(BombCommander bombCommander, BombComponent bombComponent, string moduleType, Dictionary<string, string> trollCommands)
		: base(bombCommander, bombComponent, moduleType)
	{
		_trollCommands = trollCommands ?? new Dictionary<string, string>();
	}

	public AntiTrollShim(BombCommander bombCommander, BombComponent bombComponent, string moduleType, string[] commands, string response)
		: base(bombCommander, bombComponent, moduleType)
	{
		_trollCommands = new Dictionary<string, string>();
		foreach (string command in commands)
			_trollCommands[command.ToLowerInvariant().Trim().Replace(" ", "")] = response;
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		if (!TwitchPlaySettings.data.EnableTrollCommands && _trollCommands.TryGetValue(inputCommand.ToLowerInvariant().Trim().Replace(" ", ""), out string trollResponse))
		{
			yield return $"sendtochaterror {trollResponse}";
		}
		else
		{
			IEnumerator respondToCommandInternal = RespondToCommandUnshimmed(inputCommand);
			while (respondToCommandInternal.MoveNext())
			{
				yield return respondToCommandInternal.Current;
			}
		}
	}

	private Dictionary<string, string> _trollCommands;
}