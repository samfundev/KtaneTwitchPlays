using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[ModuleID("ColorfulMadness")]
public class ColorfulMadnessShim : ComponentSolverShim
{
	public ColorfulMadnessShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<KMSelectable[]>("ModuleButtons");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		List<int> pressedButtons = _component.GetValue<List<int>>("pressedButtons");
		int[] digits = _component.GetValue<int[]>("digits");
		int[] bottomHalfTextures = _component.GetValue<int[]>("bottomHalfTextures");
		int[] topHalfTextures = _component.GetValue<int[]>("topHalfTextures");
		for (int i = 0; i < 20; i++)
		{
			if (digits.Contains(i) || i >= 10 && digits.Any(digit => bottomHalfTextures[i - 10] == topHalfTextures[digit]) && !pressedButtons.Contains(i))
				yield return DoInteractionClick(_buttons[i]);
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("ColorfulMadnessScript", "ColorfulMadness");

	private readonly object _component;

	private readonly KMSelectable[] _buttons;
}
