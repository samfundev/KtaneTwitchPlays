using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class ResistorsComponentSolver : ComponentSolver
{
	public ResistorsComponentSolver(TwitchModule module) :
		base(module)
	{
		object component = module.BombComponent.GetComponent(ComponentType);
		_pins = (KMSelectable[]) PinsField.GetValue(component);
		_checkButton = (KMSelectable) CheckButtonField.GetValue(component);
		_clearButton = (KMSelectable) ClearButtonField.GetValue(component);
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Connect sets of two pins with !{0} connect a tl tr c. Use !{0} submit to submit and !{0} clear to clear. Valid pins: A B C D TL TR BL BR. Top and Bottom refer to the top and bottom resistor.");
	}

	private static int? PinToIndex(string pin)
	{
		return pin switch
		{
			"a" => 0,
			"b" => 1,
			"c" => 2,
			"d" => 3,
			"tl" or "topleft" => 4,
			"tr" or "topright" => 5,
			"bl" or "bottomleft" => 6,
			"br" or "bottomright" => 7,
			_ => null,
		};
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

	private static readonly Type ComponentType = ReflectionHelper.FindType("ResistorsModule");
	private static readonly FieldInfo PinsField = ComponentType.GetField("pins", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo CheckButtonField = ComponentType.GetField("checkButton", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo ClearButtonField = ComponentType.GetField("clearButton", BindingFlags.Public | BindingFlags.Instance);

	private readonly KMSelectable[] _pins;
	private readonly KMSelectable _checkButton;
	private readonly KMSelectable _clearButton;
}
