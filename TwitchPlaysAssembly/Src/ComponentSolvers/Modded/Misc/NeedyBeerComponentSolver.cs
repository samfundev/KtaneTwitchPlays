using System.Collections;
using System.Linq;

public class NeedyBeerComponentSolver : ComponentSolver
{
	public NeedyBeerComponentSolver(BombCommander bombCommander, BombComponent bombComponent)
		: base(bombCommander, bombComponent)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Refill that beer with !{0} refill.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (inputCommand.ToLowerInvariant().Trim().EqualsAny("refill"))
		{
			yield return null;
			yield return DoInteractionClick(BombComponent.GetComponentsInChildren<KMSelectable>().FirstOrDefault(x => x.name.Equals("Solve")));
		}
	}
}
