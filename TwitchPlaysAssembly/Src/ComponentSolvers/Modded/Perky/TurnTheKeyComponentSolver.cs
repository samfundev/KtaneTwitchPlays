using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class TurnTheKeyComponentSolver : ComponentSolver
{
    public TurnTheKeyComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
        base(bombCommander, bombComponent)
	{
        _lock = (MonoBehaviour)_lockField.GetValue(BombComponent.GetComponent(_componentType));
        modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
	    bombCommander?.twitchBombHandle.StartCoroutine(ReWriteTurnTheKey());
    }

	private bool IsTargetTurnTimeCorrect(int turnTime)
	{
		return turnTime < 0 || turnTime == (int)_targetTimeField.GetValue(BombComponent.GetComponent(_componentType));
	}

	private bool CanTurnEarlyWithoutStrike(int turnTime)
	{
		if (!TwitchPlaySettings.data.AllowTurnTheKeyInstantSolveWhenOnlyModuleLeft) return false;
		int time = (int)_targetTimeField.GetValue(BombComponent.GetComponent(_componentType));
		int timeRemaining = (int)BombCommander.Bomb.GetTimer().TimeRemaining;
		if ((!OtherModes.zenModeOn && timeRemaining < time) || (OtherModes.zenModeOn && timeRemaining > time)) return false;
		IEnumerable<BombComponent> components = BombMessageResponder.Instance.ComponentHandles.Where(x => x.bombID == ComponentHandle.bombID && x.bombComponent.IsSolvable && !x.bombComponent.IsSolved && x.bombComponent != BombComponent).Select(x => x.bombComponent).ToArray();
		if (components.Any(x => x.GetComponent(_componentType) == null)) return false;
		if(!OtherModes.zenModeOn)
			return !components.Any(x => ((int) _targetTimeField.GetValue(x.GetComponent(_componentType)) > time)) && IsTargetTurnTimeCorrect(turnTime);
		else
			return !components.Any(x => ((int)_targetTimeField.GetValue(x.GetComponent(_componentType)) < time)) && IsTargetTurnTimeCorrect(turnTime);
	}

    private bool OnKeyTurn(int turnTime = -1)
    {
	    bool result = CanTurnEarlyWithoutStrike(turnTime);
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

		((KMSelectable)_lock).OnInteract = () => OnKeyTurn();
		int expectedTime = (int)_targetTimeField.GetValue(BombComponent.GetComponent(_componentType));
	    if (OtherModes.zenModeOn)
	    {
		    expectedTime = (int) (BombCommander.timerComponent.TimeRemaining + (BombCommander.timerComponent.TimeRemaining - expectedTime));
			_targetTimeField.SetValue(BombComponent.GetComponent(_componentType), expectedTime);
		    TextMesh display = (TextMesh)_displayField.GetValue(BombComponent.GetComponent(_componentType));
		    display.text = string.Format("{0:00}:{1:00}", expectedTime / 60, expectedTime % 60);
	    }
	    while (!BombComponent.IsSolved)
        {
            int time = Mathf.FloorToInt(BombCommander.CurrentTimer);
            if (((!OtherModes.zenModeOn && time < expectedTime) || (OtherModes.zenModeOn && time > expectedTime)) &&
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
        if (!int.TryParse(second, out int timeTarget))
        {
            string[] minsec = second.Split(':');
            if (minsec.Length != 2 || !int.TryParse(minsec[0], out int min) || !int.TryParse(minsec[1], out int sec)) yield break;
            timeTarget = min * 60 + sec;
        }

        yield return "release";

        TimerComponent timerComponent = BombCommander.Bomb.GetTimer();

        int waitingTime = (int) (timerComponent.TimeRemaining + (OtherModes.zenModeOn ? -0.25f : 0.25f));
        waitingTime -= timeTarget;

        if (Math.Abs(waitingTime) >= 30 && !CanTurnEarlyWithoutStrike(timeTarget))
        {
            yield return "elevator music";
        }

        float timeRemaining = float.PositiveInfinity;
        while (timeRemaining > 0.0f)
        {
            if (CoroutineCanceller.ShouldCancel)
            {
                CoroutineCanceller.ResetCancel();
                yield return "sendtochaterror The key turn was aborted due to a request to cancel";
                break;
            }

            timeRemaining = (int) (timerComponent.TimeRemaining + (OtherModes.zenModeOn ? -0.25f : 0.25f));

            if ((!OtherModes.zenModeOn && timeRemaining < timeTarget) || (OtherModes.zenModeOn && timeRemaining > timeTarget))
            {
                yield return "sendtochaterror The bomb timer has already gone past the time specified.";
                yield break;
            }
            if (timeRemaining == timeTarget || CanTurnEarlyWithoutStrike(timeTarget))
            {
                OnKeyTurn(timeTarget);
                yield return new WaitForSeconds(0.1f);
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
	    _displayField = _componentType.GetField("Display", BindingFlags.Public | BindingFlags.Instance);
		_stopAllCorotinesMethod = _componentType.GetMethod("StopAllCoroutines", BindingFlags.Public | BindingFlags.Instance);
        _onKeyTurnMethod = _componentType.GetMethod("OnKeyTurn", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private static Type _componentType = null;
    private static FieldInfo _lockField = null;
    private static FieldInfo _activatedField = null;
    private static FieldInfo _solvedField = null;
    private static FieldInfo _targetTimeField = null;
	public static FieldInfo _displayField = null;
    private static MethodInfo _stopAllCorotinesMethod = null;
    private static MethodInfo _onKeyTurnMethod = null;

    private MonoBehaviour _lock = null;
}
