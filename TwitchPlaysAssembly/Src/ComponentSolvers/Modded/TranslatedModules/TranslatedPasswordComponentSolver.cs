using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class TranslatedPasswordComponentSolver : ComponentSolver
{
    public TranslatedPasswordComponentSolver(BombCommander bombCommander, BombComponent bombComponent, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, canceller)
	{
        _downButtons = (KMSelectable[]) _downButtonField.GetValue(bombComponent.GetComponent(_passwordComponentType));
        _submitButton = (MonoBehaviour)_submitButtonField.GetValue(bombComponent.GetComponent(_passwordComponentType));
        _display = (TextMesh[]) _displayField.GetValue(bombComponent.GetComponent(_passwordComponentType));
        modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		
		if (bombCommander != null)
		{
			string language = TranslatedModuleHelper.GetManualCodeAddOn(bombComponent);
			if (language != null) modInfo.manualCode = $"Passwords{language}";
			modInfo.moduleDisplayName = $"Passwords Translated{TranslatedModuleHelper.GetModuleDisplayNameAddon(bombComponent)}";
			bombComponent.StartCoroutine(SetHeaderText());
		}
	}

	private IEnumerator SetHeaderText()
	{
		yield return new WaitUntil(() => ComponentHandle != null);
		ComponentHandle.HeaderText = modInfo.moduleDisplayName;
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
	    HashSet<int> alreadyCycled = new HashSet<int>();
		string[] commandParts = inputCommand.Split(' ');
        if (commandParts[0].Length != 5)
        {
            yield break;
        }

        if (commandParts[0].Equals("cycle", StringComparison.InvariantCultureIgnoreCase))
        {
	        foreach (string cycle in commandParts.Skip(1))
	        {
		        if (!int.TryParse(cycle, out int spinnerIndex) || !alreadyCycled.Add(spinnerIndex) || spinnerIndex < 1 || spinnerIndex > _downButtons.Length)
			        continue;

				IEnumerator spinnerCoroutine = CycleCharacterSpinnerCoroutine(spinnerIndex-1);
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
                yield return "unsubmittablepenalty";
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
