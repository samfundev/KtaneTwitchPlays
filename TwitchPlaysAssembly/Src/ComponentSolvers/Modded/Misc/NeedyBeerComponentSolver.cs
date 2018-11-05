using System.Collections;
using System.Linq;

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
			yield return DoInteractionClick(Module.GetComponentsInChildren<KMSelectable>().FirstOrDefault(x => x.name.Equals("Solve")));
		}
	}
}
