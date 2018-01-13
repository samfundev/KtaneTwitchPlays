using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlphabetComponentSolver : ComponentSolver
{
	public AlphabetComponentSolver(BombCommander bombCommander, BombComponent bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
		base(bombCommander, bombComponent, ircConnection, canceller)
	{
		_buttons = bombComponent.GetComponent<KMSelectable>().Children;
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
    }

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		var commands = inputCommand.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if (commands.Length >= 2 && commands[0].EqualsAny("submit", "press"))
		{
			List<string> buttonLabels = _buttons.Select(button => button.GetComponentInChildren<TextMesh>().text.ToLowerInvariant()).ToList();

			if (!buttonLabels.Any(label => label == " "))
			{
				IEnumerable<string> submittedText = commands.Where((_, i) => i > 0);
				List<string> fixedLabels = new List<string>();
				foreach (string text in submittedText)
				{
					if (buttonLabels.Any(label => label.Equals(text)))
 					{
 						fixedLabels.Add(text);
 					}
				}

				if (fixedLabels.Count == submittedText.Count())
				{
					yield return null;

					foreach (string fixedLabel in fixedLabels)
					{
						yield return DoInteractionClick(_buttons[buttonLabels.IndexOf(fixedLabel)]);
					}
				}
			}
		}
	}

	private KMSelectable[] _buttons = null;
}