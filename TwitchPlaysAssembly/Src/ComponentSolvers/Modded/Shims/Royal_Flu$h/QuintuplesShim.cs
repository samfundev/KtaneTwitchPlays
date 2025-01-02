using System;
using System.Collections;

public class QuintuplesShim : ComponentSolverShim
{
	public QuintuplesShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_upBtns = _component.GetValue<KMSelectable[]>("upCycleButtons");
		_downBtns = _component.GetValue<KMSelectable[]>("downCycleButtons");
		_submit = _component.GetValue<KMSelectable>("submitButton");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		int[] curInput = _component.GetValue<int[]>("displayedInputNumbers");
		int[] answers = _component.GetValue<int[]>("answers");
		for (int i = 0; i < 5; i++)
			yield return SelectIndex(curInput[i], answers[i], 10, _upBtns[i], _downBtns[i]);
		yield return DoInteractionClick(_submit, 0);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("quintuplesScript", "quintuples");

	private readonly object _component;
	private readonly KMSelectable[] _upBtns;
	private readonly KMSelectable[] _downBtns;
	private readonly KMSelectable _submit;
}