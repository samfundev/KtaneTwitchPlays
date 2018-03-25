using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Rules;
using UnityEngine;

public class ButtonComponentSolver : ComponentSolver
{
	public ButtonComponentSolver(BombCommander bombCommander, ButtonComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		ModuleInformation buttonInfo = ComponentSolverFactory.GetModuleInfo("ButtonComponentSolver", "!{0} tap [tap the button] | !{0} hold [hold the button] | !{0} release 7 [release when the digit shows 7]");
		ModuleInformation buttonInfoModified = ComponentSolverFactory.GetModuleInfo("ButtonComponentModifiedSolver", "Click the button with !{0} tap. Click the button at time with !{0} tap 8:55 8:44 8:33. Hold the button with !{0} hold. Release the button with !{0} release 9:58 9:49 9:30.");

		bombComponent.GetComponent<Selectable>().OnCancel += bombComponent.OnButtonCancel;
		_button = bombComponent.button;
		modInfo = VanillaRuleModifier.IsSeedVanilla() ? buttonInfo : buttonInfoModified;
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		bool isModdedSeed = VanillaRuleModifier.IsSeedModded();
		inputCommand = inputCommand.ToLowerInvariant();
		if (!_held && inputCommand.EqualsAny("tap", "click"))
		{
			yield return "tap";
			yield return DoInteractionClick(_button);

		}
		if (!_held && (inputCommand.StartsWith("tap ") ||
					   inputCommand.StartsWith("click ")))
		{
			if (!isModdedSeed)
				yield break;
			yield return "tap2";

			IEnumerator releaseCoroutine = ReleaseCoroutineModded(inputCommand.Substring(inputCommand.IndexOf(' ')));
			while (releaseCoroutine.MoveNext())
			{
				yield return releaseCoroutine.Current;
			}
		}
		else if (!_held && inputCommand.Equals("hold"))
		{
			yield return "hold";

			_held = true;
			DoInteractionStart(_button);
			yield return new WaitForSeconds(2.0f);
		}
		else if (_held)
		{
			string[] commandParts = inputCommand.Split(' ');
			if (commandParts.Length == 2 && commandParts[0].Equals("release"))
			{
				IEnumerator releaseCoroutine;

				if (!isModdedSeed)
				{
					if (!int.TryParse(commandParts[1], out int second))
					{
						yield break;
					}
					if (second >= 0 && second <= 9)
						releaseCoroutine = ReleaseCoroutineVanilla(second);
					else
						yield break;
				}
				else
				{
					releaseCoroutine = ReleaseCoroutineModded(inputCommand.Substring(inputCommand.IndexOf(' ')));
				}

				while (releaseCoroutine.MoveNext())
				{
					yield return releaseCoroutine.Current;
				}
			}
		}
	}

	private IEnumerator ReleaseCoroutineVanilla(int second)
	{
		yield return "release";
		TimerComponent timerComponent = BombCommander.Bomb.GetTimer();
		string secondString = second.ToString();
		float timeRemaining = float.PositiveInfinity;
		while (timeRemaining > 0.0f && _held)
		{
			timeRemaining = timerComponent.TimeRemaining;

			if (BombCommander.CurrentTimerFormatted.Contains(secondString))
			{
				DoInteractionEnd(_button);
				_held = false;
			}

			yield return string.Format("trycancel The button was not {0} due to a request to cancel.", _held ? "released" : "tapped");
		}
	}

	private IEnumerator ReleaseCoroutineModded(string second)
	{
		TimerComponent timerComponent = BombCommander.Bomb.GetTimer();
		int target = Mathf.FloorToInt(timerComponent.TimeRemaining);
		bool waitingMusic = true;

		string[] times = second.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
		List<int> result = new List<int>();

		foreach (string time in times)
		{
			string[] split = time.Split(':');
			int minutesInt = 0, hoursInt = 0, daysInt = 0;
			switch (split.Length)
			{
				case 1 when int.TryParse(split[0], out int secondsInt):
				case 2 when int.TryParse(split[0], out minutesInt) && int.TryParse(split[1], out secondsInt):
				case 3 when int.TryParse(split[0], out hoursInt) && int.TryParse(split[1], out minutesInt) && int.TryParse(split[2], out secondsInt):
				case 4 when int.TryParse(split[0], out daysInt) && int.TryParse(split[1], out hoursInt) && int.TryParse(split[2], out minutesInt) && int.TryParse(split[3], out secondsInt):
					result.Add((daysInt * 86400) + (hoursInt * 3600) + (minutesInt * 60) + secondsInt);
					break;
				default:
					yield break;
			}
		}
		yield return null;

		bool minutes = times.Any(x => x.Contains(":"));
		minutes |= result.Any(x => x >= 60);

		if (!minutes)
		{
			target %= 60;
			result = result.Select(x => x % 60).Distinct().ToList();
		}

		for (int i = result.Count - 1; i >= 0; i--)
		{
			int r = result[i];
			if (!minutes && !OtherModes.ZenModeOn)
			{
				waitingMusic &= ((target + (r > target ? 60 : 0)) - r) > 30;
			}
			else if (!minutes)
			{
				waitingMusic &= ((r + (r < target ? 60 : 0)) - target) > 30;
			}
			else if (!OtherModes.ZenModeOn)
			{
				if (r > target) { result.RemoveAt(i); continue; }
				waitingMusic &= (target - r) > 30;
			}
			else
			{
				if (r < target) { result.RemoveAt(i); continue; }
				waitingMusic &= (r - target) > 30;
			}
		}

		if (!result.Any())
		{
			yield return string.Format("sendtochaterror The button was not {0} because all of your specfied times are {1} than the time remaining.", _held ? "released" : "tapped", OtherModes.ZenModeOn ? "less" : "greater");
			yield break;
		}

		if (waitingMusic)
			yield return "waiting music";

		while (result.All(x => x != target))
		{
			yield return string.Format("trycancel The button was not {0} due to a request to cancel.", _held ? "released" : "tapped");
			target = (int)(timerComponent.TimeRemaining + (OtherModes.ZenModeOn ? -0.25f : 0.25f));
			if (!minutes) target %= 60;
		}

		if (!_held)
		{
			yield return DoInteractionClick(_button);
		}
		else
		{
			DoInteractionEnd(_button);
		}
		_held = false;
	}

	protected static Rule ForcedSolveRule()
	{
		var rule = new Rule();
		rule.Queries.Add(new Query { Property = ButtonForceSolveQuery, Args = new Dictionary<string, object>() });
		rule.SolutionArgs = new Dictionary<string, object>();
		rule.Solution = ButtonForceSolveSolution;
		return rule;
	}

	protected static QueryableButtonProperty ButtonForceSolveQuery = new QueryableButtonProperty
	{
		Name = "forcesolve",
		Text = $"If the user with AccessLevel.Admin or higher has issued !# solve on this Big Button instance on Twitch Plays",
		QueryFunc = ((BombComponent comp, Dictionary<string, object> args) => true)
	};

	protected static Solution ButtonForceSolveSolution = new Solution
	{
		Text = "Force solve The Button.",
		SolutionMethod = (BombComponent comp, Dictionary<string, object> args) => 0
	};

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		var ruleset = RuleManager.Instance.ButtonRuleSet;
		ruleset.HoldRuleList.Insert(0, ForcedSolveRule());
		ruleset.RuleList.Insert(0, ForcedSolveRule());
		if (!_held)
			yield return DoInteractionClick(_button);
		else
		{
			DoInteractionEnd(_button);
			_held = false;
		}
		ruleset.HoldRuleList.RemoveAt(0);
		ruleset.RuleList.RemoveAt(0);
	}

	private PressableButton _button = null;
	private bool _held = false;
}
