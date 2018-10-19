using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class ResistorsComponentSolver : ComponentSolver
{
	public ResistorsComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		object component = bombComponent.GetComponent(ComponentType);
		_pins = (KMSelectable[]) PinsField.GetValue(component);
		_checkButton = (KMSelectable) CheckButtonField.GetValue(component);
		_clearButton = (KMSelectable) ClearButtonField.GetValue(component);
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Connect sets of two pins with !{0} connect a tl tr c. Use !{0} submit to submit and !{0} clear to clear. Valid pins: A B C D TL TR BL BR. Top and Bottom refer to the top and bottom resistor.");
	}

	private static int? PinToIndex(string pin)
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

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] commands = inputCommand.ToLowerInvariant().Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if (commands[0].Equals("connect"))
		{
			IEnumerable<int?> pins = commands.Skip(1).Select(PinToIndex).ToArray();

			if (!pins.Any() || pins.Any(x => x == null) || pins.Count() % 2 == 1)
				yield break;

			IEnumerable<KMSelectable> pinIndices = pins.Where(x => x != null).Select(x => _pins[x.Value]).ToArray();
			
			yield return null;
			foreach (KMSelectable pinSelectable in pinIndices)
			{
				yield return DoInteractionClick(pinSelectable);
			}

			yield break;
		}

		if (commands.Length == 2 && commands[0].EqualsAny("hit", "press", "click"))
			commands = commands.Skip(1).ToArray();

		if (commands.Length != 1) yield break;

		if (commands[0].EqualsAny("check", "submit"))
		{
			yield return null;
			yield return DoInteractionClick(_checkButton);
		}
		else if (commands[0].EqualsAny("clear", "reset"))
		{
			yield return null;
			yield return DoInteractionClick(_clearButton);
		}
	}

	static ResistorsComponentSolver()
	{
		ComponentType = ReflectionHelper.FindType("ResistorsModule");
		PinsField = ComponentType.GetField("pins", BindingFlags.Public | BindingFlags.Instance);
		CheckButtonField = ComponentType.GetField("checkButton", BindingFlags.Public | BindingFlags.Instance);
		ClearButtonField = ComponentType.GetField("clearButton", BindingFlags.Public | BindingFlags.Instance);
	}

	private static readonly Type ComponentType;
	private static readonly FieldInfo PinsField;
	private static readonly FieldInfo CheckButtonField;
	private static readonly FieldInfo ClearButtonField;

	private readonly KMSelectable[] _pins;
	private readonly KMSelectable _checkButton;
	private readonly KMSelectable _clearButton;
}
