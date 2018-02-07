using System;
using System.Reflection;
using System.Collections;
using UnityEngine;

public class TranslatedButtonComponentSolver : ComponentSolver
{
	public TranslatedButtonComponentSolver(BombCommander bombCommander, BombComponent bombComponent, CoroutineCanceller canceller) :
		base(bombCommander, bombComponent, canceller)
	{
	    _button = (KMSelectable) _buttonField.GetValue(bombComponent.GetComponent(_componentType));
	    modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
	    Selectable selectable = bombComponent.GetComponent<Selectable>();
	    selectable.OnCancel += () => { _selectedField.SetValue(bombComponent.GetComponent(_componentType), false); return true; };

		if (bombCommander != null)
		{
			string language = TranslatedModuleHelper.GetManualCodeAddOn(bombComponent);
			if (language != null) modInfo.manualCode = $"The%20Button{language}";
			modInfo.moduleDisplayName = $"Big Button Translated{TranslatedModuleHelper.GetModuleDisplayNameAddon(bombComponent)}";
			bombComponent.StartCoroutine(SetHeaderText());
		}
	}

	private IEnumerator SetHeaderText()
	{
		yield return new WaitUntil(() => ComponentHandle != null);
		ComponentHandle.headerText.text = modInfo.moduleDisplayName;
	}

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        inputCommand = inputCommand.ToLowerInvariant();
        if (!_held && inputCommand.EqualsAny("tap", "click"))
        {
            yield return "tap";
            yield return DoInteractionClick(_button);
        }
        else if (!_held && inputCommand.Equals("hold"))
        {
            yield return "hold";

            _held = true;
            DoInteractionStart(_button);
            yield return new WaitForSeconds(2.0f);
        }
        else if (_held)
        {
            string[] commandParts = inputCommand.Split(' ');
            if (commandParts.Length == 2 && commandParts[0].Equals("release"))
            {
                if (!int.TryParse(commandParts[1], out int second))
                {
                    yield break;
                }

                if (second >= 0 && second <= 9)
                {
                    IEnumerator releaseCoroutine = ReleaseCoroutine(second);
                    while (releaseCoroutine.MoveNext())
                    {
                        yield return releaseCoroutine.Current;
                    }
                }
            }
        }

    }

    private IEnumerator ReleaseCoroutine(int second)
    {
        yield return "release";

        TimerComponent timerComponent = BombCommander.Bomb.GetTimer();

        string secondString = second.ToString();

        float timeRemaining = float.PositiveInfinity;
        while (timeRemaining > 0.0f && _held)
        {
            if (Canceller.ShouldCancel)
            {
                Canceller.ResetCancel();
                yield break;
            }

            timeRemaining = timerComponent.TimeRemaining;

            if (BombCommander.CurrentTimerFormatted.Contains(secondString))
            {
                DoInteractionEnd(_button);
                _held = false;
            }

            yield return null;
        }
    }

    static TranslatedButtonComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("BigButtonTranslatedModule");
		_buttonField = _componentType.GetField("Button", BindingFlags.Public | BindingFlags.Instance);
	    _selectedField = _componentType.GetField("isSelected", BindingFlags.NonPublic | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static FieldInfo _buttonField = null;
    private static FieldInfo _selectedField = null;

    private KMSelectable _button = null;
    private bool _held = false;
}
