using System;
using System.Reflection;
using System.Collections;

public class ErrorCodesComponentSolver : ComponentSolver
{
	public ErrorCodesComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		   base(bombCommander, bombComponent)
	{
		_component = bombComponent.GetComponent(_componentType);
		_buttons = (KMSelectable[])_buttonsField.GetValue(_component);
		submit = (KMSelectable)_sendField.GetValue(_component);
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Submit a decimal, octal, hexidecimal, or binary value using !submit 00010100");
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
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
				var s = c.ToString();
				yield return DoInteractionClick(_buttons[Convert.ToInt32(s, 16)]);
			}
			else yield break;
		}
		yield return null;
		yield return DoInteractionClick(submit);
	}

	static ErrorCodesComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("ErrorCodes");
		_buttonsField = _componentType.GetField("numberButtons", BindingFlags.Public | BindingFlags.Instance);
		_sendField = _componentType.GetField("sendButton", BindingFlags.Public | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static FieldInfo _buttonsField = null;
	private static FieldInfo _sendField = null;

	private object _component = null;
	private KMSelectable[] _buttons = null;
	private KMSelectable submit = null;
}