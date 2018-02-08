using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class MotionSenseComponentSolver : ComponentSolver
{
    public MotionSenseComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
        base(bombCommander, bombComponent)
	{
        _component = bombComponent.GetComponent(_componentType);
        _needy = (KMNeedyModule) _needyField.GetValue(_component);
        modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_needy.OnNeedyActivation += () =>
		{
			IRCConnection.Instance.SendMessage($"Motion Sense just activated: Active for {(int)_needy.GetNeedyTimeRemaining()} seconds.");
		};

		_needy.OnTimerExpired += () =>
		{
			IRCConnection.Instance.SendMessage($"Motion Sense now Inactive.");
		};
	}

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        if (!inputCommand.Equals("status", StringComparison.InvariantCultureIgnoreCase))
            yield break;

        bool active = (bool)_activeField.GetValue(_component);
	    IRCConnection.Instance.SendMessage("Motion Sense Status: " + (active ? "Active for " + (int)_needy.GetNeedyTimeRemaining() + " seconds" : "Inactive"));
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
