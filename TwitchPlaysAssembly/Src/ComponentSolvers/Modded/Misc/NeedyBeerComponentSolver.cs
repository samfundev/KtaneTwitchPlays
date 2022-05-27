using System.Collections;

public class NeedyBeerComponentSolver : ComponentSolver
{
	public NeedyBeerComponentSolver(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Refill that beer with !{0} refill.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (inputCommand.ToLowerInvariant().Trim().EqualsAny("refill"))
		{
			yield return null;
			yield return DoInteractionClick(System.Array.Find(Module.BombComponent.GetComponentsInChildren<KMSelectable>(), x => x.name.Equals("Solve")));
		}
	}
}
