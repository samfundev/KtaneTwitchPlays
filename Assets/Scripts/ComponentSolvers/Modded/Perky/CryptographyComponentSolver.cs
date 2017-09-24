using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class CryptographyComponentSolver : ComponentSolver
{
    public CryptographyComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _buttons = (MonoBehaviour[])_keysField.GetValue(bombComponent.GetComponent(_componentType));
        helpMessage = "Solve the cryptography puzzle with !{0} press N B V T K.";
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        var BeforeStrikes = StrikeCount;

        var split = inputCommand.Trim().ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (split.Length < 2 || split[0] != "press")
            yield break;

        string keytext = _buttons.Aggregate(string.Empty, (current, button) => current + ((KMSelectable) button).GetComponentInChildren<TextMesh>().text.ToLowerInvariant());

        foreach (var x in split.Skip(1))
        {
            foreach (var y in x)
                if (!keytext.Contains(y))
                    yield break;
        }

        yield return "Cryptography Solve Attempt";
        foreach (var x in split.Skip(1))
        {
            foreach (var y in x)
            {
                DoInteractionStart(_buttons[keytext.IndexOf(y)]);
                yield return new WaitForSeconds(0.1f);
                DoInteractionEnd(_buttons[keytext.IndexOf(y)]);
                if (StrikeCount != BeforeStrikes || Solved)
                    yield break;
            }
        }
    }

    static CryptographyComponentSolver()
    {
        _componentType = ReflectionHelper.FindType("CryptMod");
        _keysField = _componentType.GetField("Keys", BindingFlags.Public | BindingFlags.Instance);
    }

    private static Type _componentType = null;
    private static FieldInfo _keysField = null;

    private MonoBehaviour[] _buttons = null;
}
