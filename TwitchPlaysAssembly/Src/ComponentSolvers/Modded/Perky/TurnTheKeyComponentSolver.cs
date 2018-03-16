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
		bombComponent.GetComponent<KMBombModule>().OnActivate = OnActivate;
	}

	private bool IsTargetTurnTimeCorrect(int turnTime)
	{
		return turnTime < 0 || turnTime == (int)_targetTimeField.GetValue(BombComponent.GetComponent(_componentType));
	}

	protected override bool HandleForcedSolve()
	{
		base.HandleForcedSolve();
		CoroutineQueue.AddForcedSolve(DelayKeyTurn(true, false, true));
		return true;
	}

	private bool CanTurnEarlyWithoutStrike(int turnTime)
	{
		if (!TwitchPlaySettings.data.AllowTurnTheKeyInstantSolveWhenOnlyModuleLeft) return false;
		int time = (int)_targetTimeField.GetValue(BombComponent.GetComponent(_componentType));
		int timeRemaining = (int)BombCommander.Bomb.GetTimer().TimeRemaining;
		if ((!OtherModes.ZenModeOn && timeRemaining < time) || (OtherModes.ZenModeOn && timeRemaining > time)) return false;
		IEnumerable<BombComponent> components = BombMessageResponder.Instance.ComponentHandles.Where(x => x.bombID == ComponentHandle.bombID && x.bombComponent.IsSolvable && !x.bombComponent.IsSolved && x.bombComponent != BombComponent).Select(x => x.bombComponent).ToArray();
		if (components.Any(x => x.GetComponent(_componentType) == null)) return false;
		if(!OtherModes.ZenModeOn)
			return !components.Any(x => ((int) _targetTimeField.GetValue(x.GetComponent(_componentType)) > time)) && IsTargetTurnTimeCorrect(turnTime);
		else
			return !components.Any(x => ((int)_targetTimeField.GetValue(x.GetComponent(_componentType)) < time)) && IsTargetTurnTimeCorrect(turnTime);
	}

    private bool OnKeyTurn(int turnTime = -1)
    {
	    bool result = CanTurnEarlyWithoutStrike(turnTime);
	    BombCommander.twitchBombHandle.StartCoroutine(DelayKeyTurn(!result));
	    return false;
    }

	private IEnumerator DelayKeyTurn(bool restoreBombTimer, bool causeStrikeIfWrongTime = true, bool bypassSettings = false)
	{
		Animator keyAnimator = (Animator) _keyAnimatorField.GetValue(BombComponent.GetComponent(_componentType));
		KMAudio keyAudio = (KMAudio) _keyAudioField.GetValue(BombComponent.GetComponent(_componentType));
		int time = (int) _targetTimeField.GetValue(BombComponent.GetComponent(_componentType));

		if (!restoreBombTimer)
		{
			BombCommander.timerComponent.TimeRemaining = time + 0.5f + Time.deltaTime;
			yield return null;
		}
		else if (causeStrikeIfWrongTime && time != (int) Mathf.Floor(BombCommander.timerComponent.TimeRemaining))
		{
			BombComponent.GetComponent<KMBombModule>().HandleStrike();
			keyAnimator.SetTrigger("WrongTurn");
			keyAudio.PlaySoundAtTransform("WrongKeyTurnFK", BombComponent.transform);
			yield return null;
			if (!(TwitchPlaySettings.data.AllowTurnTheKeyEarlyLate || bypassSettings) || (bool) _solvedField.GetValue(BombComponent.GetComponent(_componentType)))
			{
				yield break;
			}
		}

		BombComponent.GetComponent<KMBombModule>().HandlePass();
		_keyUnlockedField.SetValue(BombComponent.GetComponent(_componentType), true);
		_solvedField.SetValue(BombComponent.GetComponent(_componentType), true);
		keyAnimator.SetBool("IsUnlocked", true);
		keyAudio.PlaySoundAtTransform("TurnTheKeyFX", BombComponent.transform);
		yield return null;
	}

	private void OnActivate()
	{
		string serial = BombCommander.QueryWidgets<string>(KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER).First()["serial"];
		TextMesh textMesh = (TextMesh)_displayField.GetValue(BombComponent.GetComponent(_componentType));
		_activatedField.SetValue(BombComponent.GetComponent(_componentType), true);

		if (string.IsNullOrEmpty(_previousSerialNumber) || !_previousSerialNumber.Equals(serial) || _keyTurnTimes.Count == 0)
		{
			if (!string.IsNullOrEmpty(_previousSerialNumber) && _previousSerialNumber.Equals(serial))
			{
				Animator keyAnimator = (Animator)_keyAnimatorField.GetValue(BombComponent.GetComponent(_componentType));
				KMAudio keyAudio = (KMAudio)_keyAudioField.GetValue(BombComponent.GetComponent(_componentType));
				BombComponent.GetComponent<KMBombModule>().HandlePass();
				_keyUnlockedField.SetValue(BombComponent.GetComponent(_componentType), true);
				_solvedField.SetValue(BombComponent.GetComponent(_componentType), true);
				keyAnimator.SetBool("IsUnlocked", true);
				keyAudio.PlaySoundAtTransform("TurnTheKeyFX", BombComponent.transform);
				textMesh.text = "88:88";
				return;
			}

			_keyTurnTimes.Clear();
			for (int i = (OtherModes.ZenModeOn ? 45 : 3); i < (OtherModes.ZenModeOn ? 3600 : (BombCommander.CurrentTimer - 45)); i += 3)
			{
				_keyTurnTimes.Add(i);
			}
			if (_keyTurnTimes.Count == 0) _keyTurnTimes.Add((int)(BombCommander.CurrentTimer / 2f));

			_keyTurnTimes = _keyTurnTimes.Shuffle().ToList();
			_previousSerialNumber = serial;
		}
		_targetTimeField.SetValue(BombComponent.GetComponent(_componentType), _keyTurnTimes[0]);

		string display = $"{_keyTurnTimes[0] / 60:00}:{_keyTurnTimes[0] % 60:00}";
		_keyTurnTimes.RemoveAt(0);
		
		textMesh.text = display;
	}

	private IEnumerator ReWriteTurnTheKey()
    {
        yield return new WaitUntil(() => (bool) _activatedField.GetValue(BombComponent.GetComponent(_componentType)));
        yield return new WaitForSeconds(0.1f);
        _stopAllCorotinesMethod.Invoke(BombComponent.GetComponent(_componentType), null);

		((KMSelectable)_lock).OnInteract = () => OnKeyTurn();
		int expectedTime = (int)_targetTimeField.GetValue(BombComponent.GetComponent(_componentType));
	    if (Math.Abs(expectedTime - BombCommander.CurrentTimer) < 30)
	    {
		    yield return new WaitForSeconds(0.1f);
		    AttemptedForcedSolve = true;
		    HandleForcedSolve();
		    yield break;
	    }
	    
		while (!BombComponent.IsSolved)
        {
            int time = Mathf.FloorToInt(BombCommander.CurrentTimer);
            if (((!OtherModes.ZenModeOn && time < expectedTime) || (OtherModes.ZenModeOn && time > expectedTime)) &&
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

        int waitingTime = (int) (timerComponent.TimeRemaining + (OtherModes.ZenModeOn ? -0.25f : 0.25f));
        waitingTime -= timeTarget;

        if (Math.Abs(waitingTime) >= 30 && !CanTurnEarlyWithoutStrike(timeTarget))
        {
            yield return "elevator music";
        }

        float timeRemaining = float.PositiveInfinity;
        while (timeRemaining > 0.0f)
        {
            timeRemaining = (int) (timerComponent.TimeRemaining + (OtherModes.ZenModeOn ? -0.25f : 0.25f));

            if ((!OtherModes.ZenModeOn && timeRemaining < timeTarget) || (OtherModes.ZenModeOn && timeRemaining > timeTarget))
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

			yield return "trycancel The key turn was aborted due to a request to cancel";
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
	    _keyAnimatorField = _componentType.GetField("KeyAnimator", BindingFlags.Public | BindingFlags.Instance);
	    _displayField = _componentType.GetField("Display", BindingFlags.Public | BindingFlags.Instance);
	    _keyUnlockedField = _componentType.GetField("bUnlocked", BindingFlags.NonPublic | BindingFlags.Instance);
	    _keyAudioField = _componentType.GetField("mAudio", BindingFlags.NonPublic | BindingFlags.Instance);
	    _keyTurnTimes = new List<int>();
    }

    private static Type _componentType = null;
    private static FieldInfo _lockField = null;
    private static FieldInfo _activatedField = null;
    private static FieldInfo _solvedField = null;
    private static FieldInfo _targetTimeField = null;
	private static FieldInfo _keyAnimatorField = null;
	private static FieldInfo _displayField = null;
	private static FieldInfo _keyUnlockedField = null;
	private static FieldInfo _keyAudioField = null;
	private static MethodInfo _stopAllCorotinesMethod = null;

	private static List<int> _keyTurnTimes = null;
	private static string _previousSerialNumber = null;

    private MonoBehaviour _lock = null;
}
