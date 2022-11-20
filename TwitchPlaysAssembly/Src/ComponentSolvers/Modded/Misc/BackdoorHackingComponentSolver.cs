using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

public class BackdoorHackingComponentSolver : CommandComponentSolver
{
	public BackdoorHackingComponentSolver(TwitchModule module) :
		base(module, "BackdoorHacking", "!{0} reconnect [Presses the reconnect button] | !{0} buy <1-3> [Presses the specified buy button from top to bottom] | !{0} _ [Presses the spacebar] | !{0} qwerty [Presses keys on the keyboard]")
	{
		UsableKeysValue = _component.GetValue<string>("TheLetters");
		if (module.Bomb != null)
			module.Bomb.BackdoorComponent = _component;
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
		yield return Click(index);
	}

	private IEnumerator AnyKeyPress(CommandParser _)
	{
		_.Regex("[a-z0-9_<^>/]+", out Match match);
		string input = match.Groups[0].Value.ToUpper().Replace("_", " ");

		yield return null;
		foreach (char key in input)
		{
			int state = _component.GetValue<int>("CurrentState");
			if (state == 2 && key == ' ')
				_component.CallMethod("ZoneWallPress");
			else if (state == 3)
				_component.CallMethod("MemoryFraggerPress", UsableKeysValue.IndexOf(key));
			else if (state == 4)
				_component.CallMethod("NodeHackerPress", UsableKeysValue.IndexOf(key));
			else if (state == 5 && key.EqualsAny('^', 'W') && _component.GetValue<int>("ActualSelection") > 4)
				_component.CallMethod("StackPusherUp");
			else if (state == 5 && key.EqualsAny('<', 'A') && _component.GetValue<int>("ActualSelection") % 5 != 0)
				_component.CallMethod("StackPusherLeft");
			else if (state == 5 && key.EqualsAny('/', 'S') && _component.GetValue<int>("ActualSelection") < 20)
				_component.CallMethod("StackPusherDown");
			else if (state == 5 && key.EqualsAny('>', 'D') && _component.GetValue<int>("ActualSelection") % 5 != 4)
				_component.CallMethod("StackPusherRight");
			else if (state == 5 && key == ' ')
				_component.CallMethod("StackPusherSpace");
			yield return new WaitForSeconds(.1f);
		}
	}

	private readonly string UsableKeysValue;
}