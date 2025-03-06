using System;
using System.Collections;
using System.Reflection;

[ModuleID("errorCodes")]
public class ErrorCodesComponentSolver : ComponentSolver
{
	public ErrorCodesComponentSolver(TwitchModule module) :
		   base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = (KMSelectable[]) ButtonsField.GetValue(_component);
		_submit = (KMSelectable) SendField.GetValue(_component);
		SetHelpMessage("Submit a decimal, octal, hexidecimal, or binary value using !{0} submit 00010100.");

		module.BombComponent.OnPass += _ =>
		{
			moduleSolved = true;
			return false;
		};
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

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		var solution = _component.GetValue<string>("solution");
		yield return RespondToCommandInternal($"submit {solution}");
		while (!moduleSolved)
		{
			yield return true;
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("ErrorCodes", "errorCodes");
	private readonly object _component;
	private static readonly FieldInfo ButtonsField = ComponentType.GetField("numberButtons", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo SendField = ComponentType.GetField("sendButton", BindingFlags.Public | BindingFlags.Instance);

	private readonly KMSelectable[] _buttons;
	private readonly KMSelectable _submit;
	private bool moduleSolved = false;
}