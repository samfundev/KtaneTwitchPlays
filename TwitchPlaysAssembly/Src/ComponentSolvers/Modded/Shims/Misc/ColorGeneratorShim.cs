﻿using System;
using System.Collections;
using UnityEngine;

public class ColorGeneratorShim : ComponentSolverShim
{
	public ColorGeneratorShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		IEnumerator command = RespondToCommandUnshimmed(inputCommand);
		while (command.MoveNext())
		{
			yield return command.Current;
			yield return "trycancel";
		}
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		if (Unshimmed.ForcedSolveMethod == null) yield break;
		var coroutine = (IEnumerator) Unshimmed.ForcedSolveMethod.Invoke(Unshimmed.CommandComponent, null);
		while (coroutine.MoveNext())
			yield return coroutine.Current;
		string ans = _component.GetValue<string>("displayAnswer");
		while (ans != _component.GetValue<TextMesh>("displayText").text)
			yield return true;
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("ColorGeneratorModule", "Color Generator");

	private readonly object _component;
}
