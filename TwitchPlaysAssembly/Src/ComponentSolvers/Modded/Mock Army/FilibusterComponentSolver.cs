using System.Collections;
using UnityEngine;

public class FilibusterComponentSolver : ReflectionComponentSolver
{
	public FilibusterComponentSolver(TwitchModule module) :
		base(module, "FilibusterModule", "You know the drill, keep talking")
	{
		((MonoBehaviour) _component).enabled = false;
		Module.StartCoroutine(UpdateFilibuster());

		IRCConnection.Instance.OnMessageReceived += AddToMicLevel;
		Module.OnDestroyed += () => IRCConnection.Instance.OnMessageReceived -= AddToMicLevel;
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		yield return null;
	}

	private IEnumerator UpdateFilibuster()
	{
		while (true)
		{
			yield return null;
			float _timeElapsed = _component.GetValue<float>("_timeElapsed");
			float _warningSoundTimeElapsed = _component.GetValue<float>("_warningSoundTimeElapsed");
			float _failureTimeElapsed = _component.GetValue<float>("_failureTimeElapsed");
			_component.SetValue("_timeElapsed", _timeElapsed + Time.deltaTime);

			if (_micLevel > 100f)
				_micLevel = 100;

			if (_timeElapsed > 0.1f)
			{
				_component.CallMethod("UpdateDisplay", _micLevel);
				if (_micLevel > 0)
				{
					_micLevel -= .4f;
					if (_micLevel < 0)
						_micLevel = 0;
				}
				_component.SetValue("_timeElapsed", 0);
			}

			if (_warningSoundTimeElapsed > 0.5f)
			{
				_warningSoundTimeElapsed = 0;
				_component.CallMethod("UpdateWarningSound", _micLevel);
			}

			_component.CallMethod("UpdateBar", _micLevel);

			if (_component.GetValue<bool>("_isArmed"))
			{
				_component.SetValue("_warningSoundTimeElapsed", _warningSoundTimeElapsed + Time.deltaTime);
				_component.SetValue("_failureTimeElapsed", _failureTimeElapsed + Time.deltaTime);

				if (_failureTimeElapsed > 1.0f)
				{
					_component.CallMethod("CheckForFailure", _micLevel);
					_component.SetValue("_failureTimeElapsed", 0);
				}
			}
		}
	}

	public void AddToMicLevel(IRCMessage _)
	{
		_micLevel += 20;
	}

	private float _micLevel;
}