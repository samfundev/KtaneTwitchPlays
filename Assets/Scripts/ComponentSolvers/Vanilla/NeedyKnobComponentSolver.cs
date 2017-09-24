using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class NeedyKnobComponentSolver : ComponentSolver
{
    public NeedyKnobComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _pointingKnob = (MonoBehaviour)_pointingKnobField.GetValue(bombComponent);
        
        helpMessage = "!{0} rotate 3, !{0} turn 3 [rotate the knob 3 quarter-turns]";
        manualCode = "Knobs";
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        string[] commandParts = inputCommand.Split(' ');

        if (commandParts.Length != 2)
        {
            yield break;
        }

        if (!commandParts[0].Equals("rotate", StringComparison.InvariantCultureIgnoreCase) &&
            !commandParts[0].Equals("turn", StringComparison.InvariantCultureIgnoreCase))
        {
            yield break;
        }

        int totalTurnCount = 0;
        if (!int.TryParse(commandParts[1], out totalTurnCount))
        {
            yield break;
        }

        totalTurnCount = totalTurnCount % 4;

        yield return "rotate";

        for (int turnCount = 0; turnCount < totalTurnCount; ++turnCount)
        {
            if (Canceller.ShouldCancel)
            {
                Canceller.ResetCancel();
                yield break;
            }

            DoInteractionStart(_pointingKnob);
            yield return new WaitForSeconds(0.1f);
            DoInteractionEnd(_pointingKnob);
        }
    }

    static NeedyKnobComponentSolver()
    {
        _needyKnobComponentType = ReflectionHelper.FindType("NeedyKnobComponent");
        _pointingKnobField = _needyKnobComponentType.GetField("PointingKnob", BindingFlags.Public | BindingFlags.Instance);
    }

    private static Type _needyKnobComponentType = null;
    private static FieldInfo _pointingKnobField = null;

    private MonoBehaviour _pointingKnob = null;
}
