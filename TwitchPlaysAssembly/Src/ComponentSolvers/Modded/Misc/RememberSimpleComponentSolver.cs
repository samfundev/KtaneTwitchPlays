using System;
using System.Collections;

public class RememberSimpleComponentSolver : ReflectionComponentSolver
{
	int solvedModules = 0;

	public RememberSimpleComponentSolver(TwitchModule module)
		: base(module, "SimpleModuleScript", "Use !{0} press to press the button.")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (command.Trim().ToLowerInvariant() == "press")
		{
			yield return null;
			yield return Click(0);
		}
	}

	//there is possibly an extremely edge case here which may cause a strike, if multiple modules are sovled at the same time, and not enough time is given for the force solve ienumerator to continue, this may strike, however this is unlikely
	protected override IEnumerator ForcedSolveIEnumerator()
	{
		while (!Module.BombComponent.IsSolved)
		{
			if (solvedModules != Module.Bomb.BombSolvedModules)
			{
				yield return null;
				yield return Click(0);
			}
			yield return true;
		}
	}
}
