using System;
using System.Collections;
using System.Collections.Generic;

public class MoonShim : ComponentSolverShim
{
	public MoonShim(TwitchModule module)
		: base(module, "moon")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		List<KMSelectable> btns = _component.GetValue<List<KMSelectable>>("correctButtonsOrdered");
		int stage = _component.GetValue<int>("stage");
		for (int i = stage - 1; i < btns.Count; i++)
		{
			yield return DoInteractionClick(btns[i], .2f);
			if (_component.GetValue<int>("stage") == 9)
				break;
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("theMoonScript", "moon");

	private readonly object _component;
}
