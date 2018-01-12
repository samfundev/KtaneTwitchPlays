//ListeningComponentSolver
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class ListeningComponentSolver : ComponentSolver
{
    public ListeningComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        Component component = bombComponent.GetComponent("Listening");
        if (component == null)
        {
            throw new NotSupportedException("Could not get Listening Component from bombComponent");
        }

        Type componentType = component.GetType();
        if (componentType == null)
        {
            throw new NotSupportedException("Could not get componentType from Listening Component");
        }

        FieldInfo playField = componentType.GetField("PlayButton", BindingFlags.Public | BindingFlags.Instance);
        FieldInfo dollarField = componentType.GetField("DollarButton", BindingFlags.Public | BindingFlags.Instance);
        FieldInfo poundField = componentType.GetField("PoundButton", BindingFlags.Public | BindingFlags.Instance);
        FieldInfo starField = componentType.GetField("StarButton", BindingFlags.Public | BindingFlags.Instance);
        FieldInfo ampersandField = componentType.GetField("AmpersandButton", BindingFlags.Public | BindingFlags.Instance);
        if (playField == null || dollarField == null || poundField == null || starField == null || ampersandField == null)
        {
            throw new NotSupportedException("Could not find the KMSelectable fields in component Type");
        }

        _buttons = new MonoBehaviour[4];
        _play = (MonoBehaviour)playField.GetValue(component);
        _buttons[0] = (MonoBehaviour)dollarField.GetValue(component);
        _buttons[1] = (MonoBehaviour)poundField.GetValue(component);
        _buttons[2] = (MonoBehaviour)starField.GetValue(component);
        _buttons[3] = (MonoBehaviour)ampersandField.GetValue(component);
        if (_play == null || _buttons.Any(x => x == null))
        {
            throw new NotSupportedException("Component had null KMSelectables.");
        }

        modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        MonoBehaviour button;

        var split = inputCommand.Trim().ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (split.Length < 2 || split[0] != "press")
            yield break;

        var letters = "$#*&";

        foreach (var cmd in split.Skip(1))
            switch (cmd)
            {
                case "play": if (split.Length > 2) yield break; break;
                default:
                    foreach(var x in cmd)
                        if (!letters.Contains(x))
                            yield break;
                    break;
            }   //Check for any invalid commands.  Abort entire sequence if any invalid commands are present.

        yield return "Listening Solve Attempt";
        foreach (var cmd in split.Skip(1))
        {
            switch (cmd)
            {
                case "play":
                    yield return DoInteractionClick(_play);
                    break;
                default:
                    foreach (var x in cmd)
                    {
                        button = _buttons[letters.IndexOf(x)];
                        yield return DoInteractionClick(button);
                    }
                    break;
            }
        }
    }

    private MonoBehaviour _play = null;
    private MonoBehaviour[] _buttons = null;
}
