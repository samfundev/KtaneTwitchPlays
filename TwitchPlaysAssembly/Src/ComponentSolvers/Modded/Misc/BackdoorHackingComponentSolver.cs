using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

public class BackdoorHackingComponentSolver : CommandComponentSolver
{
	public BackdoorHackingComponentSolver(TwitchModule module) :
		base(module, "BackdoorHacking", "!{0} reconnect [Presses the reconnect button] | !{0} buy <1-3> [Presses the specified buy button from top to bottom] | !{0} _ [Presses the spacebar] | !{0} qwerty [Presses keys on the keyboard]")
	{
		UsableKeysValue = _component.GetValue<string>("TheLetters");
	}

	private IEnumerator Reconnect(CommandParser _)
	{
		_.Literal("reconnect");

		yield return null;
		yield return Click(0);
	}

	private IEnumerator Buy(CommandParser _)
	{
		_.Literal("buy");
		_.Integer(out int index, 1, 3);

		yield return null;
		yield return Click(index - 1);
	}

	private IEnumerator AnyKeyPress(CommandParser _)
	{
		_.Regex("[a-z0-9_<^>/]+", out Match match);
		string input = match.Groups[0].Value.ToUpper();

		yield return null;
		foreach (char key in input)
		{
			if (_component.GetValue<int>("CurrentState") == 2 && key == '_')
				ComponentType.CallMethod("ZoneWallPress", _component);
			else if (_component.GetValue<int>("CurrentState") == 3)
				ComponentType.CallMethod("MemoryFraggerPress", _component, UsableKeysValue.IndexOf(key));
			else if (_component.GetValue<int>("CurrentState") == 4)
				ComponentType.CallMethod("NodeHackerPress", _component, UsableKeysValue.IndexOf(key));
			else if (_component.GetValue<int>("CurrentState") == 5 && (key == '^' || key == 'W') && _component.GetValue<int>("ActualSelection") > 4)
				ComponentType.CallMethod("StackPusherUp", _component);
			else if (_component.GetValue<int>("CurrentState") == 5 && (key == '<' || key == 'A') && _component.GetValue<int>("ActualSelection") % 5 != 0)
				ComponentType.CallMethod("StackPusherLeft", _component);
			else if (_component.GetValue<int>("CurrentState") == 5 && (key == '/' || key == 'S') && _component.GetValue<int>("ActualSelection") < 20)
				ComponentType.CallMethod("StackPusherDown", _component);
			else if (_component.GetValue<int>("CurrentState") == 5 && (key == '>' || key == 'D') && _component.GetValue<int>("ActualSelection") % 5 != 4)
				ComponentType.CallMethod("StackPusherRight", _component);
			else if (_component.GetValue<int>("CurrentState") == 5 && key == '_')
				ComponentType.CallMethod("StackPusherSpace", _component);
			yield return new WaitForSeconds(.1f);
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("BackdoorHacking");

	private string UsableKeysValue;
}