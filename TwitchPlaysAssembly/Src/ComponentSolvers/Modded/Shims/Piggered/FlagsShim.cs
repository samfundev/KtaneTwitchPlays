using System;
using System.Collections;

[ModuleID("FlagsModule")]
public class FlagsShim : ComponentSolverShim
{
	public FlagsShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_left = _component.GetValue<KMSelectable>("leftButton");
		_right = _component.GetValue<KMSelectable>("rightButton");
		_submit = _component.GetValue<KMSelectable>("submitButton");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		IList countries = _component.GetValue<IList>("countries");
		object[] order = _component.GetValue<object[]>("order");
		int cur = _component.GetValue<int>("position");
		int ans = _component.GetValue<int>("number") - 1;
		yield return SelectIndex(cur, countries.IndexOf(order[ans]), 7, _right, _left);
		yield return DoInteractionClick(_submit);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("FlagsModule", "FlagsModule");

	private readonly object _component;
	private readonly KMSelectable _left;
	private readonly KMSelectable _right;
	private readonly KMSelectable _submit;
}
