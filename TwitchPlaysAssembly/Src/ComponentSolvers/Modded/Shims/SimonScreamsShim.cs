using System;
using System.Collections;
using System.Linq;
using System.Text;
using TwitchPlaysAssembly.ComponentSolvers.Modded.Shims;


public class SimonScreamsShim : ComponentSolverShim
{
	public SimonScreamsShim(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), ShimData.HelpMessage);
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] split = inputCommand.Trim().ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if (split.Length.Equals(1) && split[0].Equals("disco") && !TwitchPlaySettings.data.EnableTrollCommands)
		{
			yield return "sendtochaterror Sorry, I am not going to waste time flashing all the colors.";
		}
		else if (((split.Length.Equals(1) && split[0].Equals("lasershow")) || (split.Length.Equals(2) && split[0].Equals("laser") && split[1].Equals("show"))) && !TwitchPlaySettings.data.EnableTrollCommands)
		{
			yield return "sendtochaterror Sorry, I am not going to waste time flashing all the colors.";
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

