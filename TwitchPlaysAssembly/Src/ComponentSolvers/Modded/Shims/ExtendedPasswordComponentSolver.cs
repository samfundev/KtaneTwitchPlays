using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class ExtendedPasswordComponentSolver : ComponentSolver
{
    public ExtendedPasswordComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _component = bombComponent.GetComponent(_componentType);
        modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        IEnumerator command = (IEnumerator)_ProcessCommandMethod.Invoke(_component, new object[] { inputCommand });
        while (command.MoveNext())
        {
            yield return command.Current;
        }
        if (inputCommand.Trim().Length == 6)
        {
            yield return null;
            yield return "unsubmittablepenalty";
        }
    }


    static ExtendedPasswordComponentSolver()
    {
        _componentType = ReflectionHelper.FindType("ExtendedPassword", "ExtendedPassword");
        _ProcessCommandMethod = _componentType.GetMethod("ProcessTwitchCommand", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private static Type _componentType = null;
    private static MethodInfo _ProcessCommandMethod = null;

    private object _component = null;
}
