using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdBaseComponentSolver : ComponentSolver
{
	public ThirdBaseComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		_component = bombComponent.GetComponent(_componentType);
		_buttons = (KMSelectable[]) _buttonsField.GetValue(_component);
		_phrase = (string[]) _phraseField.GetValue(_component);
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Press a button with !{0} z0s8. Word must match the button as it would appear if the module was the right way up. Not case sensitive.", null, true, true);
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
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

	static ThirdBaseComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("ThirdBaseModule");
		_buttonsField = _componentType.GetField("buttons", BindingFlags.Public | BindingFlags.Instance);
		_phraseField = _componentType.GetField("phrase", BindingFlags.NonPublic | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static FieldInfo _buttonsField = null;
	private static FieldInfo _phraseField = null;

	private object _component = null;
	private KMSelectable[] _buttons = null;
	private string[] _phrase = null;
}
