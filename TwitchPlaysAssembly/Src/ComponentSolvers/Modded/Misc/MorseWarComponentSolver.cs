using System;
using System.Collections;
using System.Text.RegularExpressions;

[ModuleID("MorseWar")]
public class MorseWarComponentSolver : ComponentSolver
{
	public MorseWarComponentSolver(TwitchModule module) :
		base(module)
	{
		selectables = Module.BombComponent.GetComponent<KMSelectable>().Children;
		SetHelpMessage("Send supply ship (S) or a submarine (U) using: !{0} press SUSU.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = Regex.Replace(inputCommand.ToLowerInvariant().Trim(), "^(press|hit|enter|push) ", "");

		if (!inputCommand.RegexMatch("^[su]+$")) yield break;

		yield return null;
		foreach (char character in inputCommand)
		{
			yield return DoInteractionClick(selectables[character == 'u' ? 0 : 1]);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		Type morseWarType = ReflectionHelper.FindType("MorseWar");
		if (morseWarType == null) yield break;

		object component = Module.BombComponent.GetComponent(morseWarType);

		yield return null;
		yield return RespondToCommandInternal(component.GetValue<string>("solution"));
	}

	private readonly KMSelectable[] selectables;
}
