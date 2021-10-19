using System;
using System.Collections;
using Assets.Scripts.Props;
using UnityEngine;

public class XandYComponentSolver : ComponentSolver
{
	public XandYComponentSolver(TwitchModule module) :
		base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "!alarm snooze <#> [Press the snooze button '#' times]");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		yield return null;
		yield return "sendtochaterror Please use the !alarm snooze command to interact with this module";
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		Type xyType = ReflectionHelper.FindType("XScript");
		if (xyType == null) yield break;

		FloatingHoldable alarm = null;
		foreach (var holdable in UnityEngine.Object.FindObjectsOfType<FloatingHoldable>())
		{
			if (holdable.GetComponent<AlarmClock>() != null)
				alarm = holdable;
		}
		if (alarm == null) yield break;

		yield return null;

		Selectable snooze = alarm.GetComponent<AlarmClock>().SnoozeButton.GetComponent<Selectable>();
		object component = Module.BombComponent.GetComponent(xyType);

		int answer = 11 + component.GetValue<int>("val");
		int current = component.GetValue<int>("currentCount");

		if (current > answer)
		{
			((MonoBehaviour) component).StopAllCoroutines();
			yield break;
		}
		while (current < answer)
		{
			snooze.Trigger();
			current++;
			yield return new WaitForSeconds(.1f);
		}
		while (!Module.BombComponent.IsSolved) yield return null;
	}
}