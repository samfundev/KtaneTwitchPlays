using System;
using System.Collections;
using System.Collections.Generic;

[ModuleID("spinningButtons")]
public class SpinningButtonsShim : ComponentSolverShim
{
	public SpinningButtonsShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<KMSelectable[]>("buttons");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		object[] btnInfo = _component.GetValue<object[]>("buttonInfo");
		int stage = _component.GetValue<int>("stage");
		for (int i = stage; i < 4; i++)
		{
			int ans = _component.GetValue<int>("expectedValue");
			List<int> valids = new List<int>();
			for (int j = 0; j < 4; j++)
			{
				if (btnInfo[j].GetValue<int>("buttonValue") == ans && !btnInfo[j].GetValue<bool>("pressed"))
					valids.Add(j);
			}
			yield return DoInteractionClick(_buttons[valids[UnityEngine.Random.Range(0, valids.Count)]]);
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("spinningButtonsScript", "spinningButtons");

	private readonly object _component;
	private readonly KMSelectable[] _buttons;
}
