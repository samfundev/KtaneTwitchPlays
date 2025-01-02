using System;
using System.Collections;
using System.Collections.Generic;

public class HuntingShim : ComponentSolverShim
{
	public HuntingShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<KMSelectable[]>("buttons");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		while (!_component.GetValue<bool>("isActivated"))
			yield return true;
		bool[] btnBad = _component.GetValue<bool[]>("buttonisfail");
		int start = _component.GetValue<int>("stage") - 1;
		for (int i = start; i < 4; i++)
		{
			while (!_component.GetValue<bool>("acceptingInput"))
				yield return true;
			if (i != start)
				btnBad = _component.GetValue<bool[]>("buttonisfail");
			List<int> choices = new List<int>();
			for (int j = 0; j < 5; j++)
			{
				if (!btnBad[j])
					choices.Add(j);
			}
			yield return DoInteractionClick(_buttons[choices[UnityEngine.Random.Range(0, choices.Count)]], 0);
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("hunting", "Hunting");

	private readonly object _component;
	private readonly KMSelectable[] _buttons;
}
