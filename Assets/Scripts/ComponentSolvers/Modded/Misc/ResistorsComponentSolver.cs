using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResistorsComponentSolver : ComponentSolver
{
	public ResistorsComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
		base(bombCommander, bombComponent, ircConnection, canceller)
	{
		object _component = bombComponent.GetComponent(_componentType);
		_pins = (KMSelectable[]) _pinsField.GetValue(_component);
		_checkButton = (KMSelectable) _checkButtonField.GetValue(_component);
		_clearButton = (KMSelectable) _clearButtonField.GetValue(_component);

		helpMessage = "Connect sets of two pins with !{0} connect a tl tr c. Use !{0} submit to submit and !{0} clear to clear. Valid pins: A B C D TL TR BL BR. Top and Bottom refer to the top and bottom resistor.";
	}

	int? PinToIndex(string pin)
	{
		switch (pin)
		{
			case "a":
				return 0;
			case "b":
				return 1;
			case "c":
				return 2;
			case "d":
				return 3;
			case "tl":
			case "topleft":
				return 4;
			case "tr":
			case "topright":
				return 5;
			case "bl":
			case "bottomleft":
				return 6;
			case "br":
			case "bottomright":
				return 7;
			default:
				return null;
		}
	}
	
	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		var commands = inputCommand.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if (commands.Length >= 3 && commands.Length % 2 == 1 && commands[0].Equals("connect"))
		{
			IEnumerable<int?> pins = commands.Where((_, i) => i > 0).Select(pin => PinToIndex(pin));

			if (pins.All(pinIndex => pinIndex != null))
			{
				yield return null;

				foreach (int? pinIndex in pins)
				{
					KMSelectable pinSelectable = _pins[(int) pinIndex];
					DoInteractionClick(pinSelectable);
					yield return new WaitForSeconds(0.1f);
				}
			}
		}
		else if (commands.Length == 1 && (commands[0].Equals("check") || commands[0].Equals("submit")))
		{
			yield return null;

			DoInteractionClick(_checkButton);
			yield return new WaitForSeconds(0.1f);
		}
		else if (commands.Length == 1 && commands[0].Equals("clear"))
		{
			yield return null;

			DoInteractionClick(_clearButton);
			yield return new WaitForSeconds(0.1f);
		}
	}

	static ResistorsComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("ResistorsModule");
		_pinsField = _componentType.GetField("pins", BindingFlags.Public | BindingFlags.Instance);
		_checkButtonField = _componentType.GetField("checkButton", BindingFlags.Public | BindingFlags.Instance);
		_clearButtonField = _componentType.GetField("clearButton", BindingFlags.Public | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static FieldInfo _pinsField = null;
	private static FieldInfo _checkButtonField = null;
	private static FieldInfo _clearButtonField = null;

	private KMSelectable[] _pins = null;
	private KMSelectable _checkButton = null;
	private KMSelectable _clearButton = null;
}
