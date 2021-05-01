using System;
using System.Collections;
using UnityEngine;

public class PlungerButtonShim : ComponentSolverShim
{
	public PlungerButtonShim(TwitchModule module) :
		base(module, "plungerButton")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_button = _component.GetValue<KMSelectable>("button");
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		IEnumerator command = RespondToCommandUnshimmed(inputCommand);
		while (command.MoveNext())
			yield return command.Current;
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		TimerComponent timerComponent = Module.Bomb.Bomb.GetTimer();
		float pressTime = _component.GetValue<float>("timeOfPress");
		pressTime = Mathf.FloorToInt(pressTime);
		if (!_component.GetValue<bool>("pressed"))
		{
			while ((int) timerComponent.TimeRemaining % 10 != _component.GetValue<int>("targetPressTime"))
				yield return true;
			DoInteractionStart(_button);
			pressTime = _component.GetValue<float>("timeOfPress");
			pressTime = Mathf.FloorToInt(pressTime);
		}
		else if (pressTime != _component.GetValue<int>("targetPressTime"))
		{
			((MonoBehaviour) _component).StopAllCoroutines();
			yield break;
		}
		while ((int) timerComponent.TimeRemaining % 10 != _component.GetValue<int>("targetReleaseTime"))
		{
			if (pressTime != _component.GetValue<int>("targetPressTime"))
			{
				((MonoBehaviour) _component).StopAllCoroutines();
				yield break;
			}
			yield return null;
		}
		DoInteractionEnd(_button);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("plungerButtonScript", "plungerButton");

	private readonly object _component;
	private readonly KMSelectable _button;
}
