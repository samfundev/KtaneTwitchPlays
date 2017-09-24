using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class WhosOnFirstComponentSolver : ComponentSolver
{
    public WhosOnFirstComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _buttons = (Array)_buttonsField.GetValue(bombComponent);
        modInfo = ComponentSolverFactory.GetModuleInfo("WhosOnFirstComponentSolver");
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        foreach (object buttonObject in _buttons)
        {
            MonoBehaviour button = (MonoBehaviour)buttonObject;
            string buttonText = (string)_getTextMethod.Invoke(button, null);

            if (inputCommand.Equals(buttonText, StringComparison.InvariantCultureIgnoreCase))
            {
                yield return buttonText;
                yield return DoInteractionClick(button);
                break;
            }
        }
    }

    static WhosOnFirstComponentSolver()
    {
        _whosOnFirstComponentType = ReflectionHelper.FindType("WhosOnFirstComponent");
        _buttonsField = _whosOnFirstComponentType.GetField("Buttons", BindingFlags.Public | BindingFlags.Instance);

        _keypadButtonType = ReflectionHelper.FindType("KeypadButton");
        _getTextMethod = _keypadButtonType.GetMethod("GetText", BindingFlags.Public | BindingFlags.Instance);
    }

    private static Type _whosOnFirstComponentType = null;
    private static FieldInfo _buttonsField = null;

    private static Type _keypadButtonType = null;
    private static MethodInfo _getTextMethod = null;

    private Array _buttons = null;
}
