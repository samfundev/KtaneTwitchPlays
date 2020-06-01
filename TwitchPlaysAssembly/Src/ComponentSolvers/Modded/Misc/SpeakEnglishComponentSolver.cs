using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

public class SpeakEnglishComponentSolver : ComponentSolver
{
	public SpeakEnglishComponentSolver(TwitchModule module)
		: base(module)
	{
		object component = module.BombComponent.GetComponent(ComponentType);
		_buttons = (List<KMSelectable>) ButtonsField.GetValue(component);
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Press the top button with !{0} press top.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.Trim().ToLowerInvariant();
		Match match1 = Regex.Match(inputCommand, @"^\s*press\s+(?<index>top|middle|bottom)\s*$");
		if (!match1.Success) yield break;
		yield return null;
		// ReSharper disable once SwitchStatementMissingSomeCases
		switch (match1.Groups["index"].Value)
		{
			case "top":
				yield return DoInteractionClick(_buttons[0]);
				break;
			case "middle":
				yield return DoInteractionClick(_buttons[1]);
				break;
			case "bottom":
				yield return DoInteractionClick(_buttons[2]);
				break;
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("SpeakEnglishBehav");
	private static readonly FieldInfo ButtonsField = ComponentType.GetField("buttons", BindingFlags.Public | BindingFlags.Instance);

	private readonly List<KMSelectable> _buttons;
}
