using System;
using System.Collections;

[ModuleID("numberCipher")]
public class NumberCipherShim : ComponentSolverShim
{
	public NumberCipherShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_left = _component.GetValue<KMSelectable>("cycleLeftButton");
		_right = _component.GetValue<KMSelectable>("cycleRightButton");
		_submit = _component.GetValue<KMSelectable>("executeButton");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		while (_component.GetValue<bool>("executeLock")) yield return true;
		int corAns = int.Parse(_component.GetValue<string>("correctAnswer"));
		int current = _component.GetValue<int>("displayedNumber");
		yield return SelectIndex(current, corAns, 10, _right, _left);
		yield return DoInteractionClick(_submit, 0);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("numberCipherScript", "numberCipher");

	private readonly object _component;
	private readonly KMSelectable _left;
	private readonly KMSelectable _right;
	private readonly KMSelectable _submit;
}
