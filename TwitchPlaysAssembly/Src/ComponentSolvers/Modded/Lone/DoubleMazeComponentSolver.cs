using System.Collections;

[ModuleID("doubleMaze")]
public class DoubleMazeComponentSolver : ReflectionComponentSolver
{
	public DoubleMazeComponentSolver(TwitchModule module) :
		base(module, "doubleMaze", "!{0} press <up/down/left/right/clockwise/counter-clockwise/flip> [Presses an arrow button that does the specified action] | Actions can be simplified to their first letter except for rotations which are clock/cw and counter/ccw | Presses can be chained using spaces, commas, or semicolons")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length < 2 || !command.StartsWith("press ")) yield break;
		for (int i = 1; i < split.Length; i++)
		{
			if (!split[i].EqualsAny("up", "u", "down", "d", "left", "l", "right", "r", "clockwise", "clock", "cw", "counter-clockwise", "counter", "ccw", "flip", "f")) yield break;
		}

		yield return null;
		for (int i = 1; i < split.Length; i++)
		{
			while (!_component.GetValue<bool>("interactable")) yield return "trycancel";
			switch (split[i])
			{
				case "flip":
				case "f":
					yield return Click(0, 0);
					break;
				case "left":
				case "l":
					yield return Click(1, 0);
					break;
				case "up":
				case "u":
					yield return Click(2, 0);
					break;
				case "right":
				case "r":
					yield return Click(3, 0);
					break;
				case "down":
				case "d":
					yield return Click(4, 0);
					break;
				case "clockwise":
				case "clock":
				case "cw":
					yield return Click(5, 0);
					break;
				default:
					yield return Click(6, 0);
					break;
			}
		}
	}
}