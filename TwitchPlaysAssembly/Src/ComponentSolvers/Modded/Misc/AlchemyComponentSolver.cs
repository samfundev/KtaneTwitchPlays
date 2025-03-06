using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

[ModuleID("JuckAlchemy")]
public class AlchemyComponentSolver : ComponentSolver
{
	public AlchemyComponentSolver(TwitchModule module) :
		base(module)
	{
		selectables = Module.BombComponent.GetComponent<KMSelectable>().Children;
		SetHelpMessage("Press buttons using !{0} press <buttons>. Runes are specified directionally. Frequencies are specified by full name. Other buttons: redraw, submit and clear.");
	}

	readonly Dictionary<string, int> buttonMap = new Dictionary<string, int>()
	{
		{ "energy", 0 },
		{ "life", 1 },
		{ "mind", 2 },
		{ "flames", 3 },
		{ "matter", 4 },
		{ "clear", 5 },
		{ "cl", 5 },
		{ "br", 6 },
		{ "r", 7 },
		{ "tr", 8 },
		{ "tl", 9 },
		{ "l", 10 },
		{ "bl", 11 },
		{ "s", 12 },
		{ "d", 13 },
		{ "redraw", 13 },
		{ "re-draw", 13 },
		{ "draw", 13 },
		{ "rd", 13 },
	};

	string SimplifyButtonName(string buttonName)
	{
		buttonName = buttonName.Replace("middle", "center");
		foreach (string direction in new[] { "left", "right", "top", "bottom", "center", "centre", "submit" })
			buttonName = buttonName.Replace(direction, direction[0].ToString());

		return Regex.Replace(buttonName, "([lr])([tb])", "$2$1");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = Regex.Replace(inputCommand.ToLowerInvariant().Trim(), "^(press|hit|enter|push)", "");
		string[] split = inputCommand.SplitFull(' ', ',', ';');

		if (split.Length >= 1)
		{
			List<int> buttonIndexes = new List<int>();
			foreach (string name in split)
			{
				if (!buttonMap.TryGetValue(SimplifyButtonName(name), out int index))
					yield break;

				buttonIndexes.Add(index);
			}

			yield return null;
			foreach (int index in buttonIndexes)
				yield return DoInteractionClick(selectables[index]);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		Type alchemyType = ReflectionHelper.FindType("AlchemyScript");
		if (alchemyType == null) yield break;

		object component = Module.BombComponent.GetComponent(alchemyType);
		var frequencies = new[] { "mind", "flames", "matter", "energy", "life" };

		yield return null;
		while (true)
		{
			int correctFreq = component.GetValue<int>("correctFreq");
			int finalFreq = component.GetValue<int>("finalFreq");
			List<int> completeSol = component.GetValue<List<int>>("completeSol");
			int[] nowSymbols = component.GetValue<int[]>("nowSymbols");

			if (correctFreq != -1) yield return RespondToCommandInternal(frequencies[correctFreq]);

			yield return RespondToCommandInternal(completeSol.Select(index =>
			{
				var buttons = new[] { "br", "r", "tr", "tl", "l", "bl", "redraw", "submit" };
				return index > 5 ? buttons[index] : buttons[Array.IndexOf(nowSymbols, index)];
			}).Join());

			if (finalFreq != -1)
			{
				yield return RespondToCommandInternal(frequencies[finalFreq] + " submit");
				break;
			}
		}

		yield return RespondToCommandInternal("submit");
	}

	private readonly KMSelectable[] selectables;
}
