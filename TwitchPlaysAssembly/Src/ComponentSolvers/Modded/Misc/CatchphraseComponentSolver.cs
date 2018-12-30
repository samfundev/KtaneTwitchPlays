using System;
using System.Collections;
using UnityEngine;

public class CatchphraseComponentSolver : ComponentSolver
{
	public CatchphraseComponentSolver(TwitchModule module) :
		base(module)
	{
		selectables = Module.BombComponent.GetComponent<KMSelectable>().Children;
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Press a panel at a specific digit using !{0} panel 2 at 8. Panels are in english reading order. Submit a number using !{0} submit 480.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] commands = inputCommand.ToLowerInvariant().Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if (commands.Length == 2 && commands[0] == "submit" && commands[1].Length <= 5 && commands[1].TryParseInt() != null)
		{
			yield return null;

			foreach (char digit in commands[1])
				yield return DoInteractionClick(selectables[digit != '0' ? digit - '1' + 4 : 13]);

			yield return DoInteractionClick(selectables[15]);
		}
		else if (commands.Length == 4 && commands[0] == "panel" && int.TryParse(commands[1], out int panelPosition) && panelPosition.InRange(1, 4) && commands[2] == "at" && int.TryParse(commands[3], out int timerDigit) && timerDigit.InRange(0, 9))
		{
			yield return null;

			TimerComponent timerComponent = Module.Bomb.Bomb.GetTimer();
			while (Mathf.RoundToInt(timerComponent.TimeRemaining) % 10 != timerDigit)
				yield return "trycancel The panel was not opened due to a request to cancel.";

			yield return DoInteractionClick(selectables[panelPosition - 1]);
		}
	}

	private readonly KMSelectable[] selectables;
}
