using System.Collections;
using UnityEngine;

[ModuleID("BlueNeedy")]
[ModuleID("RedNeedy")]
public class BlueRedShim : ComponentSolverShim
{
	public BlueRedShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		IEnumerator command = RespondToCommandUnshimmed(inputCommand);
		while (command.MoveNext())
		{
			if (command.Current is WaitForSeconds)
				yield return "trycancel";
			else
				yield return command.Current;
		}
	}
}