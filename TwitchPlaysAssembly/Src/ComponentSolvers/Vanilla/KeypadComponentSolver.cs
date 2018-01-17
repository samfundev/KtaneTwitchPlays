using System;
using System.Collections;
using System.Text.RegularExpressions;

public class KeypadComponentSolver : ComponentSolver
{
    public KeypadComponentSolver(BombCommander bombCommander, KeypadComponent bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
		_buttons = bombComponent.buttons;
        modInfo = ComponentSolverFactory.GetModuleInfo("KeypadComponentSolver");
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        if (!inputCommand.StartsWith("press ", StringComparison.InvariantCultureIgnoreCase))
        {
            yield break;
        }
        inputCommand = inputCommand.Substring(6);

        foreach (Match buttonIndexString in Regex.Matches(inputCommand, @"[1-4]"))
        {
            if (!int.TryParse(buttonIndexString.Value, out int buttonIndex))
            {
                continue;
            }

            buttonIndex--;

            if (buttonIndex >= 0 && buttonIndex < _buttons.Length)
            {
	            if (_buttons[buttonIndex].IsStayingDown)
		            continue;

                yield return buttonIndexString.Value;

                if (Canceller.ShouldCancel)
                {
                    Canceller.ResetCancel();
                    yield break;
                }
				
                yield return DoInteractionClick(_buttons[buttonIndex]);
            }
        }
    }

    private KeypadButton[] _buttons = null;
}
