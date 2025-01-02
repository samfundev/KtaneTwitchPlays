using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class SimonComponentSolver : ComponentSolver
{
	public SimonComponentSolver(TwitchModule module) :
		base(module)
	{
		_buttons = ((SimonComponent) module.BombComponent).buttons;
		SetHelpMessage("!{0} press red green blue yellow, !{0} press rgby [press a sequence of colours] | You must include the input from any previous stages");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.Trim();
		if (!inputCommand.StartsWith("press ", StringComparison.InvariantCultureIgnoreCase))
			yield break;
		inputCommand = inputCommand.Substring(6);

		string sequence = "pressing ";
		foreach (Match move in Regex.Matches(inputCommand, @"(\b(red|blue|green|yellow)\b|[rbgy])", RegexOptions.IgnoreCase))
		{
			SimonButton button = _buttons[ButtonIndex[move.Value.Substring(0, 1).ToLowerInvariant()]];

			if (button == null) continue;
			yield return move.Value;
			sequence += move.Value + " ";

			if (CoroutineCanceller.ShouldCancel)
			{
				CoroutineCanceller.ResetCancel();
				yield break;
			}

			yield return DoInteractionClick(button, sequence);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		while (!Module.BombComponent.IsActive) yield return true;
		while (!Module.Solved)
		{
			int index = ((SimonComponent) Module.BombComponent).GetNextIndexToPress();
			yield return DoInteractionClick(_buttons[index]);
		}
	}

	private static readonly Dictionary<string, int> ButtonIndex = new Dictionary<string, int>
	{
		{"r", 0}, {"b", 1}, {"g", 2}, {"y", 3}
	};

	private readonly SimonButton[] _buttons;
}
