using System;
using System.Collections;
using System.Text.RegularExpressions;

public class MorseCodeComponentSolver : ComponentSolver
{
	public MorseCodeComponentSolver(TwitchModule module) :
		base(module)
	{
		var morseModule = (MorseCodeComponent) module.BombComponent;
		_upButton = morseModule.UpButton;
		_downButton = morseModule.DownButton;
		_transmitButton = morseModule.TransmitButton;
		ModInfo = ComponentSolverFactory.GetModuleInfo("Morse", "!{0} transmit 3.573, !{0} trans 573, !{0} transmit 3.573 MHz, !{0} tx 573 [transmit frequency 3.573]");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.Trim();
		if (!inputCommand.RegexMatch(out Match match,
				"^(?:tx|trans(?:mit)?|submit|xmit) (?:3.)?(5[0-9][25]|600)( ?mhz)?$") ||
			!int.TryParse(match.Groups[1].Value, out int targetFrequency))
			yield break;

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
			yield return "unsubmittablepenalty";
	}

	private int CurrentFrequency => ((MorseCodeComponent) Module.BombComponent).CurrentFrequency;

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		while (!Module.BombComponent.IsActive) yield return true;
		IEnumerator solve = RespondToCommandInternal($"tx {((MorseCodeComponent) Module.BombComponent).ChosenFrequency}");
		while (solve.MoveNext()) yield return solve.Current;
	}

	private readonly KeypadButton _upButton;
	private readonly KeypadButton _downButton;
	private readonly KeypadButton _transmitButton;
}
