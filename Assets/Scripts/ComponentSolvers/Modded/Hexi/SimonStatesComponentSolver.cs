using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class SimonStatesComponentSolver : ComponentSolver
{
    private Component c;

    public SimonStatesComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        c = bombComponent.GetComponent(_componentType);
        _buttons = new MonoBehaviour[4];
        helpMessage = "Enter the response with !{0} press B Y R G.";
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        if(_buttons[0] == null)
        {
            _buttons[0] = (MonoBehaviour)_buttonsR.GetValue(c);
            _buttons[1] = (MonoBehaviour)_buttonsY.GetValue(c);
            _buttons[2] = (MonoBehaviour)_buttonsG.GetValue(c);
            _buttons[3] = (MonoBehaviour)_buttonsB.GetValue(c);
            if(_buttons[0] == null) yield break;
        }

        if (!inputCommand.StartsWith("press ", StringComparison.InvariantCultureIgnoreCase))
        {
            yield break;
        }
        inputCommand = inputCommand.Substring(6);

        int beforeButtonStrikeCount = StrikeCount;

        string[] sequence = inputCommand.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string buttonString in sequence)
        {
            MonoBehaviour button = null;

            if (buttonString.Equals("r", StringComparison.InvariantCultureIgnoreCase) || buttonString.Equals("red", StringComparison.InvariantCultureIgnoreCase))
            {
                button = _buttons[0];
            }
            else if (buttonString.Equals("y", StringComparison.InvariantCultureIgnoreCase) || buttonString.Equals("yellow", StringComparison.InvariantCultureIgnoreCase))
            {
                button = _buttons[1];
            }
            else if (buttonString.Equals("g", StringComparison.InvariantCultureIgnoreCase) || buttonString.Equals("green", StringComparison.InvariantCultureIgnoreCase))
            {
                button = _buttons[2];
            }
            else if (buttonString.Equals("b", StringComparison.InvariantCultureIgnoreCase) || buttonString.Equals("blue", StringComparison.InvariantCultureIgnoreCase))
            {
                button = _buttons[3];
            }

            if (button != null)
            {
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
                    break;
                }
            }
        }
    }

    static SimonStatesComponentSolver()
    {
        _componentType = ReflectionHelper.FindType("AdvancedSimon");
        _buttonsR = _componentType.GetField("ButtonRed", BindingFlags.NonPublic | BindingFlags.Instance);
        _buttonsY = _componentType.GetField("ButtonYellow", BindingFlags.NonPublic | BindingFlags.Instance);
        _buttonsG = _componentType.GetField("ButtonGreen", BindingFlags.NonPublic | BindingFlags.Instance);
        _buttonsB = _componentType.GetField("ButtonBlue", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private static Type _componentType = null;
    private static FieldInfo _buttonsR = null, _buttonsY = null, _buttonsG = null, _buttonsB = null;

    private MonoBehaviour[] _buttons = null;
}
