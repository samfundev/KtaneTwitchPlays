using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class RoundKeypadComponentSolver : ComponentSolver
{
    public RoundKeypadComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _buttons = (Array)_buttonsField.GetValue(bombComponent.GetComponent(_componentType));
        modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        if (!inputCommand.StartsWith("press ", StringComparison.InvariantCultureIgnoreCase))
        {
            yield break;
        }
        inputCommand = inputCommand.Substring(6);

        string[] sequence = inputCommand.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        List<MonoBehaviour> buttons = new List<MonoBehaviour>();

        foreach (string buttonString in sequence)
        {
            int val = -1;
            if (!int.TryParse(buttonString, out val) || val < 1 || val > 8)
            {
                yield break;
            }
            MonoBehaviour button = (MonoBehaviour) _buttons.GetValue(val - 1);
            buttons.Add(button);
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

    static RoundKeypadComponentSolver()
    {
        _componentType = ReflectionHelper.FindType("AdvancedKeypad");
        _buttonsField = _componentType.GetField("Buttons", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private static Type _componentType = null;
    private static FieldInfo _buttonsField = null;

    private Array _buttons = null;
}
