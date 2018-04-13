using System.Collections;
using TwitchPlaysAssembly.ComponentSolvers.Modded.Shims;

public class ColorGeneratorShim : ComponentSolverShim
{
	public ColorGeneratorShim(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), ShimData.HelpMessage);
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (inputCommand.ToLowerInvariant().Equals("troll"))
		{
			yield return null;
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
