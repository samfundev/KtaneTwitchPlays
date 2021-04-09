using System;
using System.Collections;
using UnityEngine;

public class ZoniShim : ComponentSolverShim
{
	public ZoniShim(TwitchModule module)
		: base(module, "lgndZoni")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<KMSelectable[]>("buttons");
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		yield return RespondToCommandUnshimmed(inputCommand);
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		TextMesh[] btnLabels = _component.GetValue<TextMesh[]>("buttonLabels");
		int stage = _component.GetValue<int>("solvedStages");
		for (int i = stage; i < 3; i++)
		{
			int wordIndex = _component.GetValue<int>("wordIndex");
			bool correct = false;
			for (int j = 0; j < 10; j++)
			{
				switch (j)
				{
					case 0: correct = wordIndex.EqualsAny(10, 14, 18, 34, 49); break;
					case 1: correct = wordIndex.EqualsAny(0, 8, 13, 23, 26, 37, 48); break;
					case 2: correct = wordIndex.EqualsAny(5, 21, 29, 33, 43); break;
					case 3: correct = wordIndex.EqualsAny(9, 12, 28, 35, 51); break;
					case 4: correct = wordIndex.EqualsAny(3, 17, 27, 30, 39, 47, 52); break;
					case 5: correct = wordIndex.EqualsAny(1, 22, 41, 50); break;
					case 6: correct = wordIndex.EqualsAny(4, 6, 15, 24, 40, 42); break;
					case 7: correct = wordIndex.EqualsAny(2, 7, 19, 36, 44); break;
					case 8: correct = wordIndex.EqualsAny(16, 20, 31, 38, 45, 53); break;
					case 9: correct = wordIndex.EqualsAny(11, 25, 32, 46); break;
				}
				if (correct)
				{
					for (int k = 0; k < 10; k++)
					{
						if (btnLabels[k].text == j.ToString())
						{
							yield return DoInteractionClick(_buttons[k]);
							break;
						}
					}
				}
			}
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("ZoniModuleScript");

	private readonly object _component;
	private readonly KMSelectable[] _buttons;
}
