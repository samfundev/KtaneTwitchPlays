using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ForeignExchangeRatesComponentSolver : ComponentSolver
{
	public ForeignExchangeRatesComponentSolver(TwitchModule module) :
		base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<MonoBehaviour[]>("buttons");
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Solve the module with !{0} press ML. Positions are TL, TM, TR, ML, MM, MR, BL, BM, BR.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		MonoBehaviour button = null;
		string[] split = inputCommand.Trim().ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (split.Length != 2 || split[0] != "press")
			yield break;

		if (_buttons.Length < 9)
			yield break;

		IEnumerable<string> cmds = split.Skip(1).Select(cmd => cmd.Replace("center", "middle")
			.Replace("centre", "middle")
			.Replace("top", "t")
			.Replace("bottom", "b")
			.Replace("left", "l")
			.Replace("right", "r")
			.Replace("middle", "m"));

		foreach (string cmd in cmds)
		{
			if (cmd.EqualsAny("tl", "lt", "1")) button = _buttons[0];
			else if (cmd.EqualsAny("tm", "mt", "2")) button = _buttons[1];
			else if (cmd.EqualsAny("tr", "rt", "3")) button = _buttons[2];
			else if (cmd.EqualsAny("ml", "lm", "4")) button = _buttons[3];
			else if (cmd.EqualsAny("mm", "5")) button = _buttons[4];
			else if (cmd.EqualsAny("mr", "rm", "6")) button = _buttons[5];
			else if (cmd.EqualsAny("bl", "lb", "7")) button = _buttons[6];
			else if (cmd.EqualsAny("bm", "mb", "8")) button = _buttons[7];
			else if (cmd.EqualsAny("br", "rb", "9")) button = _buttons[8];
			else yield break;
		}

		if (button == null)
		{
			yield return "autosolve due to the buttons not being set to expected values";
			yield break;
		}

		yield return "Foreign Exchange Rates Solve Attempt";
		yield return DoInteractionClick(button);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		while (!_component.GetValue<bool>("isReadyForInput")) yield return true;
		int answer = _component.GetValue<int>("answer");
		if (answer == 0)
			yield return DoInteractionClick(_buttons[0]);
		else
			yield return DoInteractionClick(_buttons[answer - 1]);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("ForeignExchangeRates");

	private readonly object _component;

	private readonly MonoBehaviour[] _buttons;
}
