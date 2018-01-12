using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class MorseCodeComponentSolver : ComponentSolver
{
    public MorseCodeComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _upButton = (MonoBehaviour)_upButtonField.GetValue(bombComponent);
        _downButton = (MonoBehaviour)_downButtonField.GetValue(bombComponent);
        _transmitButton = (MonoBehaviour)_transmitButtonField.GetValue(bombComponent);
        modInfo = ComponentSolverFactory.GetModuleInfo("MorseCodeComponentSolver");
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        string[] commandParts = inputCommand.ToLowerInvariant().Split(' ');

        if (commandParts.Length != 2 || !commandParts[0].EqualsAny("transmit", "trans", "xmit", "tx", "submit") || commandParts[1].Length != 3)
        {
            yield break;
        }

        int targetFrequency = 0;
        if (!int.TryParse(commandParts[1], out targetFrequency) || !Frequencies.Contains(targetFrequency))
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
            int lastFrequency = CurrentFrequency;
            yield return DoInteractionClick(buttonToShift);
            if (lastFrequency == CurrentFrequency)
                break;
        }

        if (CurrentFrequency == targetFrequency)
        {
            yield return "transmit";
            yield return DoInteractionClick(_transmitButton);
        }
        else
        {
            yield return "unsubmittablepenalty";
        }
    }    

    private int CurrentFrequency
    {
        get
        {
            return (int)_currentFrequencyProperty.GetValue(BombComponent, null);
        }
    }

    static MorseCodeComponentSolver()
    {
        _morseCodeComponentType = ReflectionHelper.FindType("MorseCodeComponent");
        _upButtonField = _morseCodeComponentType.GetField("UpButton", BindingFlags.Public | BindingFlags.Instance);
        _downButtonField = _morseCodeComponentType.GetField("DownButton", BindingFlags.Public | BindingFlags.Instance);
        _transmitButtonField = _morseCodeComponentType.GetField("TransmitButton", BindingFlags.Public | BindingFlags.Instance);
        _currentFrequencyProperty = _morseCodeComponentType.GetProperty("CurrentFrequency", BindingFlags.Public | BindingFlags.Instance);
    }

    private static readonly int[] Frequencies = new int[]
    {
        502,
        505,
        512,
        515,
        522,
        525,
        532,
        535,
        542,
        545,
        552,
        555,
        562,
        565,
        572,
        575,
        582,
        585,
        592,
        595,
        600
    };

    private static Type _morseCodeComponentType = null;
    private static FieldInfo _upButtonField = null;
    private static FieldInfo _downButtonField = null;
    private static FieldInfo _transmitButtonField = null;
    private static PropertyInfo _currentFrequencyProperty = null;

    private MonoBehaviour _upButton = null;
    private MonoBehaviour _downButton = null;
    private MonoBehaviour _transmitButton = null;
}
