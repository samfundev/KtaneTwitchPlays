using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class ForeignExchangeRatesComponentSolver : ComponentSolver
{
    public ForeignExchangeRatesComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _buttons = (MonoBehaviour[])_buttonsField.GetValue(bombComponent.GetComponent(_componentType));
        helpMessage = "Solve the module with !{0} press ML. Positions are TL, TM, TR, ML, MM, MR, BL, BM, BR.";
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        MonoBehaviour button = null;
        var split = inputCommand.Trim().ToLowerInvariant().Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
        if (split.Length != 2 || split[0] != "press")
            yield break;

        if (_buttons.Length < 9)
            yield break;

        foreach(var cmd in split.Skip(1))
            switch (cmd.Replace("center", "middle").Replace("centre", "middle"))
            {
                case "tl": case "lt": case "topleft": case "lefttop": case "1": button = _buttons[0]; break;
                case "tm": case "tc": case "mt": case "ct": case "topmiddle": case "middletop": case "2": button = _buttons[1]; ; break;
                case "tr": case "rt": case "topright": case "righttop": case "3": button = _buttons[2]; break;

                case "ml": case "cl": case "lm": case "lc": case "middleleft": case "leftmiddle": case "4": button = _buttons[3]; break;
                case "mm": case "cm": case "mc": case "cc": case "middle": case "middlemiddle": case "5": button = _buttons[4]; break;
                case "mr": case "cr": case "rm": case "rc": case "middleright": case "rightmiddle": case "6": button = _buttons[5]; break;

                case "bl": case "lb": case "bottomleft": case "leftbottom": case "7": button = _buttons[6]; break;
                case "bm": case "bc": case "mb": case "cb": case "bottommiddle": case "middlebottom": case "8": button = _buttons[7]; break;
                case "br": case "rb": case "bottomright": case "rightbottom": case "9": button = _buttons[8]; break;

                default: yield break;
            }



        if (button == null)
            yield break;

        yield return "Foreign Exchange Rates Solve Attempt";
        DoInteractionStart(button);
        yield return new WaitForSeconds(0.1f);
        DoInteractionEnd(button);
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
