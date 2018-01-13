using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class SimonComponentSolver : ComponentSolver
{
    public SimonComponentSolver(BombCommander bombCommander, SimonComponent bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
		_buttons = bombComponent.buttons;
        modInfo = ComponentSolverFactory.GetModuleInfo("SimonComponentSolver");
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

                if (Canceller.ShouldCancel)
                {
                    Canceller.ResetCancel();
                    yield break;
                }

                yield return DoInteractionClick(button, sequence);
            }
        }
    }
	
    private static readonly Dictionary<string, int> buttonIndex = new Dictionary<string, int>
    {
        {"r", 0}, {"b", 1}, {"g", 2}, {"y", 3}
    };

    private SimonButton[] _buttons = null;
}
