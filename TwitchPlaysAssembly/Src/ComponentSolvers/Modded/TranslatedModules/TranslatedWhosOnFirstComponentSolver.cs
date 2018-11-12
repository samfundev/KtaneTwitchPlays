using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class TranslatedWhosOnFirstComponentSolver : ComponentSolver
{
	public TranslatedWhosOnFirstComponentSolver(TwitchModule module) :
		base(module)
	{
		Component component = module.BombComponent.GetComponent(ComponentType);
		_buttons = (KMSelectable[]) ButtonsField.GetValue(component);
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "!{0} what? [press the button that says \"WHAT?\"] | The phrase must match exactly | Not case sensitive| If the language used asks for pressing a literally blank button, use \"!{0} literally blank\"").Clone();

		string language = TranslatedModuleHelper.GetManualCodeAddOn(module.BombComponent, component, ComponentType);
		if (language != null)
			ManualCode = $"Who%E2%80%99s%20on%20First{language}";
		ModInfo.moduleDisplayName = $"Who's on First Translated{TranslatedModuleHelper.GetModuleDisplayNameAddon(module.BombComponent, component, ComponentType)}";
		module.BombComponent.StartCoroutine(SetHeaderText());
	}

	private IEnumerator SetHeaderText()
	{
		yield return new WaitUntil(() => Module != null);
		Module.HeaderText = ModInfo.moduleDisplayName;
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.Trim();
		List<string> buttonLabels = _buttons.Select(button => button.GetComponentInChildren<TextMesh>().text.ToUpperInvariant()).ToList();

		if (inputCommand.Equals("literally blank", StringComparison.InvariantCultureIgnoreCase))
			inputCommand = "\u2003\u2003";

		int index = buttonLabels.IndexOf(inputCommand.ToUpperInvariant());
		if (index < 0)
		{
			yield return null;
			yield return buttonLabels.Any(label => label == " ")
				? "sendtochaterror The module is not ready for input yet."
				: $"sendtochaterror There isn't any label that contains \"{inputCommand.Replace("\u2003\u2003", "Literally Blank")}\".";
			yield break;
		}
		yield return null;
		yield return DoInteractionClick(_buttons[index]);
	}

	static TranslatedWhosOnFirstComponentSolver()
	{
		ComponentType = ReflectionHelper.FindType("WhosOnFirstTranslatedModule");
		ButtonsField = ComponentType.GetField("buttons", BindingFlags.Public | BindingFlags.Instance);
	}

	private static readonly Type ComponentType;
	private static readonly FieldInfo ButtonsField;

	private readonly KMSelectable[] _buttons;
}
