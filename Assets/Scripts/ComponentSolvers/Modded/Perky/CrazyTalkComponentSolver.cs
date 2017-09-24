using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class CrazyTalkComponentSolver : ComponentSolver
{
    public CrazyTalkComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _toggle = (MonoBehaviour)_toggleField.GetValue(bombComponent.GetComponent(_componentType));
        helpMessage = "Toggle the switch down and up with !{0} toggle 4 5. The order is down, then up.";
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        int downtime;
        int uptime;

        var commands = inputCommand.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

        if (commands.Length != 3 || !commands[0].Equals("toggle", StringComparison.InvariantCultureIgnoreCase) ||
            !int.TryParse(commands[1],out downtime) || !int.TryParse(commands[2],out uptime))
            yield break;

        if (downtime < 0 || downtime > 9 || uptime < 0 || uptime > 9)
            yield break;

        yield return "Crazy Talk Solve Attempt";
        int beforeButtonStrikeCount = StrikeCount;
        MonoBehaviour timerComponent = (MonoBehaviour)CommonReflectedTypeInfo.GetTimerMethod.Invoke(BombCommander.Bomb, null);
        int timeRemaining = (int)((float)CommonReflectedTypeInfo.TimeRemainingField.GetValue(timerComponent));

        while ((timeRemaining%10) != downtime)
        {
            yield return null;
            timeRemaining = (int)((float)CommonReflectedTypeInfo.TimeRemainingField.GetValue(timerComponent));
        }

        DoInteractionStart(_toggle);
        yield return new WaitForSeconds(0.1f);
        DoInteractionEnd(_toggle);

        if (StrikeCount != beforeButtonStrikeCount)
            yield break;

        while ((timeRemaining % 10) != uptime)
        {
            yield return null;
            timeRemaining = (int)((float)CommonReflectedTypeInfo.TimeRemainingField.GetValue(timerComponent));
        }

        DoInteractionStart(_toggle);
        yield return new WaitForSeconds(0.1f);
        DoInteractionEnd(_toggle);
    }

    static CrazyTalkComponentSolver()
    {
        _componentType = ReflectionHelper.FindType("CrazyTalkModule");
        _toggleField = _componentType.GetField("toggleSwitch", BindingFlags.Public | BindingFlags.Instance);
    }

    private static Type _componentType = null;
    private static FieldInfo _toggleField = null;

    private MonoBehaviour _toggle = null;
}
