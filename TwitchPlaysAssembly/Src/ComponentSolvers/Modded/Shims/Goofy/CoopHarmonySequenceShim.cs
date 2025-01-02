using System.Collections;

public class CoopHarmonySequenceShim : ReflectionComponentSolverShim
{
	public CoopHarmonySequenceShim(TwitchModule module)
		: base(module, "CoopHarmonySequenceScript", "coopharmonySequence")
	{
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		if (Unshimmed.ForcedSolveMethod == null) yield break;
		var coroutine = (IEnumerator) Unshimmed.ForcedSolveMethod.Invoke(Unshimmed.CommandComponent, null);
		while (coroutine.MoveNext())
			yield return coroutine.Current;
		while (_component.GetValue<bool>("harmonyRunning"))
			yield return true;
	}
}
