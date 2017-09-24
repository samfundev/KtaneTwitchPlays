using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class SquareButtonComponentSolver : ComponentSolver
{
    public SquareButtonComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _button = (MonoBehaviour)_buttonField.GetValue(bombComponent.GetComponent(_componentType));

        helpMessage = "Click the button with !{0} tap. Click the button at time with !{0} tap 8:55 8:44 8:33. Hold the button with !{0} hold. Release the button with !{0} release 9:58 9:49 9:30.";
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
        if (!_held && (inputCommand.StartsWith("tap ", StringComparison.InvariantCultureIgnoreCase) ||
                       inputCommand.StartsWith("click ", StringComparison.InvariantCultureIgnoreCase)))
        {
            yield return "tap2";

            IEnumerator releaseCoroutine = ReleaseCoroutine(inputCommand.Substring(inputCommand.IndexOf(' ')));
            while (releaseCoroutine.MoveNext())
            {
                yield return releaseCoroutine.Current;
            }
        }
        else if (!_held && (inputCommand.Equals("hold", StringComparison.InvariantCultureIgnoreCase) ||
                            inputCommand.Equals("press", StringComparison.InvariantCultureIgnoreCase)))
        {
            yield return "hold";

            _held = true;
            DoInteractionStart(_button);
            yield return new WaitForSeconds(2.0f);
        }
        else if (_held && inputCommand.StartsWith("release ", StringComparison.InvariantCultureIgnoreCase))
        {
            IEnumerator releaseCoroutine = ReleaseCoroutine(inputCommand.Substring(inputCommand.IndexOf(' ')));
            while (releaseCoroutine.MoveNext())
            {
                yield return releaseCoroutine.Current;
            }
        }
    }

    private IEnumerator ReleaseCoroutine(string second)
    {
        string[] list = second.Split(' ');
        List<int> sortedTimes = new List<int>();
        foreach(string value in list)
        {
            int time = -1;
            if(!int.TryParse(value, out time))
            {
                int pos = value.LastIndexOf(':');
                if(pos == -1) continue;
                int hour = 0;
                int min, sec;
                if(!int.TryParse(value.Substring(0, pos), out min))
                {
                    int pos2 = value.IndexOf(":");
                    if ( (pos2 == -1) || (pos == pos2) ) continue;
                    if (!int.TryParse(value.Substring(0, pos2), out hour)) continue;
                    if (!int.TryParse(value.Substring(pos2+1, pos-pos2-1), out min)) continue;
                }
                if(!int.TryParse(value.Substring(pos+1), out sec)) continue;
                time = (hour * 3600) + (min * 60) + sec;
            }
            sortedTimes.Add(time);
        }
        sortedTimes.Sort();
        sortedTimes.Reverse();
        if(sortedTimes.Count == 0) yield break;

        yield return "release";

        MonoBehaviour timerComponent = (MonoBehaviour)CommonReflectedTypeInfo.GetTimerMethod.Invoke(BombCommander.Bomb, null);

        int timeTarget = sortedTimes[0];
        sortedTimes.RemoveAt(0);

        int waitTime = (int)((float)CommonReflectedTypeInfo.TimeRemainingField.GetValue(timerComponent) + 0.25f);
        waitTime -= timeTarget;
        if (waitTime >= 30)
        {
            _musicPlayer = MusicPlayer.StartRandomMusic();
        }

        float timeRemaining = float.PositiveInfinity;
        while (timeRemaining > 0.0f)
        {
            if (Canceller.ShouldCancel)
            {
                if (waitTime >= 30)
                    _musicPlayer.StopMusic();

                Canceller.ResetCancel();
                yield break;
            }

            timeRemaining = (int)((float)CommonReflectedTypeInfo.TimeRemainingField.GetValue(timerComponent) + 0.25f);

            if (timeRemaining < timeTarget)
            {
                if (waitTime >= 30)
                    _musicPlayer.StopMusic();

                if(sortedTimes.Count == 0) yield break;
                timeTarget = sortedTimes[0];
                sortedTimes.RemoveAt(0);

                waitTime = (int)timeRemaining;
                waitTime -= timeTarget;
                if (waitTime >= 30)
                    _musicPlayer = MusicPlayer.StartRandomMusic();

                continue;
            }
            if (timeRemaining == timeTarget)
            {
                if (!_held)
                {
                    DoInteractionStart(_button);
                    yield return new WaitForSeconds(0.1f);
                }
                DoInteractionEnd(_button);
                _held = false;
                if (waitTime >= 30)
                    _musicPlayer.StopMusic();
                break;
            }

            yield return null;
        }
    }

    static SquareButtonComponentSolver()
    {
        _componentType = ReflectionHelper.FindType("AdvancedButton");
        _buttonField = _componentType.GetField("Button", BindingFlags.Public | BindingFlags.Instance);
    }

    private static Type _componentType = null;
    private static FieldInfo _buttonField = null;

    private MonoBehaviour _button = null;
    private bool _held = false;
    private MusicPlayer _musicPlayer = null;
}
