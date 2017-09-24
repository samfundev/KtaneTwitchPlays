using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class ForgetMeNotComponentSolver : ComponentSolver
{
    public ForgetMeNotComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _buttons = (Array)_buttonsField.GetValue(bombComponent.GetComponent(_componentType));
        helpMessage = "Enter forget me not sequence with !{0} press 5 3 1 8 2 0... The Sequence length depends on how many modules were on the bomb.";
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        if (!inputCommand.StartsWith("press ", StringComparison.InvariantCultureIgnoreCase))
        {
            yield break;
        }
        inputCommand = inputCommand.Substring(6);

        int beforeButtonStrikeCount = StrikeCount;

        foreach (char buttonString in inputCommand)
        {
            int val = buttonString - '0';
            if(val >= 0 && val <= 9)
            {
                MonoBehaviour button = (MonoBehaviour)_buttons.GetValue(val);

                yield return buttonString;

                if (Canceller.ShouldCancel)
                {
                    Canceller.ResetCancel();
                    yield break;
                }

                DoInteractionStart(button);
                yield return new WaitForSeconds(0.1f);
                DoInteractionEnd(button);

                //Escape the sequence if a part of the given sequence is wrong
                if (StrikeCount != beforeButtonStrikeCount || Solved)
                {
                    yield break;
                }
            }
        }
    }

    static ForgetMeNotComponentSolver()
    {
        _componentType = ReflectionHelper.FindType("AdvancedMemory");
        _buttonsField = _componentType.GetField("Buttons", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private static Type _componentType = null;
    private static FieldInfo _buttonsField = null;

    private Array _buttons = null;
}
