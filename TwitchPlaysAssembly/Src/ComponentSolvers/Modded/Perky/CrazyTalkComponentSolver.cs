using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class CrazyTalkComponentSolver : ComponentSolver
{
	public CrazyTalkComponentSolver(TwitchModule module) :
		base(module)
	{
		_toggle = (MonoBehaviour) ToggleField.GetValue(module.BombComponent.GetComponent(ComponentType));
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Toggle the switch down and up with !{0} toggle 4 5. The order is down, then up.");
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

	private static readonly Type ComponentType = ReflectionHelper.FindType("CrazyTalkModule");
	private static readonly FieldInfo ToggleField = ComponentType.GetField("toggleSwitch", BindingFlags.Public | BindingFlags.Instance);

	private readonly MonoBehaviour _toggle;
}
