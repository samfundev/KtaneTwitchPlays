using System.Collections;
using System.Collections.Generic;

[ModuleID("MazeV2")]
[ModuleID("danielDice")]
public class AntiTrollShim : ComponentSolverShim
{
	public AntiTrollShim(TwitchModule module)
		: base(module)
	{
		trollModules.TryGetValue(module.BombComponent.GetModuleID(), out _trollCommands);
	}

	public AntiTrollShim(TwitchModule module, IEnumerable<string> commands, string response)
		: base(module)
	{
		_trollCommands = new Dictionary<string, string>();
		foreach (string command in commands)
			_trollCommands[command.ToLowerInvariant().Trim().Replace(" ", "")] = response;
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		if ((!TwitchPlaySettings.data.EnableTrollCommands && !TwitchPlaySettings.data.AnarchyMode) && _trollCommands.TryGetValue(inputCommand.ToLowerInvariant().Trim().Replace(" ", ""), out string trollResponse))
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

	private readonly Dictionary<string, string> _trollCommands;

	private readonly Dictionary<string, Dictionary<string, string>> trollModules = new Dictionary<string, Dictionary<string, string>>()
	{
		{ "MazeV2", new Dictionary<string, string> { { "spinme", "Sorry, I am not going to waste time spinning every single pipe 360 degrees." } } },
		{ "danielDice", new Dictionary<string, string> { { "rdrts", "Sorry, the secret gambler's room is off limits to you" } } }
	};
}