using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class ThirdBaseComponentSolver : ComponentSolver
{
	public ThirdBaseComponentSolver(TwitchModule module) :
		base(module)
	{
		object component = module.BombComponent.GetComponent(ComponentType);
		_buttons = (KMSelectable[]) ButtonsField.GetValue(component);
		_phrase = (string[]) PhraseField.GetValue(component);
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Press a button with !{0} z0s8. Word must match the button as it would appear if the module was the right way up. Not case sensitive.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		List<string> buttonLabels = _buttons.Select(button => button.GetComponentInChildren<TextMesh>().text.ToUpperInvariant().Trim()).ToList();

		inputCommand = inputCommand.Replace('0', 'O').ToUpperInvariant();
		if (inputCommand.StartsWith("PRESS "))
		{
			inputCommand = inputCommand.Substring(6);
		}

		if (!_phrase.Contains(inputCommand))
		{
			yield break;
		}

		int index = buttonLabels.IndexOf(inputCommand);
		if (index < 0)
		{
			yield return null;
			yield return buttonLabels.Any(label => label == " ")
				? "sendtochaterror The module is not ready for input yet."
				: "unsubmittablepenalty"; //string.Format("sendtochaterror There isn't any label that contains \"{0}\".", inputCommand);
			yield break;
		}
		yield return null;
		yield return DoInteractionClick(_buttons[index]);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("ThirdBaseModule");
	private static readonly FieldInfo ButtonsField = ComponentType.GetField("buttons", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo PhraseField = ComponentType.GetField("phrase", BindingFlags.NonPublic | BindingFlags.Instance);

	private readonly KMSelectable[] _buttons;
	private readonly string[] _phrase;
}
