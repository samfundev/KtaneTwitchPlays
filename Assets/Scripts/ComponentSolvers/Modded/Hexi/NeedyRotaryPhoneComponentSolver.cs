using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class NeedyRotaryPhoneComponentSolver : ComponentSolver
{
    public NeedyRotaryPhoneComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _buttons = new MonoBehaviour[10];
        _buttons[0] = (MonoBehaviour)_button0Field.GetValue(bombComponent.GetComponent(_componentSolverType));
        _buttons[1] = (MonoBehaviour)_button1Field.GetValue(bombComponent.GetComponent(_componentSolverType));
        _buttons[2] = (MonoBehaviour)_button2Field.GetValue(bombComponent.GetComponent(_componentSolverType));
        _buttons[3] = (MonoBehaviour)_button3Field.GetValue(bombComponent.GetComponent(_componentSolverType));
        _buttons[4] = (MonoBehaviour)_button4Field.GetValue(bombComponent.GetComponent(_componentSolverType));
        _buttons[5] = (MonoBehaviour)_button5Field.GetValue(bombComponent.GetComponent(_componentSolverType));
        _buttons[6] = (MonoBehaviour)_button6Field.GetValue(bombComponent.GetComponent(_componentSolverType));
        _buttons[7] = (MonoBehaviour)_button7Field.GetValue(bombComponent.GetComponent(_componentSolverType));
        _buttons[8] = (MonoBehaviour)_button8Field.GetValue(bombComponent.GetComponent(_componentSolverType));
        _buttons[9] = (MonoBehaviour)_button9Field.GetValue(bombComponent.GetComponent(_componentSolverType));

        helpMessage = "Respond to the phone call with !{0} press 8 4 9.";
        manualCode = "Rotary%20Phone";
    }

    private static float[] delayTimes = new float[]{
        2.16f, 0.74f, 0.80f, 0.94f, 1.10f,
        1.30f, 1.44f, 1.54f, 1.76f, 1.98f
    };

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        if (inputCommand.StartsWith("press ", StringComparison.InvariantCultureIgnoreCase))
        {
            int val = -1;
            if(int.TryParse(inputCommand.Substring(6), out val) && val >= 0 && val <= 999)
            {
                yield return "press";

                int first = val / 100;
                int second = (val / 10) % 10;
                int third = val % 10;

                DoInteractionStart(_buttons[first]);
                yield return new WaitForSeconds(0.1f);
                DoInteractionEnd(_buttons[first]);
                yield return new WaitForSeconds(delayTimes[first]);
                
                DoInteractionStart(_buttons[second]);
                yield return new WaitForSeconds(0.1f);
                DoInteractionEnd(_buttons[second]);
                yield return new WaitForSeconds(delayTimes[second]);
                
                DoInteractionStart(_buttons[third]);
                yield return new WaitForSeconds(0.1f);
                DoInteractionEnd(_buttons[third]);
                yield return new WaitForSeconds(delayTimes[third]);

                yield break;
            }
        }
    }

    static NeedyRotaryPhoneComponentSolver()
    {
        _componentSolverType = ReflectionHelper.FindType("AdvancedKnob");
        _button0Field = _componentSolverType.GetField("Button0", BindingFlags.Public | BindingFlags.Instance);
        _button1Field = _componentSolverType.GetField("Button1", BindingFlags.Public | BindingFlags.Instance);
        _button2Field = _componentSolverType.GetField("Button2", BindingFlags.Public | BindingFlags.Instance);
        _button3Field = _componentSolverType.GetField("Button3", BindingFlags.Public | BindingFlags.Instance);
        _button4Field = _componentSolverType.GetField("Button4", BindingFlags.Public | BindingFlags.Instance);
        _button5Field = _componentSolverType.GetField("Button5", BindingFlags.Public | BindingFlags.Instance);
        _button6Field = _componentSolverType.GetField("Button6", BindingFlags.Public | BindingFlags.Instance);
        _button7Field = _componentSolverType.GetField("Button7", BindingFlags.Public | BindingFlags.Instance);
        _button8Field = _componentSolverType.GetField("Button8", BindingFlags.Public | BindingFlags.Instance);
        _button9Field = _componentSolverType.GetField("Button9", BindingFlags.Public | BindingFlags.Instance);
    }

    private static Type _componentSolverType = null;
    private static FieldInfo _button0Field = null;
    private static FieldInfo _button1Field = null;
    private static FieldInfo _button2Field = null;
    private static FieldInfo _button3Field = null;
    private static FieldInfo _button4Field = null;
    private static FieldInfo _button5Field = null;
    private static FieldInfo _button6Field = null;
    private static FieldInfo _button7Field = null;
    private static FieldInfo _button8Field = null;
    private static FieldInfo _button9Field = null;

    private MonoBehaviour[] _buttons = null;
}
