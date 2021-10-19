using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class StarsShim : ComponentSolverShim
{
	public StarsShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_starBtns = _component.GetValue<KMSelectable[]>("StarFormation");
		_otherBtns = _component.GetValue<KMSelectable[]>("ComplementaryButtons");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		Material[] colors = _component.GetValue<Material[]>("Colors");
		int[] input = _component.GetValue<int[]>("SiphoneAnswer");
		int[] answer = _component.GetValue<int[]>("Siphone");
		while (_component.GetValue<MeshRenderer[]>("Stars")[0].material.color == colors[0].color) yield return true;
		if (_component.GetValue<bool>("Animating"))
		{
			for (int i = 0; i < 10; i++)
			{
				if (input[i] != answer[i])
				{
					((MonoBehaviour) _component).StopAllCoroutines();
					yield break;
				}
			}
		}
		else
		{
			int start = 0;
			for (int i = 0; i < 10; i++)
			{
				if (input[i] != 0 && input[i] != answer[i])
				{
					input = new int[10];
					yield return DoInteractionClick(_otherBtns[0], .4f);
					break;
				}
				if (input[i] == 0)
				{
					start = i;
					break;
				}
			}
			if (input.Count(x => x != 0) == 10)
				start = 10;
			for (int i = start; i < 10; i++)
			{
				if (answer[i] == 0)
					break;
				yield return DoInteractionClick(_starBtns[answer[i] - 1], .2f);
			}
			yield return DoInteractionClick(_otherBtns[1], 0);
		}
		while (!_component.GetValue<bool>("ModuleSolved")) yield return true;
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("Stars2Script", "stars");

	private readonly object _component;

	private readonly KMSelectable[] _starBtns;
	private readonly KMSelectable[] _otherBtns;
}