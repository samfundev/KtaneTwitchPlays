using System;
using System.Collections;
using System.Linq;
using KModkit;

public class NotTimerComponentSolver : ReflectionComponentSolver
{
	public NotTimerComponentSolver(TwitchModule module) :
		base(module, "TimerModuleScript", "!{0} tap/hold <timer/strike> [Taps or holds the timer or strike indicator] | !{0} release <#> [Releases when the countdown timer has a '#' in any position]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if ((command.StartsWith("tap ") || command.StartsWith("hold ")) && split.Length == 2)
		{
			if (!_valids.Contains(split[1]))
				yield break;
			yield return null;
			if (split[0] == "tap")
				yield return Click(Array.IndexOf(_valids, split[1]));
			else
			{
				heldObj = Array.IndexOf(_valids, split[1]);
				DoInteractionStart(selectables[heldObj]);
			}
		}
		else if (command.StartsWith("release ") && split.Length == 2)
		{
			if (heldObj == -1)
				yield break;
			if (!int.TryParse(split[1], out int check))
				yield break;
			if (!check.InRange(1, 9))
				yield break;
			yield return null;
			TimerComponent timerComponent = Module.Bomb.Bomb.GetTimer();
			string secondString = check.ToString();
			float timeRemaining = float.PositiveInfinity;
			while (timeRemaining > 0.0f && heldObj != -1)
			{
				timeRemaining = timerComponent.TimeRemaining;

				if (Module.Bomb.CurrentTimerFormatted.Contains(secondString))
				{
					DoInteractionEnd(selectables[heldObj]);
					heldObj = -1;
				}

				yield return "trycancel";
			}
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		var edgework = Module.BombComponent.GetComponent<KMBombInfo>();
		yield return null;

		int numStrikes = _component.GetValue<int>("NumStrikes");
		float timerTime = _component.GetValue<float>("TimerTime");
		int index, type;
		if (numStrikes == 0 && edgework.IsIndicatorOn(Indicator.CAR))
		{
			index = 1;
			type = 1;
		}
		else if (numStrikes == 0 && edgework.GetBatteryCount() >= 2)
		{
			index = 0;
			type = 0;
		}
		else if (timerTime < 1800f)
		{
			index = 1;
			type = 0;
		}
		else if (edgework.GetBatteryCount() >= 2)
		{
			index = 1;
			type = 1;
		}
		else if (edgework.GetPorts().Contains("Parallel"))
		{
			index = 0;
			type = 0;
		}
		else if (numStrikes == 2 && edgework.IsIndicatorOn(Indicator.FRK))
		{
			index = 1;
			type = 0;
		}
		else if (edgework.IsIndicatorOff(Indicator.SND))
		{
			index = 1;
			type = 0;
		}
		else if (edgework.GetSerialNumber().Any((char x) => "02468".Contains(x)))
		{
			index = 0;
			type = 0;
		}
		else if (edgework.GetPorts().Contains("Serial") && edgework.GetOnIndicators().Any())
		{
			index = 1;
			type = 1;
		}
		else if (!edgework.GetIndicators().Any())
		{
			index = 1;
			type = 0;
		}
		else if (edgework.GetOnIndicators().Count() >= 2)
		{
			index = 0;
			type = 1;
		}
		else if (edgework.GetBatteryCount() == 0)
		{
			index = 0;
			type = 0;
		}
		else if (edgework.GetSerialNumber().Any((char x) => "AEIOU".Contains(x)))
		{
			index = 1;
			type = 0;
		}
		else if (edgework.GetPortPlateCount() >= 2)
		{
			index = 1;
			type = 1;
		}
		else
		{
			index = 1;
			type = 1;
		}
		if (type == 0)
			yield return Click(index);
		else
		{
			float needed;
			int digit;
			if (index == 0)
			{
				switch (numStrikes)
				{
					case 0:
						needed = 3f;
						digit = 2;
						break;
					case 1:
						needed = 6f;
						digit = 3;
						break;
					case 2:
						needed = 9f;
						digit = 9;
						break;
					default:
						needed = 12f;
						digit = 8;
						break;
				}
			}
			else
			{
				redo:
				if (timerTime < 60f)
				{
					needed = 1f;
					digit = 1;
				}
				else if (timerTime < 1800f)
				{
					needed = 3f;
					digit = 4;
				}
				else if (timerTime < 3600f)
				{
					needed = 6f;
					digit = 7;
				}
				else if (timerTime < 7200f)
				{
					needed = 9f;
					digit = 6;
				}
				else if (timerTime < 10800f)
				{
					needed = 12f;
					digit = 5;
				}
				else
				{
					while (_component.GetValue<float>("TimerTime") >= 10800f)
						yield return true;
					timerTime = _component.GetValue<float>("TimerTime");
					goto redo;
				}
			}
			DoInteractionStart(selectables[index]);
			while (_component.GetValue<float>("TimeElapsedSinceHold") < needed)
				yield return true;
			while (!_component.GetValue<string>("FormattedTime").Contains(digit.ToString()[0]))
				yield return true;
			DoInteractionEnd(selectables[index]);
		}
	}

	private readonly string[] _valids = new string[] { "strike", "timer" };
	private int heldObj = -1;
}