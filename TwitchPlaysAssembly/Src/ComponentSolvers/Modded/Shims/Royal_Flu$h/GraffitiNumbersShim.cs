using System;
using System.Collections;
using System.Collections.Generic;

[ModuleID("graffitiNumbers")]
public class GraffitiNumbersShim : ComponentSolverShim
{
	public GraffitiNumbersShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_numbers = _component.GetValue<KMSelectable[]>("numbers");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		List<int> selectedNums = _component.GetValue<List<int>>("selectedNumbers");
		string input = _component.GetValue<string>("answer");
		string answer = _component.GetValue<string>("correctAnswer");
		for (int i = 0; i < input.Length; i++)
		{
			if (input[i] != answer[i])
				yield break;
		}
		int start = input.Length;
		for (int i = start; i < answer.Length; i++)
			yield return DoInteractionClick(_numbers[selectedNums.IndexOf(int.Parse(answer[i].ToString()))], .2f);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("paintedNumbersScript", "graffitiNumbers");

	private readonly object _component;

	private readonly KMSelectable[] _numbers;
}