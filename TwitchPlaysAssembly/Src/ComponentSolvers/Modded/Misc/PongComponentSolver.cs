using System.Collections;
using UnityEngine;

[ModuleID("NeedyPong")]
public class PongComponentSolver : ReflectionComponentSolver
{
	public PongComponentSolver(TwitchModule module) :
		base(module, "PongBackend", "NeedyPong143", "!{0} left/l/right/r <top/t/middle/m/bottom/b> [Sets the left or right paddle to the specified position on the screen] | On Twitch Plays the ball travels at half its normal speed")
	{
		Module.StartCoroutine(TPSpeedModifier());
	}


	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !split[0].EqualsAny("left", "l", "right", "r")) yield break;
		if (!split[1].EqualsAny("top", "t", "middle", "m", "bottom", "b")) yield break;

		yield return null;
		if (split[0].FirstOrWhole("left") && split[1].FirstOrWhole("top"))
		{
			DoInteractionStart(Selectables[0]);
			while (_component.GetValue<float>("Paddle1Y") < 0.875f)
				yield return null;
			DoInteractionEnd(Selectables[0]);
		}
		else if (split[0].FirstOrWhole("left") && split[1].FirstOrWhole("bottom"))
		{
			DoInteractionStart(Selectables[1]);
			while (_component.GetValue<float>("Paddle1Y") > 0.125f)
				yield return null;
			DoInteractionEnd(Selectables[1]);
		}
		else if (split[0].FirstOrWhole("left") && split[1].FirstOrWhole("middle"))
		{
			if (_component.GetValue<float>("Paddle1Y") < 0.5f)
			{
				DoInteractionStart(Selectables[0]);
				while (_component.GetValue<float>("Paddle1Y") < 0.5f)
					yield return null;
				DoInteractionEnd(Selectables[0]);
			}
			else
			{
				DoInteractionStart(Selectables[1]);
				while (_component.GetValue<float>("Paddle1Y") > 0.5f)
					yield return null;
				DoInteractionEnd(Selectables[1]);
			}
		}
		else if (split[0].FirstOrWhole("right") && split[1].FirstOrWhole("top"))
		{
			DoInteractionStart(Selectables[2]);
			while (_component.GetValue<float>("Paddle2Y") < 0.875f)
				yield return null;
			DoInteractionEnd(Selectables[2]);
		}
		else if (split[0].FirstOrWhole("right") && split[1].FirstOrWhole("bottom"))
		{
			DoInteractionStart(Selectables[3]);
			while (_component.GetValue<float>("Paddle2Y") > 0.125f)
				yield return null;
			DoInteractionEnd(Selectables[3]);
		}
		else
		{
			if (_component.GetValue<float>("Paddle2Y") < 0.5f)
			{
				DoInteractionStart(Selectables[2]);
				while (_component.GetValue<float>("Paddle2Y") < 0.5f)
					yield return null;
				DoInteractionEnd(Selectables[2]);
			}
			else
			{
				DoInteractionStart(Selectables[3]);
				while (_component.GetValue<float>("Paddle2Y") > 0.5f)
					yield return null;
				DoInteractionEnd(Selectables[3]);
			}
		}
	}

	private IEnumerator TPSpeedModifier()
	{
		bool setValue = true;
		while (true)
		{
			yield return null;
			if (setValue && _component.GetValue<bool>("playing"))
			{
				setValue = false;
				_component.SetValue("BallVel", _component.GetValue<Vector2>("BallVel") * speedModifier);
			}
			else if (!setValue && !_component.GetValue<bool>("playing"))
				setValue = true;
		}
	}

	private readonly float speedModifier = 0.5f;
}