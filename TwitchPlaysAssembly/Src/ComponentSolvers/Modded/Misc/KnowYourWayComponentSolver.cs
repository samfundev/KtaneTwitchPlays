using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class KnowYourWayComponentSolver : ComponentSolver
{
	public KnowYourWayComponentSolver(BombCommander bombCommander, BombComponent bombComponent)
		: base(bombCommander, bombComponent)
	{
		object component = bombComponent.GetComponent(ComponentType);
		_buttons = ButtonFields.Select(field => (KMSelectable) field.GetValue(component)).ToArray();
		_textMeshes = TextFields.Select(field => (TextMesh) field.GetValue(component)).ToArray();
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Press the buttons labeled UDLR with !{0} press UDLR.");
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
			for (int i = 0; i < TextFields.Length; i++)
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
		ComponentType = ReflectionHelper.FindType("KnowYourWay");
		ButtonFields = Directions.Select(direction => ComponentType.GetField(direction + "Button", BindingFlags.Public | BindingFlags.Instance)).ToArray();
		TextFields = Directions.Select(direction => ComponentType.GetField(direction + "Text", BindingFlags.Public | BindingFlags.Instance)).ToArray();
	}

	private static readonly Type ComponentType;
	private static readonly FieldInfo[] ButtonFields;
	private static readonly FieldInfo[] TextFields;
	private readonly KMSelectable[] _buttons;
	private readonly TextMesh[] _textMeshes;

	private static readonly string[] Directions = { "Up", "Right", "Down", "Left" };
}
