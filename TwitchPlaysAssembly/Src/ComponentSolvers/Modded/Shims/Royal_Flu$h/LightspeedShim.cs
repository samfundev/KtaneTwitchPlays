using System;
using System.Collections;

[ModuleID("lightspeed")]
public class LightspeedShim : ComponentSolverShim
{
	public LightspeedShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		if (Unshimmed.ForcedSolveMethod == null) yield break;
		yield return null;
		var coroutine = (IEnumerator) Unshimmed.ForcedSolveMethod.Invoke(Unshimmed.CommandComponent, null);
		yield return coroutine;
		while (!_component.GetValue<bool>("moduleSolved"))
			yield return true;
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("lightspeedScript", "lightspeed");

	private readonly object _component;
}
