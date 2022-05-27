using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class TWordsComponentSolver : ComponentSolver
{
	public TWordsComponentSolver(TwitchModule module) :
		base(module)
	{
		selectables = Module.BombComponent.GetComponent<KMSelectable>().Children;
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "!{0} press <words> [words are numbered 1–4 top to bottom] | !{0} led");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = Regex.Replace(inputCommand.ToLowerInvariant().Trim(), "^(press|hit|enter|push) ", "");

		if (inputCommand == "led")
		{
			yield return null;
			yield return DoInteractionClick(selectables[4]);
			yield break;
		}

		var buttonIndexes = inputCommand.Replace(" ", "").ToCharArray().Select(character => character - '1').ToArray();

		if (buttonIndexes.Length == 4 && buttonIndexes.Distinct().Count() == buttonIndexes.Length && buttonIndexes.All(index => index.InRange(0, 3)))
		{
			yield return null;
			foreach (int index in buttonIndexes)
			{
				yield return DoInteractionClick(selectables[index]);
			}
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		Type tWordsScriptType = ReflectionHelper.FindType("tWordsScript");
		if (tWordsScriptType == null) yield break;

		object component = Module.BombComponent.GetComponent(tWordsScriptType);

		// If an incorrect word has already been chosen then we can't solve without taking a strike.
		if (component.GetValue<bool>("incorrect"))
			yield break;

		List<int> chosenWordsIndices = component.GetValue<List<int>>("chosenWordsIndices");
		List<int> chosenWordsIndicesOrdered = component.GetValue<List<int>>("chosenWordsIndicesOrdered");

		yield return RespondToCommandInternal(chosenWordsIndicesOrdered.Select(chosenIndex => chosenWordsIndices.IndexOf(chosenIndex) + 1).Join());
	}

	private readonly KMSelectable[] selectables;
}
