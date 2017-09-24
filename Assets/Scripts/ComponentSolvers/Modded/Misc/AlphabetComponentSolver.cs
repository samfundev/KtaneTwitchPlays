using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlphabetComponentSolver : ComponentSolver
{
	public AlphabetComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
		base(bombCommander, bombComponent, ircConnection, canceller)
	{
		_buttons = (KMSelectable[]) _buttonsField.GetValue(bombComponent.GetComponent(_componentType));
	    modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
    }

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		var commands = inputCommand.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		yield return null;

		if (commands.Length >= 2 && commands[0].Equals("submit"))
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
					foreach (string fixedLabel in fixedLabels)
					{
						KMSelectable button = _buttons[buttonLabels.IndexOf(fixedLabel)];
						DoInteractionStart(button);
						DoInteractionEnd(button);

						yield return new WaitForSeconds(0.1f);
					}
				}
			}
		}
	}

	static AlphabetComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("alphabet");
		_buttonsField = _componentType.GetField("Buttons", BindingFlags.Public | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static FieldInfo _buttonsField = null;

	private KMSelectable[] _buttons = null;
}