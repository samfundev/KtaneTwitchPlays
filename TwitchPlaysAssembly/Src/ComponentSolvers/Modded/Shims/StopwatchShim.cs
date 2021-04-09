using System;
using System.Collections;
using UnityEngine;

public class StopwatchShim : ComponentSolverShim
{
	public StopwatchShim(TwitchModule module)
		: base(module, "stopwatch")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_startButton = _component.GetValue<KMSelectable>("startButton");
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		yield return RespondToCommandUnshimmed(inputCommand);
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		if (Mathf.FloorToInt(_component.GetValue<float>("totalElapsedTime")) > _component.GetValue<int>("correctWaitTime"))
		{
			((MonoBehaviour) _component).StopAllCoroutines();
			yield break;
		}
		Module.BombComponent.StartCoroutine(HandleSolve());
		while (!_finished) yield return true;
	}

	IEnumerator HandleSolve()
	{
		while (_component.GetValue<bool>("buttonLock"))
			yield return null;
		if (!_component.GetValue<bool>("clockOn"))
			yield return DoInteractionClick(_startButton, 0);
		while (Mathf.FloorToInt(_component.GetValue<float>("totalElapsedTime")) != _component.GetValue<int>("correctWaitTime"))
			yield return null;
		yield return DoInteractionClick(_startButton, 0);
		_finished = true;
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("stopwatchScript", "stopwatch");

	private readonly object _component;

	private readonly KMSelectable _startButton;
	private bool _finished;
}
