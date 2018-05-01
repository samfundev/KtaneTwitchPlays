using System.Collections;
using System.Collections.Generic;
using TwitchPlaysAssembly.ComponentSolvers.Modded.Shims;

public class AntiTrollShim : ComponentSolverShim
{
	public AntiTrollShim(BombCommander bombCommander, BombComponent bombComponent, Dictionary<string, string> trollCommands)
		: base(bombCommander, bombComponent)
	{
		_trollCommands = trollCommands ?? new Dictionary<string, string>();
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), ShimData.HelpMessage);
	}

	public AntiTrollShim(BombCommander bombCommander, BombComponent bombComponent, string[] commands, string response)
		: base(bombCommander, bombComponent)
	{
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), ShimData.HelpMessage);
		_trollCommands = new Dictionary<string, string>();
		foreach (string command in commands)
		{
			if (_trollCommands.ContainsKey(command.ToLowerInvariant().Trim().Replace(" ", ""))) continue;
			_trollCommands[command] = response;
		}
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