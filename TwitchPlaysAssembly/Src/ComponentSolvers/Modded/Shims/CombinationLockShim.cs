using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombinationLockShim : ComponentSolverShim
{
	public CombinationLockShim(TwitchModule module)
		: base(module, "combinationLock")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_reset = _component.GetValue<KMSelectable>("ResetButton");
		_left = _component.GetValue<KMSelectable>("LeftButton");
		_right = _component.GetValue<KMSelectable>("RightButton");
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		yield return RespondToCommandUnshimmed(inputCommand);
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		if (_component.GetValue<IList<int>>("_inputCode").Count > 0)
			yield return DoInteractionClick(_reset, 1f);
		reset:
		int[] numbers = { _component.GetValue<IList<int>>("_code")[0], _component.GetValue<IList<int>>("_code")[1], _component.GetValue<IList<int>>("_code")[2] };
		bool turnDirection = false; // true for left, false for right 
		foreach (int num in numbers)
		{
			KMSelectable button = turnDirection ? _left : _right;
			yield return DoInteractionClick(button);

			while (_component.GetValue<int>("_currentInput") != num)
			{
				yield return DoInteractionClick(button);
			}

			yield return new WaitForSeconds(0.3f);

			if (numbers[0] != _component.GetValue<IList<int>>("_code")[0] || numbers[1] != _component.GetValue<IList<int>>("_code")[1] || numbers[2] != _component.GetValue<IList<int>>("_code")[2])
			{
				yield return DoInteractionClick(_reset, 1f);
				goto reset;
			}

			turnDirection = !turnDirection;
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("CombinationLockModule", "combination_lock");

	private readonly object _component;

	private readonly KMSelectable _reset;
	private readonly KMSelectable _left;
	private readonly KMSelectable _right;
}
