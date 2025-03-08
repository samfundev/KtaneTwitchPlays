using System.Collections;
using UnityEngine;

[ModuleID("BackdoorHacking")]
public class BackdoorHackingComponentSolver : CommandComponentSolver
{
	public BackdoorHackingComponentSolver(TwitchModule module) :
		base(module, "BackdoorHacking", "!{0} reconnect [Presses the reconnect button] | !{0} buy <1-3> [Presses the specified buy button from top to bottom] | !{0} _ [Presses the spacebar] | !{0} qwerty [Presses keys on the keyboard]")
	{
		UsableKeysValue = _component.GetValue<string>("TheLetters");
		if (module.Bomb != null)
			module.Bomb.BackdoorComponent = _component;
	}

	[Command("reconnect")]
	private IEnumerator Reconnect()
	{
		yield return null;
		yield return Click(0);
	}

	[Command("buy ([1-3])")]
	private IEnumerator Buy(int index)
	{
		yield return null;
		yield return Click(index);
	}

	[Command("([a-z0-9_<^>/]+)")]
	private IEnumerator AnyKeyPress(string value)
	{
		string input = value.ToUpper().Replace("_", " ");

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