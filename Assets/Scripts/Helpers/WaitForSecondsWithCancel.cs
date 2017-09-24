using UnityEngine;

public class CoroutineCanceller
{
    public void SetCancel()
    {
        ShouldCancel = true;
    }

    public void ResetCancel()
    {
        ShouldCancel = false;
    }

    public bool ShouldCancel
    {
        get;
        private set;
    }
}

public class WaitForSecondsWithCancel : CustomYieldInstruction
{
    public WaitForSecondsWithCancel(float seconds, CoroutineCanceller canceller)
    {
        _seconds = seconds;
        _canceller = canceller;
        _startingTime = Time.time;
    }

    public override bool keepWaiting
    {
        get
        {
            if (_canceller.ShouldCancel)
            {
                _canceller.ResetCancel();
                return false;
            }

            return (Time.time - _startingTime) < _seconds;
        }
    }

    private readonly float _seconds = 0.0f;
    private readonly CoroutineCanceller _canceller = null;
    private readonly float _startingTime = 0.0f;
}

