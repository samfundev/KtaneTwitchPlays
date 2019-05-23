using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class FestiveJukeboxComponentSolver : ComponentSolver
{
	public FestiveJukeboxComponentSolver(TwitchModule module) :
		base(module)
	{
		selectables = Module.BombComponent.GetComponent<KMSelectable>().Children;
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Press 3 buttons using !{0} press <buttons>. (1 = top; 2 = middle; 3 = bottom.)");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = Regex.Replace(inputCommand.ToLowerInvariant().Trim(), "^(press|hit|enter|push) ", "");

		var buttonIndexes = inputCommand.Replace(" ", "").ToCharArray().Select(character => character - '1').ToArray();

		if (buttonIndexes.Length == 3 && buttonIndexes.Distinct().Count() == buttonIndexes.Length && buttonIndexes.All(index => index.InRange(0, 2)))
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
		Type festiveJukeboxScriptType = ReflectionHelper.FindType("festiveJukeboxScript");
		if (festiveJukeboxScriptType == null) yield break;

		object component = Module.BombComponent.GetComponent(festiveJukeboxScriptType);

		// If an incorrect answer has already been submitted, we can't solve it without taking a strike.
		if (component.GetValue<bool>("incorrect"))
			yield break;

		string[] chosenLyrics = component.GetValue<string[]>("chosenLyrics");
		var lyricsText = component.GetValue<TextMesh[]>("lyricsText").Select(textMesh => textMesh.text);
		int stage = component.GetValue<int>("stage");
		for (int i = stage; i < 3; i++)
		{
			yield return DoInteractionClick(selectables[lyricsText.IndexOf(text => text == chosenLyrics[i])]);
		}
	}

	private readonly KMSelectable[] selectables;
}
