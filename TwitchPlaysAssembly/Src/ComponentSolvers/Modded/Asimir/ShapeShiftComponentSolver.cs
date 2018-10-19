using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class ShapeShiftComponentSolver : ComponentSolver
{
	public ShapeShiftComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		object component = bombComponent.GetComponent(ComponentType);
		_buttons = (KMSelectable[]) ButtonsField.GetValue(component);
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Submit your answer with !{0} submit point round. Reset to initial state with !{0} reset. Valid shapes: flat, point, round and ticket.");

		if (bombComponent.gameObject.activeInHierarchy)
			bombComponent.StartCoroutine(GetDisplay(component));
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

	private IEnumerator GetDisplay(object component)
	{
		yield return new WaitUntil(() => (bool) IsActivatedField.GetValue(component));

		_initialL = _displayL = (int) DisplayLField.GetValue(component);
		_initialR = _displayR = (int) DisplayRField.GetValue(component);
	}

	private IEnumerator SetDisplay(int displayIndexL, int displayIndexR)
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

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] commands = inputCommand.ToLowerInvariant().Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		// ReSharper disable once SwitchStatementMissingSomeCases
		switch (commands.Length)
		{
			case 3 when commands[0].Equals("submit"):
			{
				int? shapeL = ToShapeIndex(commands[1]);
				int? shapeR = ToShapeIndex(commands[2]);

				if (shapeL != null && shapeR != null)
				{
					yield return null;
					yield return SetDisplay((int) shapeL, (int) shapeR);

					DoInteractionClick(_buttons[1]);
				}

				break;
			}
			case 1 when commands[0].Equals("reset"):
				yield return null;
				yield return SetDisplay(_initialL, _initialR);
				break;
		}
	}

	static ShapeShiftComponentSolver()
	{
		ComponentType = ReflectionHelper.FindType("ShapeShiftModule");
		ButtonsField = ComponentType.GetField("buttons", BindingFlags.Public | BindingFlags.Instance);
		DisplayLField = ComponentType.GetField("displayL", BindingFlags.NonPublic | BindingFlags.Instance);
		DisplayRField = ComponentType.GetField("displayR", BindingFlags.NonPublic | BindingFlags.Instance);
		IsActivatedField = ComponentType.GetField("isActivated", BindingFlags.NonPublic | BindingFlags.Instance);
	}

	private static readonly Type ComponentType;
	private static readonly FieldInfo ButtonsField;
	private static readonly FieldInfo DisplayLField;
	private static readonly FieldInfo DisplayRField;
	private static readonly FieldInfo IsActivatedField;

	private readonly KMSelectable[] _buttons;
	private int _displayL;
	private int _displayR;
	private int _initialL;
	private int _initialR;
}
