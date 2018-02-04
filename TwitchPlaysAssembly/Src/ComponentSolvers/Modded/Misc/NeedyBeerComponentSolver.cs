using System;
using System.Collections;

public class NeedyBeerComponentSolver : ComponentSolver
{
	public NeedyBeerComponentSolver(BombCommander bombCommander, BombComponent bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) 
		: base(bombCommander, bombComponent, ircConnection, canceller)
	{
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (inputCommand.ToLowerInvariant().EqualsAny("refill"))
		{
			yield return null;
			yield return DoInteractionClick(BombComponent.GetComponentInChildren<KMSelectable>());
		}
	}
}
