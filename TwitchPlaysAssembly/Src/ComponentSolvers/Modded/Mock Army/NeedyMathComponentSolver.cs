using System;
using System.Collections;
using System.Reflection;

public class NeedyMathComponentSolver : ComponentSolver
{
	public NeedyMathComponentSolver(TwitchModule module)
		: base(module)
	{
		object component = module.BombComponent.GetComponent(ComponentType);
		_buttons = (KMSelectable[]) ButtonsField.GetValue(component);
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Submit an answer with !{0} submit -47.");
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

	private static readonly Type ComponentType = ReflectionHelper.FindType("NeedyMathModule");
	private static readonly FieldInfo ButtonsField = ComponentType.GetField("Buttons", BindingFlags.Public | BindingFlags.Instance);

	private readonly KMSelectable[] _buttons;
}
