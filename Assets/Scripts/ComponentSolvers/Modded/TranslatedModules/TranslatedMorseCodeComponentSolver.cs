using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class TranslatedMorseCodeComponentSolver : ComponentSolver
{
    public TranslatedMorseCodeComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _component = bombComponent.GetComponent(_morseCodeComponentType);
        _upButton = (MonoBehaviour)_upButtonField.GetValue(_component);
        _downButton = (MonoBehaviour)_downButtonField.GetValue(_component);
        _transmitButton = (MonoBehaviour)_transmitButtonField.GetValue(_component);
        modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        string[] commandParts = inputCommand.Split(' ');

        if (commandParts.Length != 2)
        {
            yield break;
        }

        if (!commandParts[0].Equals("transmit", StringComparison.InvariantCultureIgnoreCase) &&
            !commandParts[0].Equals("trans", StringComparison.InvariantCultureIgnoreCase) &&
            !commandParts[0].Equals("xmit", StringComparison.InvariantCultureIgnoreCase) &&
            !commandParts[0].Equals("tx", StringComparison.InvariantCultureIgnoreCase))
        {
            yield break;
        }

        int targetFrequency = 0;
        if (!int.TryParse(commandParts[1].Substring(commandParts[1].Length - 3), out targetFrequency))
        {
            yield break;
        }

        if (!Frequencies.Contains(targetFrequency))
        {
            yield break;
        }

        int initialFrequency = CurrentFrequency;
        MonoBehaviour buttonToShift = targetFrequency < initialFrequency ? _downButton : _upButton;

        while (CurrentFrequency != targetFrequency && (CurrentFrequency == initialFrequency || Mathf.Sign(CurrentFrequency - initialFrequency) != Mathf.Sign(CurrentFrequency - targetFrequency)))
        {
            yield return "change frequency";

            if (Canceller.ShouldCancel)
            {
                Canceller.ResetCancel();
                yield break;
            }

            yield return DoInteractionClick(buttonToShift);
        }

        if (CurrentFrequency == targetFrequency)
        {
            yield return "transmit";
            yield return DoInteractionClick(_transmitButton);
        }
    }    

    private int CurrentFrequency
    {
        get
        {
            return Frequencies[(int)_currentFrqIndexField.GetValue(_component)];
        }
    }

    static TranslatedMorseCodeComponentSolver()
    {
        _morseCodeComponentType = ReflectionHelper.FindType("MorseCodeTranslatedModule");
        _upButtonField = _morseCodeComponentType.GetField("ButtonRight", BindingFlags.Public | BindingFlags.Instance);
        _downButtonField = _morseCodeComponentType.GetField("ButtonLeft", BindingFlags.Public | BindingFlags.Instance);
        _transmitButtonField = _morseCodeComponentType.GetField("ButtonTX", BindingFlags.Public | BindingFlags.Instance);
        _currentFrqIndexField = _morseCodeComponentType.GetField("currentFrqIndex", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private static readonly int[] Frequencies = new int[]
    {
        505,
        515,
        522,
        532,
        535,
        542,
        545,
        552,
        555,
        565,
        572,
        575,
        582,
        592,
        595,
        600
    };

    private static Type _morseCodeComponentType = null;
    private static FieldInfo _upButtonField = null;
    private static FieldInfo _downButtonField = null;
    private static FieldInfo _transmitButtonField = null;
    private static FieldInfo _currentFrqIndexField = null;

    private Component _component = null;
    private MonoBehaviour _upButton = null;
    private MonoBehaviour _downButton = null;
    private MonoBehaviour _transmitButton = null;
}
