using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class PasswordComponentSolver : ComponentSolver
{
	public PasswordComponentSolver(BombCommander bombCommander, PasswordComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		_spinners = bombComponent.Spinners;
		_submitButton = bombComponent.SubmitButton;
		modInfo = ComponentSolverFactory.GetModuleInfo("PasswordComponentSolver", "!{0} cycle 3 [cycle through the letters in column 3] | !{0} cycle 1 3 5 [cycle through the letters in columns 1, 3, and 5] | !{0} world [try to submit a word]", "Password");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (!Regex.IsMatch(inputCommand, @"^[a-zA-Z]{5}$"))
		{
			HashSet<int> alreadyCycled = new HashSet<int>();
			string[] commandParts = inputCommand.Split(' ');
			if (!commandParts[0].Equals("cycle", StringComparison.InvariantCultureIgnoreCase)) yield break;

			foreach (string cycle in commandParts.Skip(1))
			{
				if (!int.TryParse(cycle, out int spinnerIndex) || !alreadyCycled.Add(spinnerIndex) || spinnerIndex < 1 || spinnerIndex > _spinners.Count)
					continue;

				IEnumerator spinnerCoroutine = CycleCharacterSpinnerCoroutine(_spinners[spinnerIndex-1]);
				while (spinnerCoroutine.MoveNext())
				{
					yield return spinnerCoroutine.Current;
				}
			}
		}
		else
		{
			yield return "password";

			IEnumerator solveCoroutine = SolveCoroutine(inputCommand);
			while (solveCoroutine.MoveNext())
			{
				yield return solveCoroutine.Current;
			}
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

	private IEnumerator SolveCoroutine(string word)
	{
		char[] characters = word.ToCharArray();
		for (int characterIndex = 0; characterIndex < characters.Length; ++characterIndex)
		{
			CharSpinner spinner = _spinners[characterIndex];
			IEnumerator subcoroutine = GetCharacterSpinnerToCharacterCoroutine(spinner, characters[characterIndex]);
			while (subcoroutine.MoveNext())
			{
				yield return subcoroutine.Current;
			}

			//Break out of the sequence if a column spinner doesn't have a matching character
			if (char.ToLowerInvariant(spinner.GetCurrentChar()) != char.ToLowerInvariant(characters[characterIndex]))
			{
				yield return "unsubmittablepenalty";
				yield break;
			}
		}

		yield return DoInteractionClick(_submitButton);
	}

	private IEnumerator GetCharacterSpinnerToCharacterCoroutine(CharSpinner spinner, char desiredCharacter)
	{
		MonoBehaviour downButton = spinner.DownButton;
		for (int hitCount = 0; hitCount < 6 && char.ToLowerInvariant(spinner.GetCurrentChar()) != char.ToLowerInvariant(desiredCharacter); ++hitCount)
		{
			yield return DoInteractionClick(downButton);
			yield return "trycancel";
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		while (!BombComponent.IsActive) yield return true;
		IEnumerator solve = RespondToCommandInternal(((PasswordComponent)BombComponent).CorrectWord);
		while (solve.MoveNext()) yield return solve.Current;
	}

	private List<CharSpinner> _spinners = null;
	private KeypadButton _submitButton = null;
}
