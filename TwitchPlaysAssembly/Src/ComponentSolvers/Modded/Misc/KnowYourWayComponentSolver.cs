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
		_upButton = (KMSelectable)_upButtonField.GetValue(_component);
		_rightButton = (KMSelectable)_rightButtonField.GetValue(_component);
		_downButton = (KMSelectable)_downButtonField.GetValue(_component);
		_leftButton = (KMSelectable)_leftButtonField.GetValue(_component);
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
			yield return DoInteractionClick(DetermineButton(character));
			yield return "trycancel";
		}
	}

	private KMSelectable DetermineButton(char character)
	{
		if (((TextMesh)_upTextField.GetValue(_component)).text == character.ToString())
			return _upButton;
		else if (((TextMesh)_rightTextField.GetValue(_component)).text == character.ToString())
			return _rightButton;
		else if (((TextMesh)_downTextField.GetValue(_component)).text == character.ToString())
			return _downButton;
		else if (((TextMesh)_leftTextField.GetValue(_component)).text == character.ToString())
			return _leftButton;
		else return null;
	}

	static KnowYourWayComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("KnowYourWay");
		_upButtonField = _componentType.GetField("UpButton", BindingFlags.Public | BindingFlags.Instance);
		_upTextField = _componentType.GetField("UpText", BindingFlags.Public | BindingFlags.Instance);
		_rightButtonField = _componentType.GetField("RightButton", BindingFlags.Public | BindingFlags.Instance);
		_rightTextField = _componentType.GetField("RightText", BindingFlags.Public | BindingFlags.Instance);
		_downButtonField = _componentType.GetField("DownButton", BindingFlags.Public | BindingFlags.Instance);
		_downTextField = _componentType.GetField("DownText", BindingFlags.Public | BindingFlags.Instance);
		_leftButtonField = _componentType.GetField("LeftButton", BindingFlags.Public | BindingFlags.Instance);
		_leftTextField = _componentType.GetField("LeftText", BindingFlags.Public | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static FieldInfo _upButtonField = null;
	private static FieldInfo _upTextField = null;
	private static FieldInfo _rightButtonField = null;
	private static FieldInfo _rightTextField = null;
	private static FieldInfo _downButtonField = null;
	private static FieldInfo _downTextField = null;
	private static FieldInfo _leftButtonField = null;
	private static FieldInfo _leftTextField = null;

	private readonly KMSelectable _upButton = null;
	private readonly KMSelectable _rightButton = null;
	private readonly KMSelectable _downButton = null;
	private readonly KMSelectable _leftButton = null;
	private readonly object _component = null;
}
