using System;
using System.Collections;

public class FlashingLightsShim : ComponentSolverShim
{
	public FlashingLightsShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<KMSelectable[]>("button");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		if (_component.GetValue<int>("stage") == 0)
		{
			yield return DoInteractionClick(_buttons[_component.GetValue<int>("answer1") - 1]);
			yield return DoInteractionClick(_buttons[_component.GetValue<int>("answer2") - 1], 0);
		}
		else
			yield return DoInteractionClick(_buttons[_component.GetValue<int>("answer2") - 1]);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("doubleNegativesScript", "flashingLights");

	private readonly object _component;
	private readonly KMSelectable[] _buttons;
}
