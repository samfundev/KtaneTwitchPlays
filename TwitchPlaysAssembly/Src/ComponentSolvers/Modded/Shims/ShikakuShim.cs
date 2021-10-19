using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShikakuShim : ComponentSolverShim
{
	public ShikakuShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<KMSelectable[]>("_buttons");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		TextMesh[] hints = _component.GetValue<TextMesh[]>("_hints");
		int[] solution = _component.GetValue<int[]>("_puzzle");
		int[] current = _component.GetValue<int[]>("_grid");
		for (int h = 0; h < hints.Length; h++)
		{
			if (hints[h].text != "")
			{
				IList shapes = _component.GetValue<IList>("_shapes");
				int curNumber = solution[h];
				for (int c = 0; c < shapes.Count; c++)
				{
					if (shapes[c].GetValue<int>("HintNode") == h)
					{
						for (int index = 0; index < 36; index++)
						{
							if (solution[index] == curNumber && current[index] != curNumber && index != h)
							{
								if (_component.GetValue<int>("_activeShape") != curNumber)
									yield return DoInteractionClick(_buttons[h]);
								if (!shapes[c].GetValue<bool>("CurrentHintCorrect") && !shapes[c].GetValue<object>("ShapeType").GetValue<bool>("IsNumber"))
									yield return DoInteractionClick(_buttons[h]);
								yield return DoInteractionClick(_buttons[index]);
							}
						}
						break;
					}
				}
			}
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("Shikaku", "shikaku");

	private readonly object _component;

	private readonly KMSelectable[] _buttons;
}
