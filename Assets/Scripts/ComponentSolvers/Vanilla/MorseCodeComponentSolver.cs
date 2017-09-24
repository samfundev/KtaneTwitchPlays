using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class MorseCodeComponentSolver : ComponentSolver
{
    public MorseCodeComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _upButton = (MonoBehaviour)_upButtonField.GetValue(bombComponent);
        _downButton = (MonoBehaviour)_downButtonField.GetValue(bombComponent);
        _transmitButton = (MonoBehaviour)_transmitButtonField.GetValue(bombComponent);

        helpMessage = "!{0} transmit 3.573, !{0} trans 573, !{0} tx 573 [transmit frequency 3.573]";
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

            DoInteractionStart(buttonToShift);
            yield return new WaitForSeconds(0.1f);
            DoInteractionEnd(buttonToShift);
        }

        if (CurrentFrequency == targetFrequency)
        {
            yield return "transmit";

            DoInteractionStart(_transmitButton);
            yield return new WaitForSeconds(0.1f);
            DoInteractionEnd(_transmitButton);
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
