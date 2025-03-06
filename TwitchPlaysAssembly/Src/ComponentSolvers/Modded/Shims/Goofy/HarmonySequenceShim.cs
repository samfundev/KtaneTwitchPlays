using System.Collections;

[ModuleID("harmonySequence")]
public class HarmonySequenceShim : ReflectionComponentSolverShim
{
	public HarmonySequenceShim(TwitchModule module)
		: base(module, "HarmonySequenceScript", "harmonySequence")
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
