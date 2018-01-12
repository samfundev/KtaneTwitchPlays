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
        _lock = (MonoBehaviour)_lockField.GetValue(BombComponent.GetComponent(_componentType));
        modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
        BombCommander.twitchBombHandle.StartCoroutine(ReWriteTurnTheKey());
    }

    private bool OnKeyTurn()
    {
        _onKeyTurnMethod.Invoke(BombComponent.GetComponent(_componentType), null);
        if (TwitchPlaySettings.data.AllowTurnTheKeyEarlyLate && !(bool)_solvedField.GetValue(BombComponent.GetComponent(_componentType)))
        {
            int time = (int)_targetTimeField.GetValue(BombComponent.GetComponent(_componentType));
            float currentBombTime = BombCommander.CurrentTimer;
            CommonReflectedTypeInfo.TimeRemainingField.SetValue(BombCommander.timerComponent, time + 0.5f);
            _onKeyTurnMethod.Invoke(BombComponent.GetComponent(_componentType), null);
            CommonReflectedTypeInfo.TimeRemainingField.SetValue(BombCommander.timerComponent, currentBombTime);
        }
        return false;
    }

    private IEnumerator ReWriteTurnTheKey()
    {
        yield return new WaitUntil(() => (bool) _activatedField.GetValue(BombComponent.GetComponent(_componentType)));
        yield return new WaitForSeconds(0.1f);
        _stopAllCorotinesMethod.Invoke(BombComponent.GetComponent(_componentType), null);

        ((KMSelectable)_lock).OnInteract = OnKeyTurn;
        int expectedTime = (int)_targetTimeField.GetValue(BombComponent.GetComponent(_componentType));
        while (true)
        {
            int time = Mathf.FloorToInt(BombCommander.CurrentTimer);
            if (time < expectedTime &&
                !(bool)_solvedField.GetValue(BombComponent.GetComponent(_componentType)) &&
                !TwitchPlaySettings.data.AllowTurnTheKeyEarlyLate)
            {
                BombComponent.GetComponent<KMBombModule>().HandleStrike();
            }
            yield return new WaitForSeconds(2.0f);
        }
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        var commands = inputCommand.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (commands.Length != 2 || !commands[0].Equals("turn", StringComparison.InvariantCultureIgnoreCase))
            yield break;

        IEnumerator turn = ReleaseCoroutine(commands[1]);
        while (turn.MoveNext())
            yield return turn.Current;
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
            yield return "elevator music";
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
                yield return DoInteractionClick(_lock);
                break;
            }

            yield return null;
        }
    }

    static TurnTheKeyComponentSolver()
    {
        _componentType = ReflectionHelper.FindType("TurnKeyModule");
        _lockField = _componentType.GetField("Lock", BindingFlags.Public | BindingFlags.Instance);
        _activatedField = _componentType.GetField("bActivated", BindingFlags.NonPublic | BindingFlags.Instance);
        _solvedField = _componentType.GetField("bUnlocked", BindingFlags.NonPublic | BindingFlags.Instance);
        _targetTimeField = _componentType.GetField("mTargetSecond", BindingFlags.NonPublic | BindingFlags.Instance);
        _stopAllCorotinesMethod = _componentType.GetMethod("StopAllCoroutines", BindingFlags.Public | BindingFlags.Instance);
        _onKeyTurnMethod = _componentType.GetMethod("OnKeyTurn", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private static Type _componentType = null;
    private static FieldInfo _lockField = null;
    private static FieldInfo _activatedField = null;
    private static FieldInfo _solvedField = null;
    private static FieldInfo _targetTimeField = null;
    private static MethodInfo _stopAllCorotinesMethod = null;
    private static MethodInfo _onKeyTurnMethod = null;

    private MonoBehaviour _lock = null;
}
