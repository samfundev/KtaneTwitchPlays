using System.Collections;

public class HieroglyphicsShim : ComponentSolverShim
{
	public HieroglyphicsShim(TwitchModule module)
		: base(module, "hieroglyphics")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		yield return RespondToCommandUnshimmed(inputCommand);
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		if (Unshimmed.ForcedSolveMethod == null) yield break;
		var coroutine = (IEnumerator) Unshimmed.ForcedSolveMethod.Invoke(Unshimmed.CommandComponent, null);
		while (coroutine.MoveNext())
			yield return coroutine.Current;
		while (!Module.BombComponent.IsSolved)
			yield return true;
	}
}
