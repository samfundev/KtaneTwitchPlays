using System.Collections;
using TwitchPlaysAssembly.ComponentSolvers.Modded.Shims;

public class PlumbingShim : ComponentSolverShim
{
	public PlumbingShim(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), ShimData.HelpMessage);
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (inputCommand.ToLowerInvariant().Trim().Equals("spinme") && !TwitchPlaySettings.data.EnableTrollCommands)
		{
			yield return "sendtochaterror Sorry, I am not going to waste time spinning every single pipe 360 degrees.";
		}
		else
		{
			IEnumerator command = base.RespondToCommandInternal(inputCommand);
			while (command.MoveNext())
			{
				yield return command.Current;
			}
		}
	}
}
