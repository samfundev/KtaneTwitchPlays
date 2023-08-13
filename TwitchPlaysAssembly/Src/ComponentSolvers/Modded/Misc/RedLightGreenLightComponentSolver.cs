using System.Collections;
using UnityEngine;

public class RedLightGreenLightComponentSolver : ReflectionComponentSolver
{
	public RedLightGreenLightComponentSolver(TwitchModule module) :
		base(module, "redLightGreenLight", "On Twitch Plays the Korean phrase will be sent to chat | Note that movement counts as any command that causes the bot to interact with a module")
	{
		TwitchPlaysService.Instance.OnInteractCommand += RLGLMovement;
		Module.OnDestroyed += () => TwitchPlaysService.Instance.OnInteractCommand -= RLGLMovement;
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		yield return null;
	}

	public void RLGLMovement()
	{
		if (_component.GetValue<bool>("active"))
		{
			if (!_component.GetValue<bool>("alreadyStruck"))
			{
				Debug.LogFormat("[Red Light Green Light #{0}] You moved!", _component.GetValue<int>("moduleId"));
				if (!_component.GetValue<object>("Settings").GetValue<bool>("HardMode"))
					_component.GetValue<KMBombModule>("module").HandleStrike();
				else
					Module.StartCoroutine(_component.CallMethod<IEnumerator>("Detonate"));
				_component.SetValue("alreadyStruck", true);
			}
		}
	}
}