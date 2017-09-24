using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class KeypadComponentSolver : ComponentSolver
{
    public KeypadComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _buttons = (Array)_buttonsField.GetValue(bombComponent);
        
        helpMessage = "!{0} press 3 1 2 4 | The buttons are 1=TL, 2=TR, 3=BL, 4=BR";
        manualCode = "Keypads";
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        if (!inputCommand.StartsWith("press ", StringComparison.InvariantCultureIgnoreCase))
        {
            yield break;
        }
        inputCommand = inputCommand.Substring(6);

        int beforeButtonStrikeCount = StrikeCount;

        foreach (Match buttonIndexString in Regex.Matches(inputCommand, @"[1-4]"))
        {
            int buttonIndex = 0;
            if (!int.TryParse(buttonIndexString.Value, out buttonIndex))
            {
                continue;
            }

            buttonIndex--;

            if (buttonIndex >= 0 && buttonIndex < _buttons.Length)
            {
                yield return buttonIndexString.Value;

                if (Canceller.ShouldCancel)
                {
                    Canceller.ResetCancel();
                    yield break;
                }

                MonoBehaviour button = (MonoBehaviour)_buttons.GetValue(buttonIndex);
                DoInteractionStart(button);
                yield return new WaitForSeconds(0.1f);
                DoInteractionEnd(button);

                //Escape the sequence if a part of the given sequence is wrong
                if (StrikeCount != beforeButtonStrikeCount || Solved)
                {
                    break;
                }
            }
        }
    }

    static KeypadComponentSolver()
    {
        _keypadComponentType = ReflectionHelper.FindType("KeypadComponent");
        _buttonsField = _keypadComponentType.GetField("buttons", BindingFlags.Public | BindingFlags.Instance);
    }

    private static Type _keypadComponentType = null;
    private static FieldInfo _buttonsField = null;

    private Array _buttons = null;
}
