using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class RoundKeypadComponentSolver : ComponentSolver
{
    public RoundKeypadComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _buttons = (Array)_buttonsField.GetValue(bombComponent.GetComponent(_componentType));
        helpMessage = "Solve the module with !{0} press 2 4 6 7 8. Button 1 is the top most botton, and are numbered in clockwise order.";
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        if (!inputCommand.StartsWith("press ", StringComparison.InvariantCultureIgnoreCase))
        {
            yield break;
        }
        inputCommand = inputCommand.Substring(6);

        int beforeButtonStrikeCount = StrikeCount;

        string[] sequence = inputCommand.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string buttonString in sequence)
        {
            int val = -1;
            if(int.TryParse(buttonString, out val) && val >= 1 && val <= 8)
            {
                MonoBehaviour button = (MonoBehaviour)_buttons.GetValue(val-1);

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

    static RoundKeypadComponentSolver()
    {
        _componentType = ReflectionHelper.FindType("AdvancedKeypad");
        _buttonsField = _componentType.GetField("Buttons", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private static Type _componentType = null;
    private static FieldInfo _buttonsField = null;

    private Array _buttons = null;
}
