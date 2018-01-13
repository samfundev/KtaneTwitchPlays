using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class PasswordComponentSolver : ComponentSolver
{
    public PasswordComponentSolver(BombCommander bombCommander, PasswordComponent bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
		_spinners = bombComponent.Spinners;
		_submitButton = bombComponent.SubmitButton;
        modInfo = ComponentSolverFactory.GetModuleInfo("PasswordComponentSolver");
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        if (!Regex.IsMatch(inputCommand, @"^[a-zA-Z]{5}$"))
        {
            string[] commandParts = inputCommand.Split(' ');
            if (commandParts.Length > 2)
            {
                yield break;
            }

            if (commandParts[0].Equals("claim"))
            {
                yield break;
            }

            if (commandParts[0].Equals("cycle", StringComparison.InvariantCultureIgnoreCase))
            {
                int spinnerIndex = 1;
                if (commandParts.Length == 2 && !int.TryParse(commandParts[1], out spinnerIndex))
                {
                    yield break;
                }

                spinnerIndex--;

                if (spinnerIndex >= 0 && spinnerIndex < _spinners.Count)
                {
                    IEnumerator spinnerCoroutine = CycleCharacterSpinnerCoroutine(_spinners[spinnerIndex]);
                    while (spinnerCoroutine.MoveNext())
                    {
                        yield return spinnerCoroutine.Current;
                    }
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
            if (Canceller.ShouldCancel)
            {
                Canceller.ResetCancel();
                yield break;
            }

            yield return DoInteractionClick(downButton);
            yield return new WaitForSeconds(1.0f);
        }
    }

    private IEnumerator SolveCoroutine(string word)
    {
        char[] characters = word.ToCharArray();
        for (int characterIndex = 0; characterIndex < characters.Length; ++characterIndex)
        {
            if (Canceller.ShouldCancel)
            {
                Canceller.ResetCancel();
                yield break;
            }

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
        }
    }

    private List<CharSpinner> _spinners = null;
    private KeypadButton _submitButton = null;
}
