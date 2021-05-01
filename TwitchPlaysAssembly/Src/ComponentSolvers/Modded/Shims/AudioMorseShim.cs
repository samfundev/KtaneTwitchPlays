using System;
using System.Collections;
using UnityEngine;

public class AudioMorseShim : ComponentSolverShim
{
	public AudioMorseShim(TwitchModule module)
		: base(module, "lgndAudioMorse")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<KMSelectable[]>("Buttons");
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		IEnumerator command = RespondToCommandUnshimmed(inputCommand);
		while (command.MoveNext())
			yield return command.Current;
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		bool[] ledStates = new bool[] { _component.GetValue<bool>("leftIsOn"), _component.GetValue<bool>("rightIsOn") };
		bool[] corStates = new bool[] { _component.GetValue<bool>("leftMustBeOn"), _component.GetValue<bool>("rightMustBeOn") };
		if (_component.GetValue<bool>("checking") && (ledStates[0] != corStates[0] || ledStates[1] != corStates[1]))
		{
			((MonoBehaviour) _component).StopAllCoroutines();
			yield break;
		}
		if (!_component.GetValue<bool>("checking"))
		{
			int disarm = _component.GetValue<int>("disarm");
			int left, right;
			switch (disarm)
			{
				case 0:
					left = 1;
					right = 2;
					break;
				case 1:
					left = 0;
					right = 2;
					break;
				default:
					left = 0;
					right = 1;
					break;
			}
			if (ledStates[0] != corStates[0])
				yield return DoInteractionClick(_buttons[left]);
			if (ledStates[1] != corStates[1])
				yield return DoInteractionClick(_buttons[right]);
			while (_component.GetValue<bool>("isPlaying")) yield return true;
			yield return DoInteractionClick(_buttons[disarm]);
		}
		while (!_component.GetValue<bool>("moduleSolved")) yield return true;
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("AudioMorseModuleScript");

	private readonly object _component;
	private readonly KMSelectable[] _buttons;
}
