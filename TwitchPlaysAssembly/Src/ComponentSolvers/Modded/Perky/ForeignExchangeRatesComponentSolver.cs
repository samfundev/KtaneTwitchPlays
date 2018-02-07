using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using UnityEngine;

public class ForeignExchangeRatesComponentSolver : ComponentSolver
{
    public ForeignExchangeRatesComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
        base(bombCommander, bombComponent)
	{
        _buttons = (MonoBehaviour[]) _buttonsField.GetValue(bombComponent.GetComponent(_componentType));
        modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        MonoBehaviour button = null;
        var split = inputCommand.Trim().ToLowerInvariant().Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
        if (split.Length != 2 || split[0] != "press")
            yield break;

        if (_buttons.Length < 9)
            yield break;

		var cmds = split.Skip(1).Select(cmd =>
		{
			return cmd.Replace("center", "middle")
			.Replace("centre", "middle")
			.Replace("top", "t")
			.Replace("bottom", "b")
			.Replace("left", "l")
			.Replace("right", "r")
			.Replace("middle", "m");
		});

		foreach (var cmd in cmds)
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

    static ForeignExchangeRatesComponentSolver()
    {
        _componentType = ReflectionHelper.FindType("ForeignExchangeRates");
        _buttonsField = _componentType.GetField("buttons", BindingFlags.Public | BindingFlags.Instance);
    }

    private static Type _componentType = null;
    private static FieldInfo _buttonsField = null;

    private MonoBehaviour[] _buttons = null;
}
