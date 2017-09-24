using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class TurnTheKeyAdvancedComponentSolver : ComponentSolver
{
    public TurnTheKeyAdvancedComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _leftKey = (MonoBehaviour)_leftKeyField.GetValue(bombComponent.GetComponent(_componentType));
        _rightKey = (MonoBehaviour)_rightKeyField.GetValue(bombComponent.GetComponent(_componentType));

        helpMessage = "Turn the left key with !{0} turn left. Turn the right key with !{0} turn right.";
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {

        var commands = inputCommand.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (commands.Length != 2 || commands[0] != "turn")
            yield break;

        MonoBehaviour Key;
        switch (commands[1])
        {
            case "l": case "left":
                Key = _leftKey;
                break;
            case "r": case "right":
                Key = _rightKey;
                break;
            default:
                yield break;
        }
        yield return "Turning the key";
        DoInteractionStart(Key);
        yield return new WaitForSeconds(0.1f);
        DoInteractionEnd(Key);
    }

    static TurnTheKeyAdvancedComponentSolver()
    {
        _componentType = ReflectionHelper.FindType("TurnKeyAdvancedModule");
        _leftKeyField = _componentType.GetField("LeftKey", BindingFlags.Public | BindingFlags.Instance);
        _rightKeyField = _componentType.GetField("RightKey", BindingFlags.Public | BindingFlags.Instance);
    }

    private static Type _componentType = null;
    private static FieldInfo _leftKeyField = null;
    private static FieldInfo _rightKeyField = null;

    private MonoBehaviour _leftKey = null;
    private MonoBehaviour _rightKey = null;
}
