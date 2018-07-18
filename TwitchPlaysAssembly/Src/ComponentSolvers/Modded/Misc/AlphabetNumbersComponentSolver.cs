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
			switch (number)
			{
				case 1:
					yield return DoInteractionClick(buttons[0]);
					break;
				case 2:
					yield return DoInteractionClick(buttons[2]);
					break;
				case 3:
					yield return DoInteractionClick(buttons[4]);
					break;
				case 4:
					yield return DoInteractionClick(buttons[5]);
					break;
				case 5:
					yield return DoInteractionClick(buttons[3]);
					break;
				case 6:
					yield return DoInteractionClick(buttons[1]);
					break;
			}
		}
	}

	static AlphabetNumbersComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("alphabeticalOrderScript");
	}

	private static Type _componentType = null;

	private readonly Component _alphabetNumberComponent = null;
	private readonly KMSelectable[] buttons;
}