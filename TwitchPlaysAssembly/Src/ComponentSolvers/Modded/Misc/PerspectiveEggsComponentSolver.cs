using System.Collections;
using System.Collections.Generic;

[ModuleID("bigeggs")]
public class PerspectiveEggsComponentSolver : ReflectionComponentSolver
{
	public PerspectiveEggsComponentSolver(TwitchModule module) :
		base(module, "EggSaladScript", "!{0} press <red/eggshell/green/blue/yellow> [Presses the egg with the specified color] | Colors can be simplified to their first letter | Presses can be chained using spaces, commas, or semicolons")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length < 2 || !command.StartsWith("press ")) yield break;
		for (int i = 1; i < split.Length; i++)
		{
			if (!split[i].EqualsAny("red", "r", "eggshell", "e", "green", "g", "blue", "b", "yellow", "y")) yield break;
		}

		yield return null;
		IList comparer = _component.GetValue<IList>("Comparer");
		for (int i = 1; i < split.Length; i++)
		{
			for (int j = 0; j < 5; j++)
			{
				if (((string) comparer[j])[0].Equals(split[i][0]))
				{
					yield return Click(j);
					break;
				}
			}
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		Queue<int> positions = _component.GetValue<Queue<int>>("Object");
		while (!_component.GetValue<bool>("Version"))
			yield return Click(positions.Peek());
	}
}