using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KModkit;

public class PressXShim : ComponentSolverShim
{
	public PressXShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<KMSelectable[]>("Buttons");
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		var match = Regex.Match(inputCommand.ToLowerInvariant(),
			"^(?:press |tap )?(x|y|a|b)(?:(?: at| on)?([0-9: ]+))?$");
		if (!match.Success || inputCommand.ToLowerInvariant().EqualsAny("press a", "press b", "press x", "press y", "a", "b", "x", "y")) yield break;
		int index = "xyab".IndexOf(match.Groups[1].Value, StringComparison.Ordinal);
		if (index < 0) yield break;

		string[] times = match.Groups[2].Value.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
		List<int> result = new List<int>();

		if (!times.Any() || index >= 2)
		{
			for (int i = 0; i < 60; i++)
				result.Add(i);
		}
		else
		{
			foreach (string time in times)
			{
				int daysInt = 0, hoursInt = 0, minutesInt = 0;
				string[] split = time.Split(':');
				if ((split.Length == 1 && int.TryParse(split[0], out int secondsInt)) ||
					(split.Length == 2 && int.TryParse(split[0], out minutesInt) && int.TryParse(split[1], out secondsInt)) ||
					(split.Length == 3 && int.TryParse(split[0], out hoursInt) && int.TryParse(split[1], out minutesInt) && int.TryParse(split[2], out secondsInt)) ||
					(split.Length == 4 && int.TryParse(split[0], out daysInt) && int.TryParse(split[1], out hoursInt) && int.TryParse(split[2], out minutesInt) && int.TryParse(split[3], out secondsInt)))
					result.Add((daysInt * 86400) + (hoursInt * 3600) + (minutesInt * 60) + secondsInt);
				else
				{
					yield return string.Format("sendtochaterror Badly formatted time {0}. Time should either be in seconds (53) or in full time (1:23:45)", time);
					yield break;
				}
			}
		}
		TimerComponent timerComponent = Module.Bomb.Bomb.GetTimer();
		int curTime = (int) timerComponent.TimeRemaining;
		while ((int) timerComponent.TimeRemaining == curTime) yield return "trycancel The button was not pressed due to a request to cancel";
		IEnumerator command = RespondToCommandUnshimmed(inputCommand);
		while (command.MoveNext())
			yield return command.Current;
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		TimerComponent timerComponent = Module.Bomb.Bomb.GetTimer();
		var edgework = Module.BombComponent.GetComponent<KMBombInfo>();
		int[][] table = new int[][]
		{
			new int[] { 2, 0, 3, 1 },
			new int[] { 3, 1, 2, 0 },
			new int[] { 1, 2, 0, 3 }
		};
		int index;
		if (edgework.GetOffIndicators().Count() > edgework.GetOnIndicators().Count())
			index = 0;
		else if (edgework.GetOffIndicators().Count() < edgework.GetOnIndicators().Count())
			index = 1;
		else
			index = 2;
		reEval:
		int localSolves = edgework.GetSolvedModuleNames().Count();
		int btn = table[index][localSolves % 4];
		if (edgework.IsIndicatorOn(Indicator.CAR) && btn == 0 && edgework.GetBatteryCount() < 2)
		{
			yield return DoInteractionClick(_buttons[UnityEngine.Random.Range(0, 4)]);
			yield break;
		}
		else if (edgework.GetBatteryCount() >= 3)
		{
			int target = edgework.GetSerialNumberNumbers().First();
			while ((int) timerComponent.TimeRemaining % 10 != target)
			{
				yield return true;
				if (localSolves != edgework.GetSolvedModuleNames().Count())
					goto reEval;
			}
		}
		else if (btn == 2 && (edgework.GetSerialNumberNumbers().Contains(2) || edgework.GetSerialNumberNumbers().Contains(5)))
		{
			while ((int) timerComponent.TimeRemaining % 60 != 5 && (int) timerComponent.TimeRemaining % 60 != 30)
			{
				yield return true;
				if (localSolves != edgework.GetSolvedModuleNames().Count())
					goto reEval;
			}
		}
		else if (btn != 1 && edgework.IsIndicatorOn(Indicator.NSA))
		{
			while ((int) timerComponent.TimeRemaining % 60 % 11 != 0)
			{
				yield return true;
				if (localSolves != edgework.GetSolvedModuleNames().Count())
					goto reEval;
			}
		}
		else
		{
			while ((((int) timerComponent.TimeRemaining % 60 / 10) + ((int) timerComponent.TimeRemaining % 60 % 10)) != 9)
			{
				yield return true;
				if (localSolves != edgework.GetSolvedModuleNames().Count())
					goto reEval;
			}
		}
		yield return DoInteractionClick(_buttons[btn]);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("PressX");

	private readonly object _component;
	private readonly KMSelectable[] _buttons;
}