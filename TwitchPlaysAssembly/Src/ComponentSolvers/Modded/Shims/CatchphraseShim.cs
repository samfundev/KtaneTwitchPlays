using System;
using System.Collections;
using UnityEngine;

public class CatchphraseShim : ComponentSolverShim
{
	public CatchphraseShim(TwitchModule module) :
		base(module, "catchphrase")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		panels = module.BombComponent.GetComponent(ComponentType).GetValue<KMSelectable[]>("panels");
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		string[] commands = inputCommand.ToLowerInvariant().Trim().SplitFull(' ');
		if (commands.Length == 4 && commands[0] == "panel" && int.TryParse(commands[1], out int panelPosition) && panelPosition.InRange(1, 4) && commands[2] == "at" && int.TryParse(commands[3], out int timerDigit) && timerDigit.InRange(0, 9) && panels[panelPosition - 1].GetComponentInParent<Animator>().GetBool("shrink"))
		{
			yield return $"sendtochaterror Panel {panelPosition} has already been pressed.";
			yield break;
		}

		IEnumerator command = RespondToCommandUnshimmed(inputCommand);
		while (command.MoveNext())
			yield return command.Current;
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("catchphraseScript");

	private readonly KMSelectable[] panels;
}
