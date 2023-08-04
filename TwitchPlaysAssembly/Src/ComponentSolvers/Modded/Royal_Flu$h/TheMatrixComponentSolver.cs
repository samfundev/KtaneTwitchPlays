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
		string command = inputCommand.ToLowerInvariant().Trim();
		TimerComponent timerComponent = Module.Bomb.Bomb.GetTimer();

		if (command.StartsWith("flip") || command.StartsWith("jack in"))
		{
			command = command.Replace("flip ", "").Replace("jack in ", "").Replace("for ", "");
			if (!int.TryParse(command, out int output) || output <= 0 || output > 60)
				yield return "sendtochaterror!h Time to stay in the Matrix should be between 1 and 60 seconds.";
			yield return null;

			// The module strikes as soon as the integer difference between the starting time and
			// the current time is equal to the "safe time" it gives. What this actually means is
			// that you have up to a full second less "safe time" than you supposedly do, so we
			// need to compensate for this.
			// We'll flip back when there's about 2/10ths of a second remaining, ideally.
			float flipBackTime = ((int) timerComponent.TimeRemaining - output) + 1.35f;
			int flipBackDisplayedTime = (int) timerComponent.TimeRemaining - output;
			yield return DoInteractionClick(Switch);
			yield return $"sendtochat Jacking in until {string.Format("{0:D2}:{1:D2}", flipBackDisplayedTime / 60, flipBackDisplayedTime % 60)}!";

			while (!CoroutineCanceller.ShouldCancel && timerComponent.TimeRemaining > flipBackTime)
				yield return null;

			// Work around a bug in the module itself; immediately flipping as soon as the
			// switch unlocks causes the coroutine for showing names to not get stopped.
			yield return new WaitUntil(() => !_component.GetValue<bool>("switchLocked"));
			yield return new WaitForSeconds(0.1f);
			yield return DoInteractionClick(Switch);

			// So the proper message shows.
			if (CoroutineCanceller.ShouldCancel)
				yield return "trycancel you left the Matrix early due to a request to cancel";
			yield break;
		}

		command = command.Replace("press ", "").Replace("take ", "");
		KMSelectable correctButton;
		if (command.StartsWith("blue"))
		{
			command = command.Replace("blue ", "").Replace("at ", "").Replace("on ", "");
			correctButton = bluePill;
		}
		else if (command.StartsWith("red"))
		{
			command = command.Replace("red ", "").Replace("at ", "").Replace("on ", "");
			correctButton = redPill;
		}
		else
			yield break;

		if (!int.TryParse(command, out int num) || num < 0 || num > 9)
			yield return "sendtochaterror!h Time to press the pill should be between 0 and 9.";

		yield return null;

		int timeRemaining = (int) timerComponent.TimeRemaining;
		while (timeRemaining % 10 != num)
		{
			yield return null;
			yield return "trycancel Pill was't pressed due to request to cancel.";
			timeRemaining = (int) timerComponent.TimeRemaining;
		}
		yield return DoInteractionClick(correctButton);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		int corTime = _component.GetValue<int>("pressTime");
		TimerComponent timerComponent = Module.Bomb.Bomb.GetTimer();

		if (!_component.GetValue<bool>("outsideTheMatrix")) yield return DoInteractionClick(Switch);
		while ((int) timerComponent.TimeRemaining % 10 != corTime) yield return true;
		if (_component.GetValue<string>("correctPill") == "blue pill") yield return DoInteractionClick(bluePill, 0);
		else yield return DoInteractionClick(redPill, 0);
	}

	private readonly KMSelectable Switch;
	private readonly KMSelectable bluePill;
	private readonly KMSelectable redPill;

	private readonly Component _component;
	private static readonly Type ComponentSolverType = ReflectionHelper.FindType("MatrixScript");
}