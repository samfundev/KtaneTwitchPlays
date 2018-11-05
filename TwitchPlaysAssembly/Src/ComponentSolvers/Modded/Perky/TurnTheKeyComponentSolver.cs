using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class TurnTheKeyComponentSolver : ComponentSolver
{
	public TurnTheKeyComponentSolver(TwitchModule module) :
		base(module)
	{
		_lock = (MonoBehaviour) LockField.GetValue(Module.GetComponent(ComponentType));
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Turn the key at specified time with !{0} turn 8:29");
		module.Bomb.StartCoroutine(ReWriteTurnTheKey());
		module.BombComponent.GetComponent<KMBombModule>().OnActivate = OnActivate;
		SkipTimeAllowed = true;
	}

	private bool IsTargetTurnTimeCorrect(int turnTime) => turnTime < 0 || turnTime == (int) TargetTimeField.GetValue(Module.GetComponent(ComponentType));

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		IEnumerator solve = DelayKeyTurn(true, false, true);
		while (solve.MoveNext()) yield return solve.Current;
	}

	private bool CanTurnEarlyWithoutStrike(int turnTime)
	{
		int time = (int) TargetTimeField.GetValue(Module.GetComponent(ComponentType));
		int timeRemaining = (int) Module.Bomb.CurrentTimer;
		if ((!OtherModes.ZenModeOn && timeRemaining < time) || (OtherModes.ZenModeOn && timeRemaining > time) || !IsTargetTurnTimeCorrect(turnTime)) return false;
		return TwitchGame.Instance.Modules.Where(x => x.BombID == Module.BombID && x.BombComponent.IsSolvable && !x.BombComponent.IsSolved).All(x => x.Solver.SkipTimeAllowed);
	}

	private bool OnKeyTurn(int turnTime = -1)
	{
		bool result = CanTurnEarlyWithoutStrike(turnTime);
		Module.Bomb.Bomb.StartCoroutine(DelayKeyTurn(!result));
		return false;
	}

	private IEnumerator DelayKeyTurn(bool restoreBombTimer, bool causeStrikeIfWrongTime = true, bool bypassSettings = false)
	{
		Animator keyAnimator = (Animator) KeyAnimatorField.GetValue(Module.GetComponent(ComponentType));
		KMAudio keyAudio = (KMAudio) KeyAudioField.GetValue(Module.GetComponent(ComponentType));
		int time = (int) TargetTimeField.GetValue(Module.GetComponent(ComponentType));

		if (!restoreBombTimer)
		{
			Module.Bomb.CurrentTimer = time + 0.5f + Time.deltaTime;
			yield return null;
		}
		else if (causeStrikeIfWrongTime && time != (int) Mathf.Floor(Module.Bomb.CurrentTimer))
		{
			Module.GetComponent<KMBombModule>().HandleStrike();
			keyAnimator.SetTrigger("WrongTurn");
			keyAudio.PlaySoundAtTransform("WrongKeyTurnFK", Module.transform);
			yield return null;
			if (!(TwitchPlaySettings.data.AllowTurnTheKeyEarlyLate || bypassSettings) || (bool) SolvedField.GetValue(Module.GetComponent(ComponentType)))
			{
				yield break;
			}
		}

		Module.GetComponent<KMBombModule>().HandlePass();
		KeyUnlockedField.SetValue(Module.GetComponent(ComponentType), true);
		SolvedField.SetValue(Module.GetComponent(ComponentType), true);
		keyAnimator.SetBool("IsUnlocked", true);
		keyAudio.PlaySoundAtTransform("TurnTheKeyFX", Module.transform);
		yield return null;
	}

	private void OnActivate()
	{
		string serial = Module.Bomb.QueryWidgets<string>(KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER).First()["serial"];
		TextMesh textMesh = (TextMesh) DisplayField.GetValue(Module.GetComponent(ComponentType));
		ActivatedField.SetValue(Module.GetComponent(ComponentType), true);

		if (string.IsNullOrEmpty(_previousSerialNumber) || !_previousSerialNumber.Equals(serial) || _keyTurnTimes.Count == 0)
		{
			if (!string.IsNullOrEmpty(_previousSerialNumber) && _previousSerialNumber.Equals(serial))
			{
				Animator keyAnimator = (Animator) KeyAnimatorField.GetValue(Module.GetComponent(ComponentType));
				KMAudio keyAudio = (KMAudio) KeyAudioField.GetValue(Module.GetComponent(ComponentType));
				AttemptedForcedSolve = true;
				PrepareSilentSolve();
				Module.GetComponent<KMBombModule>().HandlePass();
				KeyUnlockedField.SetValue(Module.GetComponent(ComponentType), true);
				SolvedField.SetValue(Module.GetComponent(ComponentType), true);
				keyAnimator.SetBool("IsUnlocked", true);
				keyAudio.PlaySoundAtTransform("TurnTheKeyFX", Module.transform);
				textMesh.text = "88:88";
				return;
			}

			_keyTurnTimes.Clear();
			for (int i = OtherModes.ZenModeOn ? 45 : 3; i < (OtherModes.ZenModeOn ? 3600 : Module.Bomb.CurrentTimer - 45); i += 3)
			{
				_keyTurnTimes.Add(i);
			}
			if (_keyTurnTimes.Count == 0) _keyTurnTimes.Add((int) (Module.Bomb.CurrentTimer / 2f));

			_keyTurnTimes = _keyTurnTimes.Shuffle().ToList();
			_previousSerialNumber = serial;
		}
		TargetTimeField.SetValue(Module.GetComponent(ComponentType), _keyTurnTimes[0]);

		string display = $"{_keyTurnTimes[0] / 60:00}:{_keyTurnTimes[0] % 60:00}";
		_keyTurnTimes.RemoveAt(0);

		textMesh.text = display;
	}

	private IEnumerator ReWriteTurnTheKey()
	{
		yield return new WaitUntil(() => (bool) ActivatedField.GetValue(Module.GetComponent(ComponentType)));
		yield return new WaitForSeconds(0.1f);
		StopAllCorotinesMethod.Invoke(Module.GetComponent(ComponentType), null);

		((KMSelectable) _lock).OnInteract = () => OnKeyTurn();
		int expectedTime = (int) TargetTimeField.GetValue(Module.GetComponent(ComponentType));
		if (Math.Abs(expectedTime - Module.Bomb.CurrentTimer) < 30)
		{
			yield return new WaitForSeconds(0.1f);
			AttemptedForcedSolve = true;
			HandleForcedSolve();
			yield break;
		}

		while (!Module.Solved)
		{
			int time = Mathf.FloorToInt(Module.Bomb.CurrentTimer);
			if ((!OtherModes.ZenModeOn && time < expectedTime || OtherModes.ZenModeOn && time > expectedTime) &&
				!(bool) SolvedField.GetValue(Module.GetComponent(ComponentType)) &&
				!TwitchPlaySettings.data.AllowTurnTheKeyEarlyLate)
			{
				Module.GetComponent<KMBombModule>().HandleStrike();
			}
			yield return new WaitForSeconds(2.0f);
		}
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] commands = inputCommand.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

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

		if (CanTurnEarlyWithoutStrike(timeTarget))
			yield return $"skiptime {timeTarget + 0.5f:0.00}";

		int waitingTime = (int) (Module.Bomb.CurrentTimer + (OtherModes.ZenModeOn ? -0.25f : 0.25f));
		waitingTime -= timeTarget;

		if (Math.Abs(waitingTime) >= 30)
		{
			yield return "elevator music";
		}

		float timeRemaining = float.PositiveInfinity;
		while (timeRemaining > 0.0f)
		{
			timeRemaining = (int) (Module.Bomb.CurrentTimer + (OtherModes.ZenModeOn ? -0.25f : 0.25f));

			if (!OtherModes.ZenModeOn && timeRemaining < timeTarget || OtherModes.ZenModeOn && timeRemaining > timeTarget)
			{
				yield return "sendtochaterror The bomb timer has already gone past the time specified.";
				yield break;
			}
			if ((int) timeRemaining == timeTarget)
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
		ComponentType = ReflectionHelper.FindType("TurnKeyModule");
		LockField = ComponentType.GetField("Lock", BindingFlags.Public | BindingFlags.Instance);
		ActivatedField = ComponentType.GetField("bActivated", BindingFlags.NonPublic | BindingFlags.Instance);
		SolvedField = ComponentType.GetField("bUnlocked", BindingFlags.NonPublic | BindingFlags.Instance);
		TargetTimeField = ComponentType.GetField("mTargetSecond", BindingFlags.NonPublic | BindingFlags.Instance);
		StopAllCorotinesMethod = ComponentType.GetMethod("StopAllCoroutines", BindingFlags.Public | BindingFlags.Instance);
		KeyAnimatorField = ComponentType.GetField("KeyAnimator", BindingFlags.Public | BindingFlags.Instance);
		DisplayField = ComponentType.GetField("Display", BindingFlags.Public | BindingFlags.Instance);
		KeyUnlockedField = ComponentType.GetField("bUnlocked", BindingFlags.NonPublic | BindingFlags.Instance);
		KeyAudioField = ComponentType.GetField("mAudio", BindingFlags.NonPublic | BindingFlags.Instance);
		_keyTurnTimes = new List<int>();
	}

	private static readonly Type ComponentType;
	private static readonly FieldInfo LockField;
	private static readonly FieldInfo ActivatedField;
	private static readonly FieldInfo SolvedField;
	private static readonly FieldInfo TargetTimeField;
	private static readonly FieldInfo KeyAnimatorField;
	private static readonly FieldInfo DisplayField;
	private static readonly FieldInfo KeyUnlockedField;
	private static readonly FieldInfo KeyAudioField;
	private static readonly MethodInfo StopAllCorotinesMethod;

	private static List<int> _keyTurnTimes;
	private static string _previousSerialNumber;

	private readonly MonoBehaviour _lock;
}
