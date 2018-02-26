using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class TranslatedMorseCodeComponentSolver : ComponentSolver
{
    public TranslatedMorseCodeComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
        base(bombCommander, bombComponent)
	{
        _component = bombComponent.GetComponent(_morseCodeComponentType);
        _upButton = (MonoBehaviour)_upButtonField.GetValue(_component);
        _downButton = (MonoBehaviour)_downButtonField.GetValue(_component);
        _transmitButton = (MonoBehaviour)_transmitButtonField.GetValue(_component);
        modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		
		if (bombCommander != null)
		{
			string language = TranslatedModuleHelper.GetManualCodeAddOn(bombComponent);
			if (language != null) modInfo.manualCode = $"Morse%20Code{language}";
			modInfo.moduleDisplayName = $"Morse Code Translated{TranslatedModuleHelper.GetModuleDisplayNameAddon(bombComponent)}";
			bombComponent.StartCoroutine(SetHeaderText());
		}
	}

	private IEnumerator SetHeaderText()
	{
		yield return new WaitUntil(() => ComponentHandle != null);
		ComponentHandle.HeaderText = modInfo.moduleDisplayName;
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
	    if (!inputCommand.RegexMatch(out Match match, "^(?:tx|trans(?:mit)?|submit|xmit) (?:3.)?(5[0-9][25]|600)$") 
				|| !int.TryParse(match.Groups[1].Value, out int targetFrequency)
				|| !Frequencies.Contains(targetFrequency))
	    {
		    yield break;
	    }

        int initialFrequency = CurrentFrequency;
        MonoBehaviour buttonToShift = targetFrequency < initialFrequency ? _downButton : _upButton;

        while (CurrentFrequency != targetFrequency && (CurrentFrequency == initialFrequency || Math.Sign(CurrentFrequency - initialFrequency) != Math.Sign(CurrentFrequency - targetFrequency)))
        {
            yield return "change frequency";

            if (CoroutineCanceller.ShouldCancel)
            {
	            CoroutineCanceller.ResetCancel();
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

    private int CurrentFrequency => Frequencies[(int)_currentFrqIndexField.GetValue(_component)];

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
