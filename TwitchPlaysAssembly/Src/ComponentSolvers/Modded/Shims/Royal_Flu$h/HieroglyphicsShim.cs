using System.Collections;

public class HieroglyphicsShim : ComponentSolverShim
{
	public HieroglyphicsShim(TwitchModule module)
		: base(module)
	{
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
