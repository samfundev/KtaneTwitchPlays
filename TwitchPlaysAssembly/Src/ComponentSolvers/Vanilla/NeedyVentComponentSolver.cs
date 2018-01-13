using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class NeedyVentComponentSolver : ComponentSolver
{
    public NeedyVentComponentSolver(BombCommander bombCommander, BombComponent bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _yesButton = (MonoBehaviour)_yesButtonField.GetValue(bombComponent);
        _noButton = (MonoBehaviour)_noButtonField.GetValue(bombComponent);
        modInfo = ComponentSolverFactory.GetModuleInfo("NeedyVentComponentSolver");
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        inputCommand = inputCommand.ToLowerInvariant();
        if (inputCommand.EqualsAny("y", "yes", "press y", "press yes"))
        {
            yield return "yes";
            yield return DoInteractionClick(_yesButton);
        }
        else if (inputCommand.EqualsAny("n", "no", "press n", "press no"))
        {
            yield return "no";
            yield return DoInteractionClick(_noButton);
        }
    }

    static NeedyVentComponentSolver()
    {
        _needyVentComponentSolverType = ReflectionHelper.FindType("NeedyVentComponent");
        _yesButtonField = _needyVentComponentSolverType.GetField("YesButton", BindingFlags.Public | BindingFlags.Instance);
        _noButtonField = _needyVentComponentSolverType.GetField("NoButton", BindingFlags.Public | BindingFlags.Instance);
    }

    private static Type _needyVentComponentSolverType = null;
    private static FieldInfo _yesButtonField = null;
    private static FieldInfo _noButtonField = null;

    private MonoBehaviour _yesButton = null;
    private MonoBehaviour _noButton = null;
}
