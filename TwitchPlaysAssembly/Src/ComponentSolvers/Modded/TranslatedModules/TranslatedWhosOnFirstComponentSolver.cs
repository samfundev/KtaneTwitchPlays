using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TranslatedWhosOnFirstComponentSolver : ComponentSolver
{
	public TranslatedWhosOnFirstComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		_component = bombComponent.GetComponent(_componentType);
		_buttons = (KMSelectable[]) _buttonsField.GetValue(_component);
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "!{0} what? [press the button that says \"WHAT?\"] | The phrase must match exactly | Not case sensitive| If the language used asks for pressing a literally blank button, use \"!{0} literally blank\"");
		
		if (bombCommander != null)
		{
			string language = TranslatedModuleHelper.GetManualCodeAddOn(bombComponent);
			if (language != null) modInfo.manualCode = $"Who%E2%80%99s%20on%20First{language}";
			modInfo.moduleDisplayName = $"Who's on First Translated{TranslatedModuleHelper.GetModuleDisplayNameAddon(bombComponent)}";
			bombComponent.StartCoroutine(SetHeaderText());
		}
	}

	private IEnumerator SetHeaderText()
	{
		yield return new WaitUntil(() => ComponentHandle != null);
		ComponentHandle.HeaderText = modInfo.moduleDisplayName;
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
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
				: string.Format("sendtochaterror There isn't any label that contains \"{0}\".", inputCommand.Replace("\u2003\u2003", "Literally Blank"));
			yield break;
		}
		yield return null;
		yield return DoInteractionClick(_buttons[index]);
	}

	static TranslatedWhosOnFirstComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("WhosOnFirstTranslatedModule");
		_buttonsField = _componentType.GetField("buttons", BindingFlags.Public | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static FieldInfo _buttonsField = null;

	private object _component = null;
	private KMSelectable[] _buttons = null;
}
