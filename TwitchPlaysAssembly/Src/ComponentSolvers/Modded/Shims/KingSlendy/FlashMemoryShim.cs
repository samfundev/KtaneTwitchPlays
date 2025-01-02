using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashMemoryShim : ComponentSolverShim
{
	public FlashMemoryShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<KMSelectable[]>("ModuleButtons");
		_bolt = _component.GetValue<KMSelectable>("BoltButton");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		while (_component.GetValue<bool>("checkingPass")) yield return true;
		int curr = _component.GetValue<int>("currentStage");
		Color unlitSquare = _component.GetValue<Color[]>("squareColors")[0];
		for (int i = curr; i < 4; i++)
		{
			if (_component.GetValue<int>("pressedFlash") != 2)
			{
				if (_component.GetValue<int>("pressedFlash") == 0)
					yield return DoInteractionClick(_bolt, 0);
				while (_component.GetValue<int>("pressedFlash") == 1) yield return true;
			}
			List<int> corrects = _component.GetValue<List<int>>("squareSequence");
			for (int j = 0; j < 20; j++)
			{
				if (_buttons[j].GetComponent<Renderer>().material.color.Equals(unlitSquare) && corrects.Contains(j))
					yield return DoInteractionClick(_buttons[j]);
			}
			if (curr != 3)
				while (_component.GetValue<bool>("checkingPass")) yield return true;
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("Scr_FlashMemory", "FlashMemory");

	private readonly object _component;

	private readonly KMSelectable[] _buttons;
	private readonly KMSelectable _bolt;
}
