using System.Collections;
using System.Linq;
using UnityEngine;

public class MorseCodeComponentSolver : ComponentSolver
{
    public MorseCodeComponentSolver(BombCommander bombCommander, MorseCodeComponent bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
		_upButton = bombComponent.UpButton;
		_downButton = bombComponent.DownButton;
		_transmitButton = bombComponent.TransmitButton;
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
        KeypadButton buttonToShift = targetFrequency < initialFrequency ? _downButton : _upButton;

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
			return ((MorseCodeComponent) BombComponent).CurrentFrequency;
        }
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

    private KeypadButton _upButton = null;
    private KeypadButton _downButton = null;
    private KeypadButton _transmitButton = null;
}
