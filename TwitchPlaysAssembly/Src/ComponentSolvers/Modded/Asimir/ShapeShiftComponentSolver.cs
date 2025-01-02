using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class ShapeShiftComponentSolver : ComponentSolver
{
	public ShapeShiftComponentSolver(TwitchModule module) :
		base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = (KMSelectable[]) ButtonsField.GetValue(_component);
		SetHelpMessage("Submit your answer with !{0} submit point round. Reset to initial state with !{0} reset. Valid shapes: flat, point, round and ticket.");

		if (module.BombComponent.gameObject.activeInHierarchy)
			module.BombComponent.StartCoroutine(GetDisplay());
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

	private IEnumerator GetDisplay()
	{
		yield return new WaitUntil(() => IsActivated);

		_initialL = _displayL = (int) DisplayLField.GetValue(_component);
		_initialR = _displayR = (int) DisplayRField.GetValue(_component);
		_solutionL = (int) SolutionLField.GetValue(_component);
		_solutionR = (int) SolutionRField.GetValue(_component);
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
				int? shapeL = ToShapeIndex(commands[1]);
				int? shapeR = ToShapeIndex(commands[2]);

				if (shapeL != null && shapeR != null)
				{
					yield return null;
					yield return SetDisplay((int) shapeL, (int) shapeR);

					DoInteractionClick(_buttons[1]);
				}

				break;
			case 1 when commands[0].Equals("reset"):
				yield return null;
				yield return SetDisplay(_initialL, _initialR);
				break;
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		while (!IsActivated)
			yield return true;
		yield return SetDisplay(_solutionL, _solutionR);
		DoInteractionClick(_buttons[1]);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("ShapeShiftModule");
	private static readonly FieldInfo ButtonsField = ComponentType.GetField("buttons", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo DisplayLField = ComponentType.GetField("displayL", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo DisplayRField = ComponentType.GetField("displayR", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo SolutionLField = ComponentType.GetField("solutionL", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo SolutionRField = ComponentType.GetField("solutionR", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo IsActivatedField = ComponentType.GetField("isActivated", BindingFlags.NonPublic | BindingFlags.Instance);

	private readonly KMSelectable[] _buttons;
	private readonly object _component;
	private int _displayL;
	private int _displayR;
	private int _initialL;
	private int _initialR;
	private int _solutionL;
	private int _solutionR;

	private bool IsActivated => (bool) IsActivatedField.GetValue(_component);
}
