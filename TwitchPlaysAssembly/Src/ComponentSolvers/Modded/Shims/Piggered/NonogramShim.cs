using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ModuleID("NonogramModule")]
public class NonogramShim : ComponentSolverShim
{
	public NonogramShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<KMSelectable[]>("gridButtons");
		_dot = _component.GetValue<KMSelectable>("dotButton");
		_submit = _component.GetValue<KMSelectable>("submitButton");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		while (!_component.GetValue<bool>("moduleActivated"))
			yield return true;
		List<int> sol = _component.GetValue<List<int>>("solution");
		List<int> cur = _component.GetValue<List<int>>("currentGrid");
		if (!_component.GetValue<bool>("canInteract"))
		{
			for (int i = 0; i < cur.Count; i++)
			{
				if ((sol.Contains(i) && cur[i] <= 1) || (!sol.Contains(i) && cur[i] > 1))
				{
					((MonoBehaviour) _component).StopAllCoroutines();
					yield break;
				}
			}
		}
		else
		{
			bool madePress = false;
			for (int i = 0; i < cur.Count; i++)
			{
				if (sol.Contains(i) && cur[i] == 1)
				{
					if (!madePress)
						madePress = true;
					if (!_component.GetValue<bool>("isDotActive"))
						yield return DoInteractionClick(_dot);
					yield return DoInteractionClick(_buttons[i]);
				}
			}
			if (madePress)
				yield return DoInteractionClick(_dot);
			for (int i = 0; i < cur.Count; i++)
			{
				if ((sol.Contains(i) && cur[i] < 1) || (!sol.Contains(i) && cur[i] > 1))
					yield return DoInteractionClick(_buttons[i]);
			}
			yield return DoInteractionClick(_submit);
		}
		while (!Module.BombComponent.IsSolved)
			yield return true;
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("NonogramModule", "NonogramModule");

	private readonly object _component;
	private readonly KMSelectable[] _buttons;
	private readonly KMSelectable _dot;
	private readonly KMSelectable _submit;
}
