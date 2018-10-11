using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class TranslatedPasswordComponentSolver : ComponentSolver
{
	public TranslatedPasswordComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		_downButtons = (KMSelectable[]) _downButtonField.GetValue(bombComponent.GetComponent(_passwordComponentType));
		_submitButton = (MonoBehaviour) _submitButtonField.GetValue(bombComponent.GetComponent(_passwordComponentType));
		_display = (TextMesh[]) _displayField.GetValue(bombComponent.GetComponent(_passwordComponentType));
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "!{0} cycle 3 [cycle through the letters in column 3] | !{0} cycle 1 3 5 [cycle through the letters in columns 1, 3, and 5] | !{0} world [try to submit a word]");

		if (bombCommander == null) return;
		string language = TranslatedModuleHelper.GetManualCodeAddOn(bombComponent, bombComponent.GetComponent(_passwordComponentType), _passwordComponentType);
		if (language != null) modInfo.manualCode = $"Password{language}";
		modInfo.moduleDisplayName = $"Passwords Translated{TranslatedModuleHelper.GetModuleDisplayNameAddon(bombComponent, bombComponent.GetComponent(_passwordComponentType), _passwordComponentType)}";
		bombComponent.StartCoroutine(SetHeaderText());
	}

	private IEnumerator SetHeaderText()
	{
		yield return new WaitUntil(() => ComponentHandle != null);
		ComponentHandle.HeaderText = modInfo.moduleDisplayName;
	}

	private static string DeconstructHangulSyllableToJamos(char c)
	{
		int num = (c - '가') / 0x24C;
		int num2 = (c - '가') % 0x24C / 0x1C;
		int num3 = (c - '가') % 0x24C % 0x1C;
		char c2 = (char) (Convert.ToUInt16('ᄀ') + num);
		char c3 = (char) (Convert.ToUInt16('ᅡ') + num2);
		char c4 = (char) (Convert.ToUInt16('ᆨ') + num3 - 1);
		string text = c2.ToString() + c3.ToString() + ((num3 <= 0) ? string.Empty : c4.ToString());
		return text.Replace("ᅬ", "ᅩᅵ");
	}

	private static readonly Dictionary<char, char> _similarJamos = @"ᄀᆨ,ᄁᆩ,ᄂᆫ,ᄃᆮ,ᄅᆯ,ᄆᆷ,ᄇᆸ,ᄉᆺ,ᄊᆻ,ᄋᆼ,ᄌᆽ,ᄎᆾ,ᄏᆿ,ᄐᇀ,ᄑᇁ,ᄒᇂ".Split(',').ToDictionary(str => str[0], str => str[1]);

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.Trim();
		HashSet<int> alreadyCycled = new HashSet<int>();
		string[] commandParts = inputCommand.Split(' ');

		if (commandParts.Length > 0 && commandParts[0].Equals("cycle", StringComparison.InvariantCultureIgnoreCase))
		{
			foreach (string cycle in commandParts.Skip(1))
			{
				if (!int.TryParse(cycle, out int spinnerIndex) || !alreadyCycled.Add(spinnerIndex) || spinnerIndex < 1 || spinnerIndex > _downButtons.Length)
					continue;

				IEnumerator spinnerCoroutine = CycleCharacterSpinnerCoroutine(spinnerIndex - 1);
				while (spinnerCoroutine.MoveNext())
				{
					yield return spinnerCoroutine.Current;
				}
			}
			yield break;
		}

		// Special case for Korean (convert Hangul to Jamos)
		var lettersToSubmit = commandParts[0].Length > 0 && commandParts[0][0] >= '가' && commandParts[0][0] <= '힣'
			? commandParts[0].SelectMany(ch => DeconstructHangulSyllableToJamos(ch)).Select(ch => _similarJamos.ContainsKey(ch) ? _similarJamos[ch] : ch).Join("")
			: commandParts[0];

		if (lettersToSubmit.Length != 5)
			yield break;

		yield return "password";

		IEnumerator solveCoroutine = SolveCoroutine(lettersToSubmit);
		while (solveCoroutine.MoveNext())
		{
			yield return solveCoroutine.Current;
		}
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

	static TranslatedPasswordComponentSolver()
	{
		_passwordComponentType = ReflectionHelper.FindType("PasswordsTranslatedModule");
		_displayField = _passwordComponentType.GetField("DisplaySlots", BindingFlags.NonPublic | BindingFlags.Instance);
		_downButtonField = _passwordComponentType.GetField("ButtonsDown", BindingFlags.Public | BindingFlags.Instance);
		_submitButtonField = _passwordComponentType.GetField("ButtonSubmit", BindingFlags.Public | BindingFlags.Instance);
	}

	private static Type _passwordComponentType = null;
	private static FieldInfo _displayField = null;
	private static FieldInfo _submitButtonField = null;
	private static FieldInfo _downButtonField = null;

	private readonly MonoBehaviour _submitButton = null;
	private KMSelectable[] _downButtons = null;
	private readonly TextMesh[] _display = null;
}
