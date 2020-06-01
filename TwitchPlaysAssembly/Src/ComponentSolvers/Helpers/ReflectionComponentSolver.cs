using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class ReflectionComponentSolver : ComponentSolver
{
	protected ReflectionComponentSolver(TwitchModule module, string componentString, string helpMessage) :
		base(module)
	{
		if (!componentTypes.ContainsKey(componentString))
			componentTypes[componentString] = ReflectionHelper.FindType(componentString);

		var componentType = componentTypes[componentString];

		_component = module.BombComponent.GetComponent(componentType);
		selectables = Module.BombComponent.GetComponent<KMSelectable>().Children;
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), helpMessage);
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.ToLowerInvariant().Trim();
		string[] split = inputCommand.SplitFull(" ,;");

		var enumerator = Respond(split, inputCommand);
		while (enumerator.MoveNext())
			yield return enumerator.Current;
	}

	public abstract IEnumerator Respond(string[] split, string command);

	protected static Dictionary<string, Type> componentTypes = new Dictionary<string, Type>();

	protected readonly object _component;
	protected readonly KMSelectable[] selectables;

	// Helper methods
	protected WaitForSeconds Click(int index, float delay = 0.1f) => DoInteractionClick(selectables[index], delay);
	protected void LogSelectables() => DebugHelper.Log($"Selectables:\n{selectables.Select((selectable, index) => $"{index} = {selectable.name}").Join("\n")}");
}