using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class KnowYourWayComponentSolver : ComponentSolver
{
	public KnowYourWayComponentSolver(BombCommander bombCommander, BombComponent bombComponent)
		: base (bombCommander, bombComponent)
	{
		_component = bombComponent.GetComponent(_componentType);
		_buttons = _buttonFields.Select(field => (KMSelectable) field.GetValue(_component)).ToArray();
		_textMeshes = _textFields.Select(field => (TextMesh) field.GetValue(_component)).ToArray();
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Press the buttons labeled UDLR with !{0} press UDLR.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.Trim().ToLower();
		if (!inputCommand.StartsWith("press ")) yield break;
		string iterator = inputCommand.Substring(6);

		IEnumerable<char> invalid = iterator.Where(x => !x.EqualsAny('u', 'd', 'l', 'r'));
		if (invalid.Any()) yield break;

		foreach (char character in iterator)
		{
			yield return null;
			for (int i = 0; i < _textFields.Length; i++)
			{
				if (_textMeshes[i].text.ToLower()[0] == character)
				{
					DoInteractionClick(_buttons[i]);
					break;
				}
			}
			yield return "trycancel";
		}
	}

	static KnowYourWayComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("KnowYourWay");
		_buttonFields = _directions.Select(direction => _componentType.GetField(direction + "Button", BindingFlags.Public | BindingFlags.Instance)).ToArray();
		_textFields = _directions.Select(direction => _componentType.GetField(direction + "Text", BindingFlags.Public | BindingFlags.Instance)).ToArray();
	}

	private static Type _componentType = null;
	private static FieldInfo[] _buttonFields = null;
	private static FieldInfo[] _textFields = null;
	private KMSelectable[] _buttons = null;
	private TextMesh[] _textMeshes = null;

	private static readonly string[] _directions = new string[4] { "Up", "Right", "Down", "Left" };
	private readonly object _component = null;
}
