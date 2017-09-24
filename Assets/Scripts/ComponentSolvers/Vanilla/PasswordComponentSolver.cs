using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class PasswordComponentSolver : ComponentSolver
{
    public PasswordComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _spinners = (IList)_spinnersField.GetValue(bombComponent);
        _submitButton = (MonoBehaviour)_submitButtonField.GetValue(bombComponent);
        
        helpMessage = "!{0} cycle 3 [cycle through the letters in column 3] | !{0} world [try to submit a word]";
        manualCode = "Passwords";
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        if (inputCommand.Equals("claim", StringComparison.InvariantCultureIgnoreCase))
        {
            yield break;
        }
        else if (inputCommand.Equals("cycle", StringComparison.InvariantCultureIgnoreCase))
        {
            IEnumerator spinnerCoroutine = CycleCharacterSpinnerCoroutine(GetCharSpinner(0));
            while (spinnerCoroutine.MoveNext())
            {
                yield return spinnerCoroutine.Current;
            }
        }
        else if (Regex.IsMatch(inputCommand, @"^[a-zA-Z]{5}$"))
        {
            yield return "password";

            IEnumerator solveCoroutine = SolveCoroutine(inputCommand);
            while (solveCoroutine.MoveNext())
            {
                yield return solveCoroutine.Current;
            }
        }
        else
        {
            string[] commandParts = inputCommand.Split(' ');
            if (commandParts.Length != 2)
            {
                yield break;
            }

            if (commandParts[0].Equals("cycle", StringComparison.InvariantCultureIgnoreCase))
            {
                int spinnerIndex = 0;
                if (!int.TryParse(commandParts[1], out spinnerIndex))
                {
                    yield break;
                }

                spinnerIndex--;

                if (spinnerIndex >= 0 && spinnerIndex < _spinners.Count)
                {
                    IEnumerator spinnerCoroutine = CycleCharacterSpinnerCoroutine(GetCharSpinner(spinnerIndex));
                    while (spinnerCoroutine.MoveNext())
                    {
                        yield return spinnerCoroutine.Current;
                    }
                }
            }
        }
    }

    private IEnumerator CycleCharacterSpinnerCoroutine(MonoBehaviour spinner)
    {
        yield return "cycle";

        MonoBehaviour downButton = (MonoBehaviour)_downButtonField.GetValue(spinner);

        for (int hitCount = 0; hitCount < 6; ++hitCount)
        {
            if (Canceller.ShouldCancel)
            {
                Canceller.ResetCancel();
                yield break;
            }

            DoInteractionStart(downButton);
            yield return new WaitForSeconds(0.1f);
            DoInteractionEnd(downButton);
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

            MonoBehaviour spinner = GetCharSpinner(characterIndex);
            IEnumerator subcoroutine = GetCharacterSpinnerToCharacterCoroutine(spinner, characters[characterIndex]);
            while (subcoroutine.MoveNext())
            {
                yield return subcoroutine.Current;
            }

            //Break out of the sequence if a column spinner doesn't have a matching character
            if (GetCurrentChar(spinner) != characters[characterIndex])
            {
                yield break;
            }
        }

        DoInteractionStart(_submitButton);
        yield return new WaitForSeconds(0.1f);
        DoInteractionEnd(_submitButton);
    }

    private IEnumerator GetCharacterSpinnerToCharacterCoroutine(MonoBehaviour spinner, char desiredCharacter)
    {
        MonoBehaviour downButton = (MonoBehaviour)_downButtonField.GetValue(spinner);
        for (int hitCount = 0; hitCount < 6 && char.ToLower(GetCurrentChar(spinner)) != char.ToLower(desiredCharacter); ++hitCount)
        {
            DoInteractionStart(downButton);
            yield return new WaitForSeconds(0.1f);
            DoInteractionEnd(downButton);
        }
    }

    private MonoBehaviour GetCharSpinner(int spinnerIndex)
    {
        return (MonoBehaviour)_spinners[spinnerIndex];
    }

    private char GetCurrentChar(int spinnerIndex)
    {
        return GetCurrentChar(GetCharSpinner(spinnerIndex));
    }

    private char GetCurrentChar(MonoBehaviour spinner)
    {
        return (char)_getCurrentCharMethod.Invoke(spinner, null);
    }

    static PasswordComponentSolver()
    {
        _passwordComponentType = ReflectionHelper.FindType("PasswordComponent");
        _spinnersField = _passwordComponentType.GetField("Spinners", BindingFlags.Public | BindingFlags.Instance);
        _submitButtonField = _passwordComponentType.GetField("SubmitButton", BindingFlags.Public | BindingFlags.Instance);

        _charSpinnerType = ReflectionHelper.FindType("CharSpinner");
        _downButtonField = _charSpinnerType.GetField("DownButton", BindingFlags.Public | BindingFlags.Instance);
        _getCurrentCharMethod = _charSpinnerType.GetMethod("GetCurrentChar", BindingFlags.Public | BindingFlags.Instance);
    }

    private static Type _passwordComponentType = null;
    private static Type _charSpinnerType = null;
    private static FieldInfo _spinnersField = null;
    private static FieldInfo _submitButtonField = null;
    private static FieldInfo _downButtonField = null;
    private static MethodInfo _getCurrentCharMethod = null;

    private IList _spinners = null;
    private MonoBehaviour _submitButton = null;
}
