using System;
using System.Collections;
using System.Collections.Generic;
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
        modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        if(_buttons[0] == null)
        {
            _buttons[0] = (MonoBehaviour)_buttonsR.GetValue(c);
            _buttons[1] = (MonoBehaviour)_buttonsY.GetValue(c);
            _buttons[2] = (MonoBehaviour)_buttonsG.GetValue(c);
            _buttons[3] = (MonoBehaviour)_buttonsB.GetValue(c);
            if (_buttons[0] == null || _buttons[1] == null || _buttons[2] == null || _buttons[3] == null)
            {
                yield return "autosolve due to the buttons not having a valid MonoBehaviour assigned to them.";
                yield break;
            }
        }

        if (!inputCommand.StartsWith("press ", StringComparison.InvariantCultureIgnoreCase))
        {
            yield break;
        }
        inputCommand = inputCommand.Substring(6);

        string[] sequence = inputCommand.ToLowerInvariant().Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        List<MonoBehaviour> buttons = new List<MonoBehaviour>();

        foreach (string buttonString in sequence)
        {
            switch (buttonString)
            {
                case "red":
                case "r":
                    buttons.Add(_buttons[0]);
                    break;
                case "y":
                case "yellow":
                    buttons.Add(_buttons[1]);
                    break;
                case "g":
                case "green":
                    buttons.Add(_buttons[2]);
                    break;
                case "b":
                case "blue":
                    buttons.Add(_buttons[3]);
                    break;
                default:
                    yield break;
            }
        }
        yield return inputCommand;
        foreach (MonoBehaviour button in buttons)
        {
            if (Canceller.ShouldCancel)
            {
                Canceller.ResetCancel();
                yield break;
            }

            yield return DoInteractionClick(button);
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
