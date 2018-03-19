using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class SimonComponentSolver : ComponentSolver
{
    public SimonComponentSolver(BombCommander bombCommander, SimonComponent bombComponent) :
        base(bombCommander, bombComponent)
	{
		_buttons = bombComponent.buttons;
        modInfo = ComponentSolverFactory.GetModuleInfo("SimonComponentSolver", "!{0} press red green blue yellow, !{0} press rgby [press a sequence of colours] | You must include the input from any previous stages");
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        if (!inputCommand.StartsWith("press ", StringComparison.InvariantCultureIgnoreCase))
        {
            yield break;
        }
        inputCommand = inputCommand.Substring(6);

        string sequence = "pressing ";
        foreach (Match move in Regex.Matches(inputCommand, @"(\b(red|blue|green|yellow)\b|[rbgy])", RegexOptions.IgnoreCase))
        {
            SimonButton button = _buttons[buttonIndex[move.Value.Substring(0, 1).ToLowerInvariant()]];

            if (button != null)
            {
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
    }

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		while (!BombComponent.IsActive) yield return true;
		while (!BombComponent.IsSolved)
		{
			int index = ((SimonComponent) BombComponent).GetNextIndexToPress();
			yield return DoInteractionClick(_buttons[index]);
		}
	}

	private static readonly Dictionary<string, int> buttonIndex = new Dictionary<string, int>
    {
        {"r", 0}, {"b", 1}, {"g", 2}, {"y", 3}
    };

    private SimonButton[] _buttons = null;
}
