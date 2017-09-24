using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class ButtonComponentSolver : ComponentSolver
{
    public ButtonComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _button = (MonoBehaviour)_buttonField.GetValue(bombComponent);

        helpMessage = "!{0} tap [tap the button] | !{0} hold [hold the button] | !{0} release 7 [release when the digit shows 7]";
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        if (!_held && (inputCommand.Equals("tap", StringComparison.InvariantCultureIgnoreCase) ||
                       inputCommand.Equals("click", StringComparison.InvariantCultureIgnoreCase)))
        {
            yield return "tap";

            DoInteractionStart (_button);
            yield return new WaitForSeconds(0.1f);
            DoInteractionEnd(_button);
        }
        else if (!_held && (inputCommand.Equals("hold", StringComparison.InvariantCultureIgnoreCase)))
        {
            yield return "hold";

            _held = true;
            DoInteractionStart(_button);
            yield return new WaitForSeconds(2.0f);
        }
        else if (_held)
        {
            string[] commandParts = inputCommand.Split(' ');
            if (commandParts.Length == 2 && commandParts[0].Equals("release", StringComparison.InvariantCultureIgnoreCase))
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
        while (timeRemaining > 0.0f)
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
                break;
            }

            yield return null;
        }
    }

    static ButtonComponentSolver()
    {
        _buttonComponentType = ReflectionHelper.FindType("ButtonComponent");
        _buttonField = _buttonComponentType.GetField("button", BindingFlags.Public | BindingFlags.Instance);
    }

    private static Type _buttonComponentType = null;
    private static FieldInfo _buttonField = null;

    private MonoBehaviour _button = null;
    private bool _held = false;

}
