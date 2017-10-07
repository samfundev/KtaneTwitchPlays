using System;
using System.Reflection;
using System.Collections;
using UnityEngine;

public class TranslatedButtonComponentSolver : ComponentSolver
{
	public TranslatedButtonComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
		base(bombCommander, bombComponent, ircConnection, canceller)
	{
	    _button = (KMSelectable) _buttonField.GetValue(bombComponent.GetComponent(_componentType));
	    modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
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
                int second = 0;
                if (!int.TryParse(commandParts[1], out second))
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

        MonoBehaviour timerComponent = (MonoBehaviour)CommonReflectedTypeInfo.GetTimerMethod.Invoke(BombCommander.Bomb, null);

        string secondString = second.ToString();

        float timeRemaining = float.PositiveInfinity;
        while (timeRemaining > 0.0f && _held)
        {
            if (Canceller.ShouldCancel)
            {
                Canceller.ResetCancel();
                yield break;
            }

            timeRemaining = (float)CommonReflectedTypeInfo.TimeRemainingField.GetValue(timerComponent);
            string formattedTime = (string)CommonReflectedTypeInfo.GetFormattedTimeMethod.Invoke(null, new object[] { timeRemaining, true });

            if (formattedTime.Contains(secondString))
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
	}

	private static Type _componentType = null;
	private static FieldInfo _buttonField = null;

    private KMSelectable _button = null;
    private bool _held = false;
}
