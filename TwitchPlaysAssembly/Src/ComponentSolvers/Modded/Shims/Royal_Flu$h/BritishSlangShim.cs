using System;
using System.Collections;
using UnityEngine;

public class BritishSlangShim : ComponentSolverShim
{
	public BritishSlangShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		object[] btns = _component.GetValue<object[]>("buttons");
		string correctAnswer = "";
		int stage = _component.GetValue<int>("stage");
		for (int i = stage - 1; i < 5; i++)
		{
			if (i != -1)
				correctAnswer = _component.GetValue<string[]>("correctAnswer")[i];
			for (int j = 0; j < btns.Length; j++)
			{
				if (btns[j].GetValue<TextMesh>("text").text == correctAnswer)
				{
					yield return DoInteractionClick(btns[j].GetValue<KMSelectable>("selectable"));
					break;
				}
			}
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("britishSlangScript", "britishSlang");

	private readonly object _component;
}
