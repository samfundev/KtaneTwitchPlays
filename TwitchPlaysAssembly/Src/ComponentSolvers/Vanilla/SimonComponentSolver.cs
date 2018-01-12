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
            MonoBehaviour button = (MonoBehaviour)_buttons.GetValue(  buttonIndex[ move.Value.Substring(0, 1).ToLowerInvariant() ]  );
            

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
