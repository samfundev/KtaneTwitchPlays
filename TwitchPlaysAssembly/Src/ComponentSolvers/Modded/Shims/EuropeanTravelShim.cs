using System;
using System.Collections;
using TwitchPlaysAssembly.ComponentSolvers.Modded.Shims;

public class EuropeanTravelShim : ComponentSolverShim
{
	public EuropeanTravelShim(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent, "europeanTravel")
	{
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		if (inputCommand.Length < 7 || inputCommand.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Length < 5) yield break;

		IEnumerator respondToCommandInternal = RespondToCommandUnshimmed(inputCommand);
		while (respondToCommandInternal.MoveNext())
		{
			yield return respondToCommandInternal.Current;
		}
	}
}
