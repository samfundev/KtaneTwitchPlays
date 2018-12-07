using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class ExtendedPasswordComponentSolver : ComponentSolverShim
{
	public ExtendedPasswordComponentSolver(TwitchModule module) :
		base(module, "ExtendedPassword")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "!{0} cycle 6 [cycle through the letters in column 6] | !{0} cycle [cycle all columns] | !{0} toggle [move all columns down one letter] | !{0} lambda [try to submit a word]");
		_buttons = (KMSelectable[]) ButtonsField.GetValue(module.BombComponent.GetComponent(ComponentType));
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		inputCommand = inputCommand.Trim();
		if (inputCommand.Equals("toggle", StringComparison.InvariantCultureIgnoreCase))
		{
			yield return "password";
			for (int i = 0; i < 6; i++)
				yield return DoInteractionClick(_buttons[i]);
			yield break;
		}

		if (inputCommand.StartsWith("cycle ", StringComparison.InvariantCultureIgnoreCase))
		{
			HashSet<int> alreadyCycled = new HashSet<int>();
			string[] commandParts = inputCommand.Split(' ');

			foreach (string cycle in commandParts.Skip(1))
			{
				if (!int.TryParse(cycle, out int spinnerIndex) || !alreadyCycled.Add(spinnerIndex) || spinnerIndex < 1 || spinnerIndex > 6)
					continue;

				IEnumerator spinnerCoroutine = RespondToCommandUnshimmed($"cycle {cycle}");
				while (spinnerCoroutine.MoveNext())
				{
					yield return spinnerCoroutine.Current;
					yield return "trycancel";
				}
			}
		}
		else if (inputCommand.Trim().Length == 6)
		{
			IEnumerator command = RespondToCommandUnshimmed(inputCommand);
			while (command.MoveNext())
			{
				yield return command.Current;
				yield return "trycancel";
			}
			yield return null;
			yield return "unsubmittablepenalty";
		}
		else
		{
			yield return "sendtochaterror valid commands are 'cycle [columns]' or a 6 letter password to try.";
		}
	}

	static ExtendedPasswordComponentSolver()
	{
		ComponentType = ReflectionHelper.FindType("ExtendedPassword", "ExtendedPassword");
		ButtonsField = ComponentType.GetField("buttons", BindingFlags.Public | BindingFlags.Instance);
	}

	private static readonly Type ComponentType;
	private static readonly FieldInfo ButtonsField;

	private readonly KMSelectable[] _buttons;
}
