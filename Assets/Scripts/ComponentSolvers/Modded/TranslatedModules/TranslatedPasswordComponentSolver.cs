using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class TranslatedPasswordComponentSolver : ComponentSolver
{
    public TranslatedPasswordComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _downButtons = (KMSelectable[]) _downButtonField.GetValue(bombComponent.GetComponent(_passwordComponentType));
        _submitButton = (MonoBehaviour)_submitButtonField.GetValue(bombComponent.GetComponent(_passwordComponentType));
        _display = (TextMesh[]) _displayField.GetValue(bombComponent.GetComponent(_passwordComponentType));
        modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        string[] commandParts = inputCommand.Split(' ');
        if (commandParts.Length > 2 || commandParts[0].Length != 5 || commandParts[0] == "claim")
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

            if (spinnerIndex >= 0 && spinnerIndex < _downButtons.Length)
            {
                IEnumerator spinnerCoroutine = CycleCharacterSpinnerCoroutine(spinnerIndex);
                while (spinnerCoroutine.MoveNext())
                {
                    yield return spinnerCoroutine.Current;
                }
            }
            yield break;
        }

        yield return "password";

        IEnumerator solveCoroutine = SolveCoroutine(inputCommand);
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

            IEnumerator subcoroutine = GetCharacterSpinnerToCharacterCoroutine(characterIndex, characters[characterIndex].ToString());
            while (subcoroutine.MoveNext())
            {
                yield return subcoroutine.Current;
            }

            //Break out of the sequence if a column spinner doesn't have a matching character
            if (!_display[characterIndex].text.Equals(characters[characterIndex].ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                yield break;
            }
        }

        yield return DoInteractionClick(_submitButton);
    }

    private IEnumerator GetCharacterSpinnerToCharacterCoroutine(int index, string desiredCharacter)
    {
        KMSelectable downButton = _downButtons[index];
        for (int hitCount = 0; hitCount < 6 && !_display[index].text.Equals(desiredCharacter, StringComparison.InvariantCultureIgnoreCase); ++hitCount)
        {
            yield return DoInteractionClick(downButton);
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

    private MonoBehaviour _submitButton = null;
    private KMSelectable[] _downButtons = null;
    private TextMesh[] _display = null;
}
