using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[ModuleID("PasswordsTranslated")]
public class TranslatedPasswordComponentSolver : ComponentSolver
{
	public TranslatedPasswordComponentSolver(TwitchModule module) :
		base(module)
	{
		_component = module.BombComponent.GetComponent(PasswordComponentType);
		_downButtons = (KMSelectable[]) DownButtonField.GetValue(_component);
		_submitButton = (MonoBehaviour) SubmitButtonField.GetValue(_component);
		_display = (TextMesh[]) DisplayField.GetValue(_component);
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "!{0} cycle 1 3 5 [cycle through the letters in columns 1, 3, and 5] | !{0} toggle [move all columns down one letter] | !{0} world [try to submit a word]").Clone();

		LanguageCode = TranslatedModuleHelper.GetLanguageCode(_component, PasswordComponentType);
		ModInfo.moduleDisplayName = $"Passwords Translated{TranslatedModuleHelper.GetModuleDisplayNameAddon(_component, PasswordComponentType)}";
		Module.HeaderText = ModInfo.moduleDisplayName;
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.Trim();
		string[] commandParts = inputCommand.Split(' ');

		if (commandParts.Length == 1 && commandParts[0].Equals("toggle", StringComparison.InvariantCultureIgnoreCase))
		{
			yield return "password";
			for (int i = 0; i < 5; i++)
				yield return DoInteractionClick(_downButtons[i]);
			yield break;
		}

		if (commandParts.Length > 0 && commandParts[0].Equals("cycle", StringComparison.InvariantCultureIgnoreCase))
		{
			HashSet<int> alreadyCycled = new HashSet<int>();
			foreach (string cycle in commandParts.Skip(1))
			{
				if (!int.TryParse(cycle, out int spinnerIndex) || !alreadyCycled.Add(spinnerIndex) || spinnerIndex < 1 || spinnerIndex > _downButtons.Length)
					continue;

				IEnumerator spinnerCoroutine = CycleCharacterSpinnerCoroutine(spinnerIndex - 1);
				while (spinnerCoroutine.MoveNext())
					yield return spinnerCoroutine.Current;
			}
			yield break;
		}

		string lettersToSubmit =
			// Special case for Korean (convert Hangul to Jamos)
			commandParts[0].Length > 0 && commandParts[0][0] >= '가' && commandParts[0][0] <= '힣'
				? commandParts[0].SelectMany(ch => TranslatedModulesSettings.CallMethod<string>("DeconstructHangulSyllableToJamos", null, ch)).Select(ch => SimilarJamos.ContainsKey(ch) ? SimilarJamos[ch] : ch).Join("") :
			// Special case for Hebrew (expects input back to front)
			TranslatedModuleHelper.GetLanguageCode(_component, PasswordComponentType) == "he"
				? commandParts[0].Reverse().Join("") :
			// Usual case
			commandParts[0];

		if (lettersToSubmit.Length != 5)
			yield break;

		yield return "password";

		IEnumerator solveCoroutine = SolveCoroutine(lettersToSubmit);
		while (solveCoroutine.MoveNext())
			yield return solveCoroutine.Current;
	}

	private IEnumerator CycleCharacterSpinnerCoroutine(int index)
	{
		yield return "cycle";

		KMSelectable downButton = _downButtons[index];

		for (int hitCount = 0; hitCount < 6; ++hitCount)
		{
			yield return DoInteractionClick(downButton);
			yield return "trywaitcancel 1.0";
		}
	}

	private IEnumerator SolveCoroutine(string characters)
	{
		for (int characterIndex = 0; characterIndex < characters.Length; ++characterIndex)
		{
			IEnumerator subcoroutine = GetCharacterSpinnerToCharacterCoroutine(characterIndex, characters.Substring(characterIndex, 1));
			while (subcoroutine.MoveNext())
				yield return subcoroutine.Current;

			//Break out of the sequence if a column spinner doesn't have a matching character
			if (_display[characterIndex].text.Equals(characters.Substring(characterIndex, 1), StringComparison.InvariantCultureIgnoreCase))
				continue;
			yield return "unsubmittablepenalty";
			yield break;
		}

		yield return DoInteractionClick(_submitButton);
	}

	private IEnumerator GetCharacterSpinnerToCharacterCoroutine(int index, string desiredCharacter)
	{
		KMSelectable downButton = _downButtons[index];
		for (int hitCount = 0; hitCount < 6 && !_display[index].text.Equals(desiredCharacter, StringComparison.InvariantCultureIgnoreCase); ++hitCount)
		{
			yield return DoInteractionClick(downButton);
			yield return "trycancel";
		}
	}

	private static readonly Type PasswordComponentType = ReflectionHelper.FindType("PasswordsTranslatedModule");
	private static readonly FieldInfo DisplayField = PasswordComponentType.GetField("DisplaySlots", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo SubmitButtonField = PasswordComponentType.GetField("ButtonSubmit", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo DownButtonField = PasswordComponentType.GetField("ButtonsDown", BindingFlags.Public | BindingFlags.Instance);
	private static readonly Dictionary<char, char> SimilarJamos = PasswordComponentType.GetValue<Dictionary<char, char>>("SimilarJamos");
	private static readonly Type TranslatedModulesSettings = ReflectionHelper.FindType("TranslatedModulesSettings");

	private readonly MonoBehaviour _submitButton;
	private readonly KMSelectable[] _downButtons;
	private readonly TextMesh[] _display;
	private readonly Component _component;
}
