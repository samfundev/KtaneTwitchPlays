using System;
using System.Collections;

public class HiddenColorsShim : ComponentSolverShim
{
	public HiddenColorsShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<KMSelectable[]>("Buttons");
		_toggle = _component.GetValue<KMSelectable>("ToggleButton");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		if (!_component.GetValue<bool>("LEDon"))
			yield return DoInteractionClick(_toggle);
		yield return DoInteractionClick(_buttons[_component.GetValue<int>("correctButton") - 1]);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("HiddenColorsScript");

	private readonly object _component;
	private readonly KMSelectable[] _buttons;
	private readonly KMSelectable _toggle;
}
