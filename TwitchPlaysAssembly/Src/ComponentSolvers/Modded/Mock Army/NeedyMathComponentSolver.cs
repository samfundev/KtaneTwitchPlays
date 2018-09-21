using System;
using System.Collections;
using System.Reflection;

public class NeedyMathComponentSolver : ComponentSolver
{
	public NeedyMathComponentSolver(BombCommander bombCommander, BombComponent bombComponent)
		: base(bombCommander, bombComponent)
	{
		_component = bombComponent.GetComponent(_componentType);
		_buttons = (KMSelectable[])_buttonsField.GetValue(_component);
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Submit an answer with !{0} submit -47.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.Trim().ToLowerInvariant();

		if (!inputCommand.StartsWith("submit "))
			yield break;

		inputCommand = inputCommand.Substring(7);

		string trimmedCommand = inputCommand.StartsWith("-") ? inputCommand.Substring(1) : inputCommand;

		if (!int.TryParse(trimmedCommand, out _))
			yield break;

		yield return null;

		if (inputCommand.StartsWith("-"))
			yield return DoInteractionClick(_buttons[10]);

		foreach (char character in trimmedCommand)
		{
			yield return DoInteractionClick(_buttons[int.Parse(character.ToString())]);
		}

		yield return DoInteractionClick(_buttons[11]);
	}

	static NeedyMathComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("NeedyMathModule");
		_buttonsField = _componentType.GetField("Buttons", BindingFlags.Public | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static FieldInfo _buttonsField = null;

	private readonly object _component = null;
	private readonly KMSelectable[] _buttons = null;
}
