using System;
using System.Collections;
using System.Collections.Generic;

public class ColorfulInsanityShim : ComponentSolverShim
{
	public ColorfulInsanityShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<KMSelectable[]>("ModuleButtons");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		List<int> pressedButtons = _component.GetValue<List<int>>("pressedButtons");
		List<int> correctButtons = _component.GetValue<List<int>>("correctTotal");
		for (int i = 0; i < 35; i++)
		{
			if (correctButtons.Contains(i) && !pressedButtons.Contains(i))
				yield return DoInteractionClick(_buttons[i]);
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("scr_colorfulInsanity", "ColorfulInsanity");

	private readonly object _component;

	private readonly KMSelectable[] _buttons;
}
