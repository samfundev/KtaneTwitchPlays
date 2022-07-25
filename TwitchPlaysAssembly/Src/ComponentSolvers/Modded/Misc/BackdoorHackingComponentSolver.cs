using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class BackdoorHackingComponentSolver : CommandComponentSolver
{
	public BackdoorHackingComponentSolver(TwitchModule module) :
		base(module, "BackdoorHacking", "!{0} reconnect [Presses the reconnect button] | !{0} buy <1-3> [Presses the specified buy button from top to bottom] | !{0} _ [Presses the spacebar] | !{0} qwerty [Presses keys on the keyboard]")
	{
		_reconnect = (KMSelectable) ReconnectField.GetValue(_component);
		_buy = (KMSelectable[]) BuyField.GetValue(_component);
	}

	private IEnumerator Reconnect(CommandParser _)
	{
		_.Literal("reconnect");

		yield return null;
		yield return DoInteractionClick(_reconnect);
	}

	private IEnumerator Buy(CommandParser _)
	{
		_.Literal("buy");
		_.Integer(out int index, 1, 3);

		yield return null;
		yield return DoInteractionClick(_buy[index - 1]);
	}

	private IEnumerator AnyKeyPress(CommandParser _)
	{
		_.Regex("[a-z0-9_<^>/]+", out Match match);
		string input = match.Groups[0].Value.ToUpper();

		yield return null;
		foreach (char key in input)
		{
			if (StateValue == 2 && key == '_')
				ComponentType.CallMethod("ZoneWallPress", _component);
			else if (StateValue == 3)
				ComponentType.CallMethod("MemoryFraggerPress", _component, UsableKeysValue.IndexOf(key));
			else if (StateValue == 4)
				ComponentType.CallMethod("NodeHackerPress", _component, UsableKeysValue.IndexOf(key));
			else if (StateValue == 5 && (key == '^' || key == 'W') && SelectionValue > 4)
				ComponentType.CallMethod("StackPusherUp", _component);
			else if (StateValue == 5 && (key == '<' || key == 'A') && SelectionValue % 5 != 0)
				ComponentType.CallMethod("StackPusherLeft", _component);
			else if (StateValue == 5 && (key == '/' || key == 'S') && SelectionValue < 20)
				ComponentType.CallMethod("StackPusherDown", _component);
			else if (StateValue == 5 && (key == '>' || key == 'D') && SelectionValue % 5 != 4)
				ComponentType.CallMethod("StackPusherRight", _component);
			else if (StateValue == 5 && key == '_')
				ComponentType.CallMethod("StackPusherSpace", _component);
			yield return new WaitForSeconds(.1f);
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("BackdoorHacking");
	private static readonly FieldInfo ReconnectField = ComponentType.GetField("Button", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo BuyField = ComponentType.GetField("BuyButtons", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo StateField = ComponentType.GetField("CurrentState", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo SelectionField = ComponentType.GetField("ActualSelection", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo UsableKeysField = ComponentType.GetField("TheLetters", BindingFlags.NonPublic | BindingFlags.Instance);

	private readonly KMSelectable _reconnect;
	private readonly KMSelectable[] _buy;

	private int StateValue => (int) StateField.GetValue(_component);
	private int SelectionValue => (int) SelectionField.GetValue(_component);
	private string UsableKeysValue => (string) UsableKeysField.GetValue(_component);
}