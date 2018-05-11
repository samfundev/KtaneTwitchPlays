using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class CrazyTalkComponentSolver : ComponentSolver
{
	public CrazyTalkComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		_toggle = (MonoBehaviour)_toggleField.GetValue(bombComponent.GetComponent(_componentType));
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Toggle the switch down and up with !{0} toggle 4 5. The order is down, then up.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		var commands = inputCommand.ToLowerInvariant().Trim().Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

		if (commands.Length != 3 || !commands[0].EqualsAny("toggle", "flip", "switch") ||
			!int.TryParse(commands[1], out int downtime) || !int.TryParse(commands[2], out int uptime))
			yield break;

		if (downtime < 0 || downtime > 9 || uptime < 0 || uptime > 9)
			yield break;

		yield return "Crazy Talk Solve Attempt";
		TimerComponent timerComponent = BombCommander.Bomb.GetTimer();
		int timeRemaining = (int)(timerComponent.TimeRemaining);

		while ((timeRemaining%10) != downtime)
		{
			yield return null;
			timeRemaining = (int)(timerComponent.TimeRemaining);
		}
		yield return DoInteractionClick(_toggle);

		while ((timeRemaining % 10) != uptime)
		{
			yield return null;
			timeRemaining = (int)(timerComponent.TimeRemaining);
		}
		yield return DoInteractionClick(_toggle);
	}

	static CrazyTalkComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("CrazyTalkModule");
		_toggleField = _componentType.GetField("toggleSwitch", BindingFlags.Public | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static FieldInfo _toggleField = null;

	private readonly MonoBehaviour _toggle = null;
}
