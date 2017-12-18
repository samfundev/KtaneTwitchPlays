using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class NeedyQuizComponentSolver : ComponentSolver
{
    public NeedyQuizComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _yesButton = (MonoBehaviour)_yesButtonField.GetValue(bombComponent.GetComponent(_componentSolverType));
        _noButton = (MonoBehaviour)_noButtonField.GetValue(bombComponent.GetComponent(_componentSolverType));
        _display = (TextMesh) _displayField.GetValue(bombComponent.GetComponent(_componentSolverType));

        modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
		inputCommand = inputCommand.ToLowerInvariant();

        if (inputCommand.EqualsAny("y", "yes", "press y", "press yes"))
        {
            yield return "yes";

            if (_display.text.Equals("Abort?"))
            {
                yield return new string[] {"detonate", "ABORT! ABORT!!! ABOOOOOOORT!!!!!", "ABORT!"};
                yield break;
            }
            yield return DoInteractionClick(_yesButton);
        }
        else if (inputCommand.EqualsAny("n", "no", "press n", "press no"))
        {
            yield return "no";
            yield return DoInteractionClick(_noButton);
        }
    }

    static NeedyQuizComponentSolver()
    {
        _componentSolverType = ReflectionHelper.FindType("AdvancedVentingGas");
        _yesButtonField = _componentSolverType.GetField("YesButton", BindingFlags.Public | BindingFlags.Instance);
        _noButtonField = _componentSolverType.GetField("NoButton", BindingFlags.Public | BindingFlags.Instance);
        _displayField = _componentSolverType.GetField("Display", BindingFlags.Public | BindingFlags.Instance);
    }

    private static Type _componentSolverType = null;
    private static FieldInfo _yesButtonField = null;
    private static FieldInfo _noButtonField = null;
    private static FieldInfo _displayField = null;

    private MonoBehaviour _yesButton = null;
    private MonoBehaviour _noButton = null;
    private TextMesh _display = null;
}
