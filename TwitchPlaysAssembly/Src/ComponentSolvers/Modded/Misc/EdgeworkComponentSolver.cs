using System;
using System.Reflection;
using System.Collections;
using UnityEngine;

public class EdgeworkComponentSolver : ComponentSolver
{
	public EdgeworkComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		_component = bombComponent.GetComponent(_componentType);
		_buttons = (KMSelectable[]) _buttonsField.GetValue(_component);
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Press an answer using !{0} press left. Answers can be referred to numbered from left to right. They can also be referred to by their position.");
	}

	int? ButtonToIndex(string button)
	{
		switch (button)
		{
			case "left":
			case "l":
			case "1":
				return 0;
			case "middle":
			case "m":
			case "center":
			case "centre":
			case "c":
			case "2":
				return 1;
			case "right":
			case "r":
			case "3":
				return 2;
			default:
				return null;
		}
	}
	
	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		var commands = inputCommand.ToLowerInvariant().Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if (commands.Length == 2 && commands[0].EqualsAny("press", "submit", "click", "answer"))
		{
			if ((bool) _canPressButtonsField.GetValue(_component) == false)
			{
				yield return null;
				yield return "sendtochaterror You can't interact with the module right now.";
				yield break;
			}

			int? buttonIndex = ButtonToIndex(commands[1]);
			if (buttonIndex == null) yield break;
			
			yield return null;

			_buttons[(int) buttonIndex].OnInteract();
			yield return new WaitForSeconds(0.1f);
		}
	}

	static EdgeworkComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("EdgeworkModule");
		_buttonsField = _componentType.GetField("Buttons", BindingFlags.Public | BindingFlags.Instance);
		_canPressButtonsField = _componentType.GetField("canPressButtons", BindingFlags.NonPublic | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static FieldInfo _buttonsField = null;
	private static FieldInfo _canPressButtonsField = null;

	private readonly KMSelectable[] _buttons = null;
	private readonly object _component = null;
}
