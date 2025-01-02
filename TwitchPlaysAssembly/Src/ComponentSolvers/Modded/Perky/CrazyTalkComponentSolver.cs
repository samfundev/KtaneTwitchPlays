using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class CrazyTalkComponentSolver : ComponentSolver
{
	public CrazyTalkComponentSolver(TwitchModule module) :
		base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_toggle = _component.GetValue<KMSelectable>("toggleSwitch");
		SetHelpMessage("Toggle the switch down and up with !{0} toggle 4 5. The order is down, then up.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] commands = inputCommand.ToLowerInvariant().Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if (commands.Length < 2 || !commands[0].EqualsAny("toggle", "flip", "switch") ||
			commands.Skip(1).Any(x => !int.TryParse(x, out int flipTime) || flipTime < 0 || flipTime > 9))
			yield break;

		yield return "Crazy Talk Solve Attempt";
		TimerComponent timerComponent = Module.Bomb.Bomb.GetTimer();
		int timeRemaining = (int) timerComponent.TimeRemaining;

		foreach (int time in commands.Skip(1).Select(int.Parse))
		{
			while (timeRemaining % 10 != time)
			{
				yield return null;
				yield return "trycancel";
				timeRemaining = (int) timerComponent.TimeRemaining;
			}

			yield return DoInteractionClick(_toggle);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		while (!_component.GetValue<bool>("bActive"))
			yield return true;
		TimerComponent timerComponent = Module.Bomb.Bomb.GetTimer();
		int start = _component.GetValue<int>("mCorrectSwitches");
		for (int i = start; i < 2; i++)
		{
			int digit;
			if (_component.GetValue<bool>("bSwitchState"))
				digit = _component.GetValue<object>("mOption").GetValue<int>("down");
			else
				digit = _component.GetValue<object>("mOption").GetValue<int>("up");
			while ((int) timerComponent.TimeRemaining % 10 != digit)
				yield return true;
			yield return DoInteractionClick(_toggle);
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("CrazyTalkModule");

	private readonly MonoBehaviour _toggle;
	private readonly object _component;
}
