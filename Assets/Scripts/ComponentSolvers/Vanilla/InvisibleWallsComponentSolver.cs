using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class InvisibleWallsComponentSolver : ComponentSolver
{
    public InvisibleWallsComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _buttons = (IList)_buttonsField.GetValue(bombComponent);

        helpMessage = "!{0} move up down left right, !{0} move udlr [make a series of white icon moves]";
        manualCode = "Mazes";
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {     
        if (!inputCommand.StartsWith("move ", StringComparison.InvariantCultureIgnoreCase))
        {
            yield break;
        }

        inputCommand = inputCommand.Substring(5);

        int beforeButtonStrikeCount = StrikeCount;

        foreach (Match move in Regex.Matches(inputCommand, @"[udlr]", RegexOptions.IgnoreCase))
        {
            MonoBehaviour button = (MonoBehaviour)_buttons[  buttonIndex[ move.Value.ToLowerInvariant() ]  ];
            
            if (button != null)
            {
                yield return move.Value;

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

    static InvisibleWallsComponentSolver()
    {
        _invisibleWallsComponentType = ReflectionHelper.FindType("InvisibleWallsComponent");
        _buttonsField = _invisibleWallsComponentType.GetField("Buttons", BindingFlags.Public | BindingFlags.Instance);
    }

    private static Type _invisibleWallsComponentType = null;
    private static FieldInfo _buttonsField = null;
    private static readonly Dictionary<string, int> buttonIndex = new Dictionary<string, int>
    {
        {"u", 0}, {"l", 1}, {"r", 2}, {"d", 3}
    };

    private IList _buttons = null;
}
