using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

[ModuleID("MorseCodeTranslated")]
public class TranslatedMorseCodeComponentSolver : ComponentSolver
{
	public TranslatedMorseCodeComponentSolver(TwitchModule module) :
		base(module)
	{
		_component = module.BombComponent.GetComponent(MorseCodeComponentType);
		_upButton = (MonoBehaviour) UpButtonField.GetValue(_component);
		_downButton = (MonoBehaviour) DownButtonField.GetValue(_component);
		_transmitButton = (MonoBehaviour) TransmitButtonField.GetValue(_component);
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "!{0} transmit 3.573, !{0} trans 573, !{0} transmit 3.573 MHz, !{0} tx 573 [transmit frequency 3.573]").Clone();

		LanguageCode = TranslatedModuleHelper.GetLanguageCode(_component, MorseCodeComponentType);
		ModInfo.moduleDisplayName = $"Morse Code Translated{TranslatedModuleHelper.GetModuleDisplayNameAddon(_component, MorseCodeComponentType)}";
		Module.HeaderText = ModInfo.moduleDisplayName;
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (!inputCommand.Trim().RegexMatch(out Match match, "^(?:tx|trans(?:mit)?|submit|xmit) (?:3.)?(5[0-9][25]|600)( ?mhz)?$")
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
			yield return "trycancel";
			yield return DoInteractionClick(buttonToShift);
		}

		if (CurrentFrequency == targetFrequency)
		{
			yield return "transmit";
			yield return DoInteractionClick(_transmitButton);
		}
	}

	private int CurrentFrequency => Frequencies[(int) CurrentFrqIndexField.GetValue(_component)];

	private static readonly int[] Frequencies = {
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

	private static readonly Type MorseCodeComponentType = ReflectionHelper.FindType("MorseCodeTranslatedModule");
	private static readonly FieldInfo UpButtonField = MorseCodeComponentType.GetField("ButtonRight", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo DownButtonField = MorseCodeComponentType.GetField("ButtonLeft", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo TransmitButtonField = MorseCodeComponentType.GetField("ButtonTX", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo CurrentFrqIndexField = MorseCodeComponentType.GetField("currentFrqIndex", BindingFlags.NonPublic | BindingFlags.Instance);

	private readonly Component _component;
	private readonly MonoBehaviour _upButton;
	private readonly MonoBehaviour _downButton;
	private readonly MonoBehaviour _transmitButton;
}
