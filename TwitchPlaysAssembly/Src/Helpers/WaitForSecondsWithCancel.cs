using UnityEngine;

public static class CoroutineCanceller
{
	public static void SetCancel()
	{
		ShouldCancel = true;
	}

	public static void ResetCancel()
	{
		ShouldCancel = false;
	}

	public static bool ShouldCancel
	{
		get;
		private set;
	}
}

public class WaitForSecondsWithCancel : CustomYieldInstruction
{
	public WaitForSecondsWithCancel(float seconds, bool resetCancel=true, ComponentSolver solver=null)
	{
		_seconds = seconds;
		_startingTime = Time.time;
		_resetCancel = resetCancel;
		_solver = solver;
		_startingStrikes = _solver?.StrikeCount ?? 0;
	}

	public override bool keepWaiting
	{
		get
		{
			if (!CoroutineCanceller.ShouldCancel && !(_solver?.Solved ?? false) && (_solver?.StrikeCount ?? 0) == _startingStrikes)
				return (Time.time - _startingTime) < _seconds;

			if(CoroutineCanceller.ShouldCancel && _resetCancel)
				CoroutineCanceller.ResetCancel();

			return false;
		}
	}

	private readonly float _seconds = 0.0f;
	private readonly float _startingTime = 0.0f;
	private readonly bool _resetCancel = true;
	private readonly int _startingStrikes = 0;
	private readonly ComponentSolver _solver = null;
}
