using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NumberNimblenessComponentSolver : ComponentSolver
{
	public NumberNimblenessComponentSolver(TwitchModule module) :
		base(module)
	{
		selectables = Module.BombComponent.GetComponent<KMSelectable>().Children;
		SetHelpMessage("!{0} screen [press the screen] | !{0} press <numbers> [press those numbers]");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] split = inputCommand.ToLowerInvariant().SplitFull(' ', ',', ';');

		if (split.Length == 1 && split[0].FirstOrWhole("screen") && selectables[6].gameObject.activeInHierarchy)
		{
			yield return null;
			yield return "strike";
			yield return DoInteractionClick(selectables[6]);
		}
		else if (split.Length >= 2 && split[0].FirstOrWhole("press"))
		{
			var numbers = split.Skip(1).Select(argument =>
			{
				if (!int.TryParse(argument, out int number))
					number = -1;

				return number;
			});

			if (numbers.All(number => number.InRange(0, 9)))
			{
				List<KMSelectable> buttons = new List<KMSelectable>();
				foreach (int number in numbers)
				{
					var matchingButton = selectables.Take(6).FirstOrDefault(button => button.GetComponentInChildren<TextMesh>().text == number.ToString());
					if (matchingButton == null) yield break;

					buttons.Add(matchingButton);
				}

				yield return null;
				yield return "strike";
				yield return "solve";
				foreach (KMSelectable button in buttons)
				{
					yield return DoInteractionClick(button, 0.05f);
				}
			}
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		Type NumberNimblenessScriptType = ReflectionHelper.FindType("NumberNimblenessScript");
		if (NumberNimblenessScriptType == null) yield break;

		object component = Module.BombComponent.GetComponent(NumberNimblenessScriptType);

		// The module might be in an incorrect state if a wrong number was pressed in a minigame, if so we can't force solve without taking a strike.
		if (component.GetValue<bool>("gameStarted"))
		{
			int amountPressed = component.GetValue<int>("amountPressed");
			bool stillCorrect = component.GetValue<int[]>("solution").Take(amountPressed).SequenceEqual(component.GetValue<int[]>("inputtedNums").Take(amountPressed));
			if (!stillCorrect)
				yield break;
		}

		yield return null;

		if (!component.GetValue<bool>("warmUpStarted"))
		{
			yield return RespondToCommandInternal("screen");
		}

		while (component.GetValue<int>("wins") < 3)
		{
			if (!component.GetValue<bool>("gameStarted"))
			{
				while (component.GetValue<int>("timerNum") < 3)
					yield return true;
				yield return RespondToCommandInternal("screen");
			}

			yield return RespondToCommandInternal($"press {component.GetValue<int[]>("solution").Skip(component.GetValue<int>("amountPressed")).Join()}");
			while (component.GetValue<bool>("gameStarted"))
				yield return true;
		}
	}

	private readonly KMSelectable[] selectables;
}
