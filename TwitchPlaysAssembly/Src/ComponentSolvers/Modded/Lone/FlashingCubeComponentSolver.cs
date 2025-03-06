using System.Collections;

[ModuleID("flashingCube")]
public class FlashingCubeComponentSolver : ReflectionComponentSolver
{
	public FlashingCubeComponentSolver(TwitchModule module) :
		base(module, "flashingCube", "!{0} press <top/left/front/right/bottom> [Presses the specified face of the cube] | Faces can be simplified to their first letter | Presses can be chained using spaces, commas, or semicolons")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length < 2 || !command.StartsWith("press ")) yield break;
		for (int i = 1; i < split.Length; i++)
		{
			if (!split[i].EqualsAny("top", "t", "left", "l", "front", "f", "right", "r", "bottom", "b")) yield break;
		}

		yield return null;
		for (int i = 1; i < split.Length; i++)
		{
			switch (split[i])
			{
				case "top":
				case "t":
					yield return Click(3);
					break;
				case "left":
				case "l":
					yield return Click(4);
					break;
				case "front":
				case "f":
					yield return Click(0);
					break;
				case "right":
				case "r":
					yield return Click(2);
					break;
				case "bottom":
				case "b":
					yield return Click(1);
					break;
			}
		}
	}
}