using System;
using System.Reflection;
using System.Collections;
using UnityEngine;

public class ShapeShiftComponentSolver : ComponentSolver
{
	public ShapeShiftComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
		base(bombCommander, bombComponent, ircConnection, canceller)
	{
		object _component = bombComponent.GetComponent(_componentType);
		_buttons = (KMSelectable[]) _buttonsField.GetValue(_component);
		bombComponent.StartCoroutine(GetDisplay(_component));
		helpMessage = "Submit your anwser with !{0} submit point round. Reset to initial state with !{0} reset. Valid shapes: flat, point, round and ticket.";
	}

	private int? ToShapeIndex(string shape)
	{
		switch (shape)
		{
			case "flat":
			case "rectangle":
				return 0;
			case "round":
			case "pill":
			case "circle":
				return 1;
			case "point":
			case "triangle":
				return 2;
			case "ticket":
			case "cut":
				return 3;
			default:
				return null;
		}
	}

	private IEnumerator GetDisplay(object _component)
	{
		yield return new WaitUntil(() => (bool) _isActivatedField.GetValue(_component));

		initialL = _displayL = (int) _displayLField.GetValue(_component);
		initialR = _displayR = (int) _displayRField.GetValue(_component);
		Debug.Log(_displayL + " " + _displayR);
	}

	private IEnumerable SetDisplay(int displayIndexL, int displayIndexR)
	{
		while (_displayL != displayIndexL)
		{
			DoInteractionClick(_buttons[0]);
			_displayL = (_displayL + 1) % 4;
			yield return new WaitForSeconds(0.1f);
		}
		
		while (_displayR != displayIndexR)
		{
			DoInteractionClick(_buttons[2]);
			_displayR = (_displayR + 1) % 4;
			yield return new WaitForSeconds(0.1f);
		}
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		var commands = inputCommand.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if (commands.Length == 3 && commands[0].Equals("submit"))
		{
			int? shapeL = ToShapeIndex(commands[1]);
			int? shapeR = ToShapeIndex(commands[2]);

			if (shapeL != null && shapeR != null)
			{
				yield return null;
				foreach (object obj in SetDisplay((int) shapeL, (int) shapeR)) yield return obj;

				DoInteractionClick(_buttons[1]);
			}
		}
		else if (commands.Length == 1 && commands[0].Equals("reset"))
		{
			yield return null;
			foreach (object obj in SetDisplay(initialL, initialR)) yield return obj;
		}
	}

	static ShapeShiftComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("ShapeShiftModule");
		_buttonsField = _componentType.GetField("buttons", BindingFlags.Public | BindingFlags.Instance);
		_displayLField = _componentType.GetField("displayL", BindingFlags.NonPublic | BindingFlags.Instance);
		_displayRField = _componentType.GetField("displayR", BindingFlags.NonPublic | BindingFlags.Instance);
		_isActivatedField = _componentType.GetField("isActivated", BindingFlags.NonPublic | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static FieldInfo _buttonsField = null;
	private static FieldInfo _displayLField = null;
	private static FieldInfo _displayRField = null;
	private static FieldInfo _isActivatedField = null;

	private KMSelectable[] _buttons = null;
	private int _displayL;
	private int _displayR;
	private int initialL;
	private int initialR;
}
