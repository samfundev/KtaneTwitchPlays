using System;
using System.Collections;
using UnityEngine;

public class TheMatrixComponentSolver : ComponentSolver
{
	public TheMatrixComponentSolver(TwitchModule module) :
		base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentSolverType);
		Switch = _component.GetValue<KMSelectable>("switchObject");
		bluePill = _component.GetValue<KMSelectable>("bluePill");
		redPill = _component.GetValue<KMSelectable>("redPill");
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Flip the switch and flip it again in x seconds with '!{0} flip for x'! Use '!{0} press <colour>' at # to press the pill corresponding to the colour you specified when the last digit of the timer equals to #");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string command = inputCommand.ToLowerInvariant();
		if (command.StartsWith("flip"))
		{
			command=command.Replace("flip ","").Replace("for ","");
			if (!int.TryParse(command, out int output))
			{
				yield return "sendtochaterror Number not valid!";
				yield break;
			}
			yield return null;
			yield return DoInteractionClick(Switch);
			yield return new WaitForSeconds((float) output-0.01f);
			yield return DoInteractionClick(Switch);
			yield break;
		}

		TimerComponent timerComponent = Module.Bomb.Bomb.GetTimer();
		int timeRemaining = (int) timerComponent.TimeRemaining;
		command = command.Replace("press ", "");
		KMSelectable correctButton;
		if (command.StartsWith("blue"))
		{
			command = command.Replace("blue ", "").Replace("at ", "");
			correctButton = bluePill;
		}
		else if (command.StartsWith("red"))
		{
			command = command.Replace("red ", "").Replace("at ", "");
			correctButton = redPill;
		}
		else
		{
			yield return "sendtochaterror Colour not valid!";
			yield break;
		}

		if (!int.TryParse(command, out int num))
		{
			yield return "sendtochaterror Number not valid!";
			yield break;
		}

		yield return null;
		while (timeRemaining % 10 != num)
		{
			yield return null;
			yield return "trycancel Pill was't pressed due to request to cancel.";
			timeRemaining = (int) timerComponent.TimeRemaining;
		}
		yield return DoInteractionClick(correctButton);
	}

	private KMSelectable Switch;
	private KMSelectable bluePill;
	private KMSelectable redPill;

	private readonly Component _component;
	private static readonly Type ComponentSolverType = ReflectionHelper.FindType("MatrixScript");
}