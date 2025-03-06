using System;
using System.Collections;
using System.Collections.Generic;

[ModuleID("sun")]
public class SunShim : ComponentSolverShim
{
	public SunShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		List<KMSelectable> btns = _component.GetValue<List<KMSelectable>>("correctButtonsOrdered");
		int stage = _component.GetValue<int>("stage");
		for (int i = stage - 1; i < btns.Count; i++)
		{
			yield return DoInteractionClick(btns[i], 0.2f);
			if (_component.GetValue<int>("stage") == 9)
				break;
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("theSunScript", "sun");

	private readonly object _component;
}
