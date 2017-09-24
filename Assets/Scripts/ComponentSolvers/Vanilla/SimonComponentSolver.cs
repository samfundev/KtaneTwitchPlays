using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class SimonComponentSolver : ComponentSolver
{
    public SimonComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _buttons = (Array)_buttonsField.GetValue(bombComponent);
        
        helpMessage = "!{0} press red green blue yellow, !{0} press rgby [press a sequence of colours] | You must include the input from any previous stages";
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        if (!inputCommand.StartsWith("press ", StringComparison.InvariantCultureIgnoreCase))
        {
            yield break;
        }
        inputCommand = inputCommand.Substring(6);

        int beforeButtonStrikeCount = StrikeCount;

        foreach (Match move in Regex.Matches(inputCommand, @"(\b(red|blue|green|yellow)\b|[rbgy])", RegexOptions.IgnoreCase))
        {
            MonoBehaviour button = (MonoBehaviour)_buttons.GetValue(  buttonIndex[ move.Value.Substring(0, 1).ToLowerInvariant() ]  );
        
            if (button != null)
            {
                yield return move;

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
                    break;
                }
            }
        }
    }

    static SimonComponentSolver()
    {
        _simonComponentType = ReflectionHelper.FindType("SimonComponent");
        _buttonsField = _simonComponentType.GetField("buttons", BindingFlags.Public | BindingFlags.Instance);
    }

    private static Type _simonComponentType = null;
    private static FieldInfo _buttonsField = null;
    private static readonly Dictionary<string, int> buttonIndex = new Dictionary<string, int>
    {
        {"r", 0}, {"b", 1}, {"g", 2}, {"y", 3}
    };

    private Array _buttons = null;
}
