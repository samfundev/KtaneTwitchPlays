using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class TurnTheKeyComponentSolver : ComponentSolver
{
    public TurnTheKeyComponentSolver(BombCommander bombCommander, BombComponent bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _lock = (MonoBehaviour)_lockField.GetValue(BombComponent.GetComponent(_componentType));
        modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
	    bombCommander?.twitchBombHandle.StartCoroutine(ReWriteTurnTheKey());
    }


	private bool CanTurnEarlyWithoutStrike()
	{
		int time = (int)_targetTimeField.GetValue(BombComponent.GetComponent(_componentType));
		int timeRemaining = (int)BombCommander.Bomb.GetTimer().TimeRemaining;
		if (timeRemaining < time) return false;
		IEnumerable<BombComponent> components = BombMessageResponder.Instance.ComponentHandles.Where(x => x.bombID == ComponentHandle.bombID && x.bombComponent.IsSolvable && !x.bombComponent.IsSolved && x.bombComponent != BombComponent).Select(x => x.bombComponent).ToArray();
		if (components.Any(x => x.GetComponent(_componentType) == null)) return false;
		return !components.Any(x => ((int) _targetTimeField.GetValue(x.GetComponent(_componentType)) > time));
	}

    private bool OnKeyTurn()
    {
	    bool result = CanTurnEarlyWithoutStrike();
	    if (!result)
	    {
		    _onKeyTurnMethod.Invoke(BombComponent.GetComponent(_componentType), null);
		    if (!TwitchPlaySettings.data.AllowTurnTheKeyEarlyLate || (bool) _solvedField.GetValue(BombComponent.GetComponent(_componentType))) return false;
	    }
	    BombCommander.twitchBombHandle.StartCoroutine(DelayKeyTurn(!result));
	    return false;
    }

	private IEnumerator DelayKeyTurn(bool restoreBombTimer)
	{
		
		int time = (int)_targetTimeField.GetValue(BombComponent.GetComponent(_componentType));
		float currentBombTime = BombCommander.CurrentTimer;
		BombCommander.timerComponent.TimeRemaining = time + 0.5f + Time.deltaTime;
		yield return null;
		_onKeyTurnMethod.Invoke(BombComponent.GetComponent(_componentType), null);
		if (restoreBombTimer)
			BombCommander.timerComponent.TimeRemaining = currentBombTime;
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
            if (!int.TryParse(value, out int time))
            {
                int pos = value.IndexOf(':');
                if (pos == -1) continue;
                if (!int.TryParse(value.Substring(0, pos), out int min)) continue;
                if (!int.TryParse(value.Substring(pos + 1), out int sec)) continue;
                time = min * 60 + sec;
            }
            sortedTimes.Add(time);
        }
        sortedTimes.Sort();
        sortedTimes.Reverse();
        if (sortedTimes.Count == 0) yield break;

        yield return "release";

        TimerComponent timerComponent = BombCommander.Bomb.GetTimer();

        int timeTarget = sortedTimes[0];
        sortedTimes.RemoveAt(0);
        int waitingTime = (int)(timerComponent.TimeRemaining + 0.25f);
        waitingTime -= timeTarget;

        if (waitingTime >= 30 && !CanTurnEarlyWithoutStrike())
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

            timeRemaining = (int)(timerComponent.TimeRemaining + 0.25f);

            if (timeRemaining < timeTarget)
            {
                if (sortedTimes.Count == 0) yield break;
                timeTarget = sortedTimes[0];
                sortedTimes.RemoveAt(0);
                continue;
            }
            if (timeRemaining == timeTarget || CanTurnEarlyWithoutStrike())
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
