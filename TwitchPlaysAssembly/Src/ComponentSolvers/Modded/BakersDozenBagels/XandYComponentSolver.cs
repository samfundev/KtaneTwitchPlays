using System;
using System.Collections;
using Assets.Scripts.Props;
using UnityEngine;

public class XandYComponentSolver : ComponentSolver
{
	public XandYComponentSolver(TwitchModule module) :
		base(module)
	{
		SetHelpMessage("!alarm help [View the commands for the alarm clock] | !{0} cover [Presses the cover to solve the module if no alarm clock is present]");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (!inputCommand.Equals("cover")) yield break;

		yield return null;
		AlarmClock alarm = UnityEngine.Object.FindObjectOfType<AlarmClock>();
		if (alarm == null)
			yield return DoInteractionClick(Module.Selectable.Children[0], 0);
		else
			yield return "sendtochaterror Command rejected since an alarm clock present.";
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		Type xyType = ReflectionHelper.FindType("XScript");
		if (xyType == null) yield break;

		yield return null;

		AlarmClock alarm = UnityEngine.Object.FindObjectOfType<AlarmClock>();
		if (alarm != null)
		{
			Selectable snooze = alarm.SnoozeButton.GetComponent<Selectable>();
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
		else
			yield return DoInteractionClick(Module.Selectable.Children[0], 0);
	}
}