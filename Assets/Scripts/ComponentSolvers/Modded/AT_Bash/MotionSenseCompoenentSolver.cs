using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class MotionSenseComponentSolver : ComponentSolver
{
    private readonly IRCConnection _connection;
    public MotionSenseComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {

        _connection = ircConnection;
        _component = bombComponent.GetComponent(_componentType);
        _needy = (KMNeedyModule) _needyField.GetValue(_component);
        helpMessage = "I am a passive module that awards strikes for motion while I am active. Use !{0} status to find out if I am active, and for how long.";
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        if (!inputCommand.Equals("status", StringComparison.InvariantCultureIgnoreCase))
            yield break;

        bool active = (bool)_activeField.GetValue(_component);
        _connection.SendMessage("Motion Sense Status: " + (active ? "Active for " + (int)_needy.GetNeedyTimeRemaining() + " seconds" : "Inactive"));
    }

    static MotionSenseComponentSolver()
    {
        _componentType = ReflectionHelper.FindType("MotionSenseModule");
        _activeField = _componentType.GetField("_active", BindingFlags.NonPublic | BindingFlags.Instance);
        _needyField = _componentType.GetField("NeedyModule", BindingFlags.Public | BindingFlags.Instance);
    }

    private static Type _componentType = null;
    private static Component _component;
    private static FieldInfo _activeField = null;
    private static FieldInfo _needyField = null;

    private KMNeedyModule _needy = null;
}
