using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AlphabetNumbersComponentSolver : ComponentSolver
{
	public AlphabetNumbersComponentSolver(BombCommander bombCommander, BombComponent bombComponent)
		: base (bombCommander, bombComponent)
	{
		_alphabetNumberComponent = bombComponent.GetComponent(_componentType);
		buttons = _alphabetNumberComponent.GetComponent<KMSelectable>().Children;
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Press the buttons with !{0} press 1 2 3 4 5 6. The buttons are numbered 1 to 6 in clockwise order.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] split = inputCommand.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (split[0] != "press" || split.Length == 1) yield break;

		List<int> correct = new List<int> { };
		foreach (string number in split.Skip(1))
		{
			if (!int.TryParse(number, out int result)) yield break;
			if (result > buttons.Length || result == 0) yield break;
			correct.Add(result);
		}

		foreach (int number in correct)
		{
			yield return null;
			yield return DoInteractionClick(buttons[_buttonMap[number - 1]]);
		}
	}

	static AlphabetNumbersComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("alphabeticalOrderScript");
	}

	private static Type _componentType = null;

	private readonly int[] _buttonMap = { 0, 2, 4, 5, 3, 1 };

	private readonly Component _alphabetNumberComponent = null;
	private readonly KMSelectable[] buttons;
}