using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class TurnTheKeyComponentSolver : ComponentSolver
{
    public TurnTheKeyComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _lock = (MonoBehaviour)_lockField.GetValue(bombComponent.GetComponent(_componentType));
        helpMessage = "Turn the key at specified time with !{0} turn 8:29";
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {

        var commands = inputCommand.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (commands.Length != 2 || !commands[0].Equals("turn", StringComparison.InvariantCultureIgnoreCase))
            yield break;

        yield return "Turning the key";

        yield return ReleaseCoroutine(commands[1]);

    }

    private IEnumerator ReleaseCoroutine(string second)
    {
        string[] list = second.Split(' ');
        List<int> sortedTimes = new List<int>();
        foreach (string value in list)
        {
            int time = -1;
            if (!int.TryParse(value, out time))
            {
                int pos = value.IndexOf(':');
                if (pos == -1) continue;
                int min, sec;
                if (!int.TryParse(value.Substring(0, pos), out min)) continue;
                if (!int.TryParse(value.Substring(pos + 1), out sec)) continue;
                time = min * 60 + sec;
            }
            sortedTimes.Add(time);
        }
        sortedTimes.Sort();
        sortedTimes.Reverse();
        if (sortedTimes.Count == 0) yield break;

        yield return "release";

        MonoBehaviour timerComponent = (MonoBehaviour)CommonReflectedTypeInfo.GetTimerMethod.Invoke(BombCommander.Bomb, null);

        int timeTarget = sortedTimes[0];
        sortedTimes.RemoveAt(0);
        int waitingTime = (int)((float)CommonReflectedTypeInfo.TimeRemainingField.GetValue(timerComponent) + 0.25f);
        waitingTime -= timeTarget;

        if (waitingTime >= 30)
        {
            _musicPlayer = MusicPlayer.StartRandomMusic();
        }

        float timeRemaining = float.PositiveInfinity;
        while (timeRemaining > 0.0f)
        {
            if (Canceller.ShouldCancel)
            {
                Canceller.ResetCancel();
                break;
            }

            timeRemaining = (int)((float)CommonReflectedTypeInfo.TimeRemainingField.GetValue(timerComponent) + 0.25f);

            if (timeRemaining < timeTarget)
            {
                if (sortedTimes.Count == 0) yield break;
                timeTarget = sortedTimes[0];
                sortedTimes.RemoveAt(0);
                continue;
            }
            if (timeRemaining == timeTarget)
            {
                DoInteractionStart(_lock);
                yield return new WaitForSeconds(0.1f);
                DoInteractionEnd(_lock);
                break;
            }

            yield return null;
        }

        if (waitingTime >= 30)
        {
            _musicPlayer.StopMusic();
        }

    }

    static TurnTheKeyComponentSolver()
    {
        _componentType = ReflectionHelper.FindType("TurnKeyModule");
        _lockField = _componentType.GetField("Lock", BindingFlags.Public | BindingFlags.Instance);
    }

    private static Type _componentType = null;
    private static FieldInfo _lockField = null;

    private MonoBehaviour _lock = null;
    private MusicPlayer _musicPlayer = null;
}
