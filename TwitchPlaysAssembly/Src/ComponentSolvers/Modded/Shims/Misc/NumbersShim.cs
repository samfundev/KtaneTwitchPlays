using System;
using System.Collections;

public class NumbersShim : ComponentSolverShim
{
	public NumbersShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_up = _component.GetValue<KMSelectable>("buttonUp");
		_down = _component.GetValue<KMSelectable>("buttonDown");
		_submit = _component.GetValue<KMSelectable>("buttonDisplay");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		int curr = _component.GetValue<int>("inputAns");
		int stage = _component.GetValue<int>("stageCur");
		for (int i = stage; i < 5; i++)
		{
			int ans = _component.GetValue<int>("expectAns");
			yield return SelectIndex(curr, ans, 10, _up, _down);
			yield return DoInteractionClick(_submit);
			if (i == stage && curr != 0)
				curr = 0;
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("WAnumbersScript", "WAnumbers");

	private readonly object _component;

	private readonly KMSelectable _up;
	private readonly KMSelectable _down;
	private readonly KMSelectable _submit;
}
