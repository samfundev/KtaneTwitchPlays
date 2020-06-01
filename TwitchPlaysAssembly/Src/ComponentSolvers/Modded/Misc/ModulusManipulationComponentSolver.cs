using System;
using System.Collections;

public class ModulusManipulationComponentSolver : ComponentSolver
{
	public ModulusManipulationComponentSolver(TwitchModule module) :
		base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		selectables = Module.BombComponent.GetComponent<KMSelectable>().Children;
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "!{0} 123 4 [submits 123 when there is 4 minutes left on the clock]");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] split = inputCommand.ToLowerInvariant().SplitFull(" ,;");

		if (split.Length == 2 && int.TryParse(split[0], out int answer) && answer.InRange(0, 999) && int.TryParse(split[1], out int minutes))
		{
			yield return null;

			int[] digits = _component.GetValue<int[]>("digits");
			string answerString = answer.ToString().PadLeft(3, '0');
			for (int i = 0; i < 3; i++)
				yield return SelectIndex(digits[i], answerString[i].ToIndex() + 1, 10, selectables[i], selectables[i + 3]);

			var timer = Module.Bomb.Bomb.GetTimer();
			while (true)
			{
				yield return "trycancel Answer wasn't submitted to due a request to cancel.";

				var minutesLeft = (int) timer.TimeRemaining / 60;
				if (timer.GetRate() > 0 ? minutesLeft <= minutes : minutesLeft >= minutes)
				{
					if (minutesLeft != minutes)
					{
						yield return "sendtochaterror The requested time to submit has passed.";
						yield break;
					}

					break;
				}
			}

			yield return DoInteractionClick(selectables[7]);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		// Based on code from the module.
		// Also a horrific amount of reflection.
		var bombInfo = _component.GetValue<KMBombInfo>("bombInfo");
		var aaBatteryCount = _component.GetValue<int>("aaBatteryCount");
		var dBatteryCount = _component.GetValue<int>("dBatteryCount");
		var batteryCount = aaBatteryCount + dBatteryCount;
		var serialNumberSpecial = _component.GetValue<bool>("serialNumberSpecial");
		var serialLetterSpecial = _component.GetValue<bool>("serialLetterSpecial");
		var serialVowel = _component.GetValue<bool>("serialVowel");
		var serialLastDigitEven = _component.GetValue<bool>("serialLastDigitEven");
		var litIndicatorCount = _component.GetValue<int>("litIndicatorCount");
		var unlitIndicatorCount = _component.GetValue<int>("unlitIndicatorCount");
		var containsSpecificPorts = _component.GetValue<bool>("containsSpecificPorts");

		int finalSolution = _component.GetValue<int>("startingNumber");
		int otherModsRemainingCount = bombInfo.GetSolvableModuleNames().Count - bombInfo.GetSolvedModuleNames().Count - 1;
		int strikeCount = bombInfo.GetStrikes();
		bool minutesRemainingIsEven = (int) bombInfo.GetTime() / 60 % 2 == 0;

		if (otherModsRemainingCount % 5 == 0)
		{
			if (batteryCount > 1)
				finalSolution += 400;

			if (serialNumberSpecial)
				finalSolution -= 40;
		}

		if (otherModsRemainingCount % 4 == 0)
		{
			if (aaBatteryCount >= 1 && dBatteryCount >= 1)
				finalSolution *= 2;

			if (serialLetterSpecial)
				finalSolution -= 290;
		}

		if (otherModsRemainingCount % 3 == 0)
		{
			if (batteryCount > 3)
				finalSolution -= 160;

			if (litIndicatorCount > unlitIndicatorCount)
				finalSolution += 75;
		}

		if (otherModsRemainingCount % 2 == 0)
		{
			if (serialVowel)
				finalSolution += 340;

			if (containsSpecificPorts)
				finalSolution += 180;
		}

		if (strikeCount >= 1)
			finalSolution -= 45;

		if (unlitIndicatorCount > 0)
			finalSolution -= 15;

		if (serialLastDigitEven)
			finalSolution += 150;

		if (minutesRemainingIsEven)
			finalSolution += 6;

		if (finalSolution < 0)
			finalSolution = 0;
		else
			finalSolution %= 1000;

		var targetMinutes = (int) bombInfo.GetTime() / 60;
		yield return RespondToCommandInternal($"{finalSolution} {targetMinutes}");

		// If we missed submitting the module at the time we wanted to, attempt submitting again.
		if (targetMinutes != (int) bombInfo.GetTime() / 60)
			yield return ForcedSolveIEnumerator();
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("ModulusManipulation");

	private readonly object _component;
	private readonly KMSelectable[] selectables;
}
