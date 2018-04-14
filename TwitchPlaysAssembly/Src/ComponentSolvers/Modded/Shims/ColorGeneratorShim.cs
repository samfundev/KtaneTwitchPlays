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
		if (inputCommand.ToLowerInvariant().Equals("troll") && !TwitchPlaySettings.data.EnableTrollCommands)
		{
			yield return "sendtochaterror Sorry, I am not going to press the red button 75 times, the green button 75 times, and the blue button 75 times.";
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
