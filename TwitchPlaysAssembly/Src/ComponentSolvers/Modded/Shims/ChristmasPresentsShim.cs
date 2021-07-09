using System;
using System.Collections;
using UnityEngine;

public class ChristmasPresentsShim : ComponentSolverShim
{
	public ChristmasPresentsShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_clock = _component.GetValue<KMSelectable>("clockButton");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		int correctTime = _component.GetValue<int>("correctTime");
		if (_component.GetValue<int>("hour") == correctTime)
			yield return DoInteractionClick(_clock, 0);
		else
		{
			while (_component.GetValue<int>("hour") != correctTime) yield return true;
			yield return new WaitForSeconds(0.02f);
			yield return DoInteractionClick(_clock, 0);
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("christmasTreeScript", "christmasPresents");

	private readonly object _component;
	private readonly KMSelectable _clock;
}
