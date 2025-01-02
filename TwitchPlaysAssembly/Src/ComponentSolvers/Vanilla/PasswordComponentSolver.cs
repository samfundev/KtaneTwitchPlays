using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class PasswordComponentSolver : ComponentSolver
{
	public PasswordComponentSolver(TwitchModule module) :
		base(module)
	{
		var passwordModule = (PasswordComponent) module.BombComponent;
		_spinners = passwordModule.Spinners;
		_submitButton = passwordModule.SubmitButton;
		SetHelpMessage("!{0} cycle 1 3 5 [cycle through the letters in columns 1, 3, and 5] | !{0} cycle [cycle through all columns] | !{0} toggle [move all columns down one letter] | !{0} world [try to submit a word]");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		Match m;

		if (Regex.IsMatch(inputCommand, @"^\s*toggle\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
		{
			yield return "password";
			for (int i = 0; i < 5; i++)
				yield return DoInteractionClick(_spinners[i].DownButton);
		}
		else if (Regex.IsMatch(inputCommand, @"^\s*cycle\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
		{
			yield return "password";
			for (int i = 0; i < 5; i++)
			{
				IEnumerator spinnerCoroutine = CycleCharacterSpinnerCoroutine(_spinners[i]);
				while (spinnerCoroutine.MoveNext())
					yield return spinnerCoroutine.Current;
			}
		}
		else if ((m = Regex.Match(inputCommand, @"^\s*cycle\s+([ \d]+)\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)).Success)
		{
			var slots = new HashSet<int>();
			foreach (var piece in m.Groups[1].Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
			{
				if (!int.TryParse(piece, out var i) || i < 1 || i > 5)
				{
					yield return string.Format("sendtochaterror “{0}” is not a number from 1 to 5.", piece);
					yield break;
				}
				slots.Add(i);
			}
			if (slots.Count > 0)
			{
				yield return "password";
				foreach (var slot in slots)
				{
					IEnumerator spinnerCoroutine = CycleCharacterSpinnerCoroutine(_spinners[slot - 1]);
					while (spinnerCoroutine.MoveNext())
						yield return spinnerCoroutine.Current;
				}
			}
		}
		else if ((m = Regex.Match(inputCommand, @"^\s*(\S{5})\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)).Success)
		{
			yield return "password";

			char[] characters = m.Groups[1].Value.ToLowerInvariant().ToCharArray();
			for (int ix = 0; ix < characters.Length; ++ix)
			{
				CharSpinner spinner = _spinners[ix];
				IEnumerator subcoroutine = GetCharacterSpinnerToCharacterCoroutine(spinner, characters[ix]);
				while (subcoroutine.MoveNext())
					yield return subcoroutine.Current;

				//Break out of the sequence if a column spinner doesn't have a matching character
				if (char.ToLowerInvariant(spinner.GetCurrentChar()) ==
					char.ToLowerInvariant(characters[ix])) continue;
				yield return "unsubmittablepenalty";
				yield break;
			}

			yield return DoInteractionClick(_submitButton);
		}
	}

	private IEnumerator CycleCharacterSpinnerCoroutine(CharSpinner spinner)
	{
		yield return "cycle";

		KeypadButton downButton = spinner.DownButton;

		for (int hitCount = 0; hitCount < 6; ++hitCount)
		{
			yield return DoInteractionClick(downButton);
			yield return "trywaitcancel 1.0";
		}
	}

	private IEnumerator GetCharacterSpinnerToCharacterCoroutine(CharSpinner spinner, char desiredCharacter)
	{
		var options = spinner.Options;
		var targetIndex = options.IndexOf(char.ToUpperInvariant(desiredCharacter));
		if (targetIndex == -1)
			yield break;

		yield return SelectIndex(options.IndexOf(spinner.GetCurrentChar()), targetIndex, options.Count, spinner.DownButton, spinner.UpButton);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		while (!Module.BombComponent.IsActive) yield return true;
		IEnumerator solve = RespondToCommandInternal(((PasswordComponent) Module.BombComponent).CorrectWord);
		while (solve.MoveNext()) yield return solve.Current;
	}

	private readonly List<CharSpinner> _spinners;
	private readonly KeypadButton _submitButton;
}
