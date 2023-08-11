using System.Collections;
using System.Collections.Generic;

public class AntiTrollShim : ComponentSolverShim
{
	public AntiTrollShim(TwitchModule module, Dictionary<string, string> trollCommands)
		: base(module)
	{
		_trollCommands = trollCommands ?? new Dictionary<string, string>();
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
}