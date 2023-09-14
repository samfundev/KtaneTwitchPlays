using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class ReflectionComponentSolver : ComponentSolver
{
	protected ReflectionComponentSolver(TwitchModule module, string componentTypeString, string helpMessage) :
		this(module, componentTypeString, null, helpMessage)
	{
	}

	protected ReflectionComponentSolver(TwitchModule module, string componentTypeString, string assemblyName, string helpMessage) :
		base(module)
	{
		string typeKey = $"{assemblyName}.{componentTypeString}";
		if (!componentTypes.ContainsKey(typeKey))
			componentTypes[typeKey] = ReflectionHelper.FindType(componentTypeString, assemblyName);

		var componentType = componentTypes[typeKey];

		_component = module.BombComponent.GetComponent(componentType);
		if (_component == null) _component = module.BombComponent.GetComponentInChildren(componentType);

		selectable = Module.Selectable;
		if (helpMessage != null) ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), helpMessage);
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

	protected Dictionary<string, int> buttonMap;

	protected static Dictionary<string, Type> componentTypes = new Dictionary<string, Type>();

	protected readonly object _component;
	protected readonly Selectable selectable;

	protected Selectable[] Selectables => selectable.Children;

	// Helper methods
	protected WaitForSeconds Click(int index, float delay = 0.1f) => DoInteractionClick(Selectables[index], delay);
	protected WaitForSeconds Click(string button, float delay = 0.1f) => Click(buttonMap[button], delay);
	protected WaitForSeconds Click(char button, float delay = 0.1f) => Click(button.ToString(), delay);
	protected void LogSelectables() => DebugHelper.Log($"Selectables:\n{Selectables.Select((selectable, index) => $"{index} = {selectable.name}").Join("\n")}");
}