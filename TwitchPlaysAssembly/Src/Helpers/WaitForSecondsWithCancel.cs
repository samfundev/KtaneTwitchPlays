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
    public WaitForSecondsWithCancel(float seconds, bool resetCancel=true)
    {
        _seconds = seconds;
        _startingTime = Time.time;
        _resetCancel = resetCancel;
    }

    public override bool keepWaiting
    {
        get
        {
	        if (!CoroutineCanceller.ShouldCancel)
				return (Time.time - _startingTime) < _seconds;

	        if(_resetCancel)
		        CoroutineCanceller.ResetCancel();

	        return false;
        }
    }

    private readonly float _seconds = 0.0f;
    private readonly float _startingTime = 0.0f;
    private readonly bool _resetCancel = true;
}

