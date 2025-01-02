using System;
using System.Collections;
using System.Text.RegularExpressions;
using KModkit;
using UnityEngine;

public class LetterKeysComponentSolver : ComponentSolver
{
	public LetterKeysComponentSolver(TwitchModule module) :
		base(module)
	{
		_buttons = module.BombComponent.GetComponent<KMSelectable>().Children;
		SetHelpMessage("!{0} press b");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (!inputCommand.StartsWith("press ", StringComparison.InvariantCultureIgnoreCase)) yield break;
		Match match = Regex.Match(inputCommand, "[1-4a-d]", RegexOptions.IgnoreCase);
		if (!match.Success)
			yield break;

		if (int.TryParse(match.Value, out int buttonID))
		{
			yield return null;
			yield return DoInteractionClick(_buttons[buttonID - 1]);
			yield break;
		}

		foreach (KMSelectable button in _buttons)
		{
			if (!match.Value.Equals(button.GetComponentInChildren<TextMesh>().text,
				StringComparison.InvariantCultureIgnoreCase)) continue;
			yield return null;
			yield return DoInteractionClick(button);
			yield break;
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		Type letterKeysType = ReflectionHelper.FindType("LetterKeys");
		if (letterKeysType == null) yield break;

		object component = Module.BombComponent.GetComponent(letterKeysType);
		var edgework = Module.BombComponent.GetComponent<KMBombInfo>();

		yield return RespondToCommandInternal("press " + component.CallMethod<string>("getCorrectButton", edgework.GetBatteryCount(), edgework.GetSerialNumber()));
	}

	private readonly KMSelectable[] _buttons;
}