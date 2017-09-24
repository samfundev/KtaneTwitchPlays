using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class MemoryComponentSolver : ComponentSolver
{
    public MemoryComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _buttons = (Array)_buttonsField.GetValue(bombComponent);
        modInfo = ComponentSolverFactory.GetModuleInfo("MemoryComponentSolver");
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        string[] commandParts = inputCommand.Split(' ');

        if (commandParts.Length != 2)
        {
            yield break;
        }

        int buttonNumber = 0;
        if (!int.TryParse(commandParts[1], out buttonNumber))
        {
            yield break;
        }

        if (buttonNumber >= 1 && buttonNumber <= 4)
        {
            if (commandParts[0].Equals("position", StringComparison.InvariantCultureIgnoreCase) ||
                commandParts[0].Equals("pos", StringComparison.InvariantCultureIgnoreCase) ||
                commandParts[0].Equals("p", StringComparison.InvariantCultureIgnoreCase))
            {
                yield return "position";

                MonoBehaviour button = (MonoBehaviour)_buttons.GetValue(buttonNumber - 1);
                yield return DoInteractionClick(button);
            }
            else if (commandParts[0].Equals("label", StringComparison.InvariantCultureIgnoreCase) ||
                    commandParts[0].Equals("lab", StringComparison.InvariantCultureIgnoreCase) ||
                    commandParts[0].Equals("l", StringComparison.InvariantCultureIgnoreCase))
            {
                foreach(object buttonObject in _buttons)
                {
                    MonoBehaviour button = (MonoBehaviour)buttonObject;
                    string buttonText = (string)_getTextMethod.Invoke(button, null);
                    if (buttonText.Equals(buttonNumber.ToString()))
                    {
                        yield return "label";
                        yield return DoInteractionClick(button);
                        break;
                    }
                }
            }
        }
    }

    static MemoryComponentSolver()
    {
        _memoryComponentType = ReflectionHelper.FindType("MemoryComponent");
        _buttonsField = _memoryComponentType.GetField("Buttons", BindingFlags.Public | BindingFlags.Instance);

        _keypadButtonType = ReflectionHelper.FindType("KeypadButton");
        _getTextMethod = _keypadButtonType.GetMethod("GetText", BindingFlags.Public | BindingFlags.Instance);
    }

    private static Type _memoryComponentType = null;
    private static FieldInfo _buttonsField = null;

    private static Type _keypadButtonType = null;
    private static MethodInfo _getTextMethod = null;

    private Array _buttons = null;
}
