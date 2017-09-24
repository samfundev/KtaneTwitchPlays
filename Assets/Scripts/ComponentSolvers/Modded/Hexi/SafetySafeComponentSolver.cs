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

    public SafetySafeComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _buttons = (MonoBehaviour[])_buttonsField.GetValue(bombComponent.GetComponent(_componentType));
        _lever = (MonoBehaviour)_leverField.GetValue(bombComponent.GetComponent(_componentType));

        helpMessage = "Listen to the dials with !{0} cycle. Listen to a single dial with !{0} cycle BR. Make a correction to a single dial with !{0} BM 3. Enter the solution with !{0} 6 0 6 8 2 5. Submit the answer with !{0} submit. Dial positions are TL, TM, TR, BL, BM, BR.";
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        var split = inputCommand.ToLowerInvariant().Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
        int pos;

        if (split[0] == "submit" && split.Length == 1)
        {
            yield return "submit";
            DoInteractionStart(_lever);
            yield return new WaitForSeconds(0.1f);
            DoInteractionEnd(_lever);
        }
        else if (split[0] == "cycle" && split.Length <= 2)
        {
            yield return "cycle";
            if (split.Length == 1)
            {
                for (var i = 0; i < 6; i++)
                {
                    for (var j = 0; j < 12; j++)
                    {
                        yield return HandlePress(i);
                        yield return new WaitForSeconds(0.3f);
                        if (Canceller.ShouldCancel)
                        {
                            Canceller.ResetCancel();
                            yield break;
                        }
                    }
                    if (i < 5)
                        yield return new WaitForSeconds(0.5f);
                }
            }
            else if (DialPosNames.TryGetValue(split[1], out pos))
            {
                for (var j = 0; j < 12; j++)
                {
                    yield return HandlePress(pos);
                    yield return new WaitForSeconds(0.3f);
                    if (Canceller.ShouldCancel)
                    {
                        Canceller.ResetCancel();
                        yield break;
                    }
                }
            }
        }
        else if (DialPosNames.TryGetValue(split[0], out pos))
        {
            if (split.Length == 1)
            {
                yield return split[0];
                yield return HandlePress(pos);
            }
            else if (split.Length == 2)
            {
                int val = 0;
                if (!int.TryParse(split[1], out val)) yield break;
                val %= 12;
                while (val < 0)
                    val += 12;
                yield return split[0];
                for (int z = 0; z < val; z++)
                {
                    yield return HandlePress(pos);
                    if (Canceller.ShouldCancel)
                    {
                        Canceller.ResetCancel();
                        yield break;
                    }
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
                values[a] %= 12;
                while (values[a] < 0)
                    values[a] += 12;
            }

            yield return inputCommand;
            for (int a = 0; a < 6; a++)
            {
                for (int z = 0; z < values[a]; z++)
                {
                    yield return HandlePress(a);
                    if (Canceller.ShouldCancel)
                    {
                        Canceller.ResetCancel();
                        yield break;
                    }
                }
            }
        }
    }

    private IEnumerator HandlePress(int pos)
    {
        DoInteractionStart(_buttons[pos]);
        yield return new WaitForSeconds(0.1f);
        DoInteractionEnd(_buttons[pos]);
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
