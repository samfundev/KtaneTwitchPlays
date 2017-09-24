using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NumberPadComponentSolver : ComponentSolver
{
	public NumberPadComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
		base(bombCommander, bombComponent, ircConnection, canceller)
	{
		_buttons = (KMSelectable[]) _buttonsField.GetValue(bombComponent.GetComponent(_componentType));
		helpMessage = "Submit your anwser with !{0} submit 4236.";
	}

	int? ButtonToIndex(string button)
	{
		switch (button)
		{
			case "enter":
				return 0;
			case "0":
				return 1;
			case "clear":
				return 2;
			default:
				int i;
				if (int.TryParse(button, out i))
				{
					return i + 2;
				}
				else
				{
					return null;
				}
		}
	}
	
	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		var commands = inputCommand.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if (commands.Length == 2 && commands[0].Equals("submit"))
		{
			List<string> buttons = commands[1].Select(c => c.ToString()).ToList();

			if (buttons.Count() == 4 && buttons.All(num => ButtonToIndex(num) != null))
			{
				yield return null;

				buttons.Insert(0, "clear");
				buttons.Add("enter");
				foreach (string button in buttons)
				{
					int? buttonIndex = ButtonToIndex(button);
					if (buttonIndex != null)
					{
						KMSelectable buttonSelectable = _buttons[(int) buttonIndex];

						DoInteractionClick(buttonSelectable);
						yield return new WaitForSeconds(0.1f);
					}
				}
			}
		}
	}

	static NumberPadComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("NumberPadModule");
		_buttonsField = _componentType.GetField("buttons", BindingFlags.Public | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static FieldInfo _buttonsField = null;

	private KMSelectable[] _buttons = null;
}
