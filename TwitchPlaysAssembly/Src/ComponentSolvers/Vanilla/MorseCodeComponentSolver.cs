using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class MorseCodeComponentSolver : ComponentSolver
{
    public MorseCodeComponentSolver(BombCommander bombCommander, MorseCodeComponent bombComponent) :
        base(bombCommander, bombComponent)
	{
		_upButton = bombComponent.UpButton;
		_downButton = bombComponent.DownButton;
		_transmitButton = bombComponent.TransmitButton;
        modInfo = ComponentSolverFactory.GetModuleInfo("MorseCodeComponentSolver");
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
	    if (!inputCommand.RegexMatch(out Match match, "^(?:tx|trans(?:mit)?|submit|xmit) (?:3.)?(5[0-9][25]|600)$") || !int.TryParse(match.Groups[1].Value, out int targetFrequency))
	    {
		    yield break;
	    }

        int initialFrequency = CurrentFrequency;
        KeypadButton buttonToShift = targetFrequency < initialFrequency ? _downButton : _upButton;

        while (CurrentFrequency != targetFrequency && (CurrentFrequency == initialFrequency || Math.Sign(CurrentFrequency - initialFrequency) != Math.Sign(CurrentFrequency - targetFrequency)))
        {
            yield return "change frequency";

	        yield return "trycancel";
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

    private int CurrentFrequency => ((MorseCodeComponent) BombComponent).CurrentFrequency;

	private KeypadButton _upButton = null;
    private KeypadButton _downButton = null;
    private KeypadButton _transmitButton = null;
}
