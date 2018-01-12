using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class SafetySafeComponentSolver : ComponentSolver
{
    private static Dictionary<string, int> DialPosNames = new Dictionary<string, int>()
    {
        {"tl",0}, {"tm",1}, {"tc",1}, {"tr",2},
        {"lt",0}, {"mt",1}, {"ct",1}, {"rt",2},
        {"bl",3}, {"bm",4}, {"bc",4}, {"br",5},
        {"lb",3}, {"mb",4}, {"cb",4}, {"rb",5},
        {"topleft",0}, {"topmiddle",1}, {"topcenter",1}, {"topcentre",1}, {"topright",2},
        {"lefttop",0}, {"middletop",1}, {"centertop",1}, {"centretop",1}, {"righttop",2},
        {"bottomleft",3}, {"bottommiddle",4}, {"bottomcenter",4}, {"bottomcentre",4}, {"bottomright",5},
        {"leftbottom",3}, {"middlebottom",4}, {"centerbottom",4}, {"centrebottom",4}, {"rightbottom",5},
    };

    private static string[] DialNames =
    {
        "top left", "top middle", "top right",
        "bottom left", "bottom middle", "bottom right"
    };

    public SafetySafeComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _buttons = (MonoBehaviour[])_buttonsField.GetValue(bombComponent.GetComponent(_componentType));
        _lever = (MonoBehaviour)_leverField.GetValue(bombComponent.GetComponent(_componentType));
        modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        string[] split = inputCommand.ToLowerInvariant().Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
        int pos;

        if (split[0] == "submit" && split.Length == 1)
        {
            yield return "submit";
            yield return DoInteractionClick(_lever);
        }
        else if (split[0] == "cycle" && split.Length <= 2)
        {
            if (split.Length == 1)
            {
                for (int i = 0; i < 6; i++)
                {
                    IEnumerator cycle = CycleDial(i, i < 5);
                    while (cycle.MoveNext())
                    {
                        yield return cycle.Current;
                    }
                }
            }
            else if (DialPosNames.TryGetValue(split[1], out pos))
            {
                IEnumerator cycle = CycleDial(pos);
                while (cycle.MoveNext())
                {
                    yield return cycle.Current;
                }
            }
        }
        else if (DialPosNames.TryGetValue(split[0], out pos))
        {
            if (split.Length == 1)
            {
                yield return split[0];
                yield return DoInteractionClick(_buttons[pos]);
            }
            else if (split.Length == 2)
            {
                int val = 0;
                if (!int.TryParse(split[1], out val)) yield break;

                IEnumerator set = SetDial(pos, val);
                while (set.MoveNext())
                {
                    yield return set.Current;
                }
            }
        }
        else if (split.Length == 6)
        {
            int[] values = new int[6];
            for (int a = 0; a < 6; a++)
            {
                if (!int.TryParse(split[a], out values[a]))
                    yield break;
            }

            for (int a = 0; a < 6; a++)
            {
                IEnumerator set = SetDial(a, values[a]);
                while (set.MoveNext())
                {
                    yield return set.Current;
                }
            }
        }
    }

    private IEnumerator CycleDial(int pos, bool wait=false)
    {
        yield return "cycle " + pos;
        for (var j = 0; j < 12; j++)
        {
            yield return DoInteractionClick(_buttons[pos]);
            yield return new WaitForSeconds(0.3f);
            if (Canceller.ShouldCancel)
            {
                Canceller.ResetCancel();
                yield break;
            }
        }
        if (wait)
        {
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator SetDial(int pos, int value)
    {
        yield return "move dial " + pos + " by " + value + " clicks";
        value = ((value % 12) + 12) % 12;
        for (int i = 0; i < value; i++)
        {
            yield return DoInteractionClick(_buttons[pos]);
            if (Canceller.ShouldCancel)
            {
                yield return "sendtochat Setting the " + DialNames[pos] + " dial on safety safe was interrupted due to a request to cancel.";
                Canceller.ResetCancel();
                yield break;
            }
        }
    }

    static SafetySafeComponentSolver()
    {
        _componentType = ReflectionHelper.FindType("AdvancedPassword");
        _buttonsField = _componentType.GetField("Dials", BindingFlags.NonPublic | BindingFlags.Instance);
        _leverField = _componentType.GetField("Lever", BindingFlags.Public | BindingFlags.Instance);
    }

    private static Type _componentType = null;
    private static FieldInfo _buttonsField, _leverField = null;

    private MonoBehaviour[] _buttons = null;
    private MonoBehaviour _lever = null;
}
