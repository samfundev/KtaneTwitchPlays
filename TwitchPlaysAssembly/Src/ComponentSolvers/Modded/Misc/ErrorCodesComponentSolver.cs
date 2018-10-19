using System;
using System.Collections;
using System.Reflection;

public class ErrorCodesComponentSolver : ComponentSolver
{
	public ErrorCodesComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		   base(bombCommander, bombComponent)
	{
		object component = bombComponent.GetComponent(ComponentType);
		_buttons = (KMSelectable[]) ButtonsField.GetValue(component);
		_submit = (KMSelectable) SendField.GetValue(component);
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Submit a decimal, octal, hexidecimal, or binary value using !{0} submit 00010100.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		var commands = inputCommand.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (!commands[0].Equals("submit") || !commands.Length.Equals(2)) yield break;

		foreach (char c in commands[1])
		{
			if (c >= '0' && c <= '9')
			{
				yield return null;
				yield return DoInteractionClick(_buttons[c - '0']);
			}
			else if (c >= 'a' && c <= 'f')
			{
				yield return null;
				yield return DoInteractionClick(_buttons[Convert.ToInt32(c.ToString(), 16)]);
			}
			else yield break;
		}
		yield return null;
		yield return DoInteractionClick(_submit);
	}

	static ErrorCodesComponentSolver()
	{
		ComponentType = ReflectionHelper.FindType("ErrorCodes");
		ButtonsField = ComponentType.GetField("numberButtons", BindingFlags.Public | BindingFlags.Instance);
		SendField = ComponentType.GetField("sendButton", BindingFlags.Public | BindingFlags.Instance);
	}

	private static readonly Type ComponentType;
	private static readonly FieldInfo ButtonsField;
	private static readonly FieldInfo SendField;

	private readonly KMSelectable[] _buttons;
	private readonly KMSelectable _submit;
}