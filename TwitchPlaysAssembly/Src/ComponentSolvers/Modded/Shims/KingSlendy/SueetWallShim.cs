using System;
using System.Collections;
using System.Collections.Generic;

[ModuleID("SueetWall")]
public class SueetWallShim : ComponentSolverShim
{
	public SueetWallShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<KMSelectable[]>("ModuleButtons");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		List<int> pressedButtons = _component.GetValue<List<int>>("pressedButtons");
		bool[] correctButtons = _component.GetValue<bool[]>("correctButtons");
		for (int i = 0; i < 20; i++)
		{
			if (correctButtons[i] && !pressedButtons.Contains(i))
				yield return DoInteractionClick(_buttons[i]);
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("scr_sueetWall", "SueetWall");

	private readonly object _component;

	private readonly KMSelectable[] _buttons;
}
