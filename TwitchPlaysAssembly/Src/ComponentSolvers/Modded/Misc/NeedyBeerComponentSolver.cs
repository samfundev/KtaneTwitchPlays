using System;
using System.Collections;
using System.Linq;

public class NeedyBeerComponentSolver : ComponentSolver
{
	public NeedyBeerComponentSolver(BombCommander bombCommander, BombComponent bombComponent, CoroutineCanceller canceller) 
		: base(bombCommander, bombComponent, canceller)
	{
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (inputCommand.ToLowerInvariant().EqualsAny("refill"))
		{
			yield return null;
			yield return DoInteractionClick(BombComponent.GetComponentsInChildren<KMSelectable>().FirstOrDefault(x => x.name.Equals("Solve")));
		}
	}
}
