using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ModuleID("reverseMorse")]
public class ReverseMorseShim : ComponentSolverShim
{
	public ReverseMorseShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_dot = _component.GetValue<KMSelectable>("dotButton");
		_dash = _component.GetValue<KMSelectable>("dashButton");
		_break = _component.GetValue<KMSelectable>("breakButton");
		_space = _component.GetValue<KMSelectable>("spaceButton");
		_reset = _component.GetValue<KMSelectable>("resetButton");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		List<string> letters;
		string[] submitted = _component.GetValue<string[]>("submittedLetters");
		string input = _component.GetValue<string>("typingLetters");
		int index = _component.GetValue<int>("letterIndex");
		if (_component.GetValue<bool>("strikeAlarm"))
		{
			((MonoBehaviour) _component).StopAllCoroutines();
			yield break;
		}
		while (_component.GetValue<bool>("transmitting")) yield return true;
		int end = _component.GetValue<bool>("stage1") ? 1 : 2;
		for (int i = 0; i < end; i++)
		{
			letters = (i == 1) ? _component.GetValue<List<string>>("selectedLetters2").ToList() : _component.GetValue<List<string>>("selectedLetters1").ToList();
			letters = ConvertToMorse(letters);
			for (int j = 0; j < (index + 1); j++)
			{
				if (j == index)
				{
					if (input.Length > letters[j].Length)
					{
						yield return DoInteractionClick(_reset);
						index = 0;
						input = "";
						goto skip;
					}
					for (int k = 0; k < input.Length; k++)
					{
						if (k == letters[j].Length)
							goto skip;
						else if (input[k] != letters[j][k])
						{
							yield return DoInteractionClick(_reset);
							index = 0;
							input = "";
							goto skip;
						}
					}
				}
				else if (letters[j] != submitted[j])
				{
					yield return DoInteractionClick(_reset);
					index = 0;
					input = "";
					goto skip;
				}
			}
			skip:
			int start = index;
			for (int n = start; n < 6; n++)
			{
				for (int k = input.Length; k < letters[n].Length; k++)
				{
					if (letters[n][k] == '-')
						yield return DoInteractionClick(_dash);
					else
						yield return DoInteractionClick(_dot);
				}
				input = "";
				yield return DoInteractionClick(_break);
			}
			yield return DoInteractionClick(_space);
			while (_component.GetValue<bool>("transmitting")) yield return true;
		}
	}

	List<string> ConvertToMorse(List<string> chars)
	{
		string[] letters = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
		string[] morse = { ".-", "-...", "-.-.", "-..", ".", "..-.", "--.", "....", "..", ".---", "-.-", ".-..", "--", "-.", "---", ".--.", "--.-", ".-.", "...", "-", "..-", "...-", ".--", "-..-", "-.--", "--..", ".----", "..---", "...--", "....-", ".....", "-....", "--...", "---..", "----.", "-----" };
		for (int i = 0; i < chars.Count; i++)
		{
			chars[i] = morse[Array.IndexOf(letters, chars[i])];
		}
		return chars;
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("reverseMorseScript", "reverseMorse");

	private readonly object _component;
	private readonly KMSelectable _dot;
	private readonly KMSelectable _dash;
	private readonly KMSelectable _break;
	private readonly KMSelectable _space;
	private readonly KMSelectable _reset;
}
