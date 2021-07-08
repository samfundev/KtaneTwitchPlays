using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ExtendedPasswordComponentSolver : ComponentSolverShim
{
	public ExtendedPasswordComponentSolver(TwitchModule module) :
		base(module, "ExtendedPassword")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "!{0} cycle 6 [cycle through the letters in column 6] | !{0} cycle [cycle all columns] | !{0} toggle [move all columns down one letter] | !{0} lambda [try to submit a word]");
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<KMSelectable[]>("buttons");
		_submit = _component.GetValue<KMSelectable>("submitbutton");
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

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		while (!_component.GetValue<bool>("isActivated"))
			yield return true;
		int[] dispPositions = _component.GetValue<int[]>("displaysTextPosition");
		string[,] displays = _component.GetValue<string[,]>("displaysText");
		string goal = _component.GetValue<string>("goalword");
		for (int i = 0; i < 6; i++)
		{
			int goalIndex = -1;
			for (int j = 0; j < 6; j++)
			{
				if (displays[i, j].Equals(goal[i].ToString()))
				{
					goalIndex = j;
					break;
				}
			}
			yield return SelectIndex(dispPositions[i], goalIndex, 6, _buttons[i], _buttons[i + 6]);
		}
		yield return DoInteractionClick(_submit);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("ExtendedPassword", "ExtendedPassword");

	private readonly object _component;
	private readonly KMSelectable[] _buttons;
	private readonly KMSelectable _submit;
}
