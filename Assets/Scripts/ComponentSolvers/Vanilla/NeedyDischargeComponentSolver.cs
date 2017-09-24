using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class NeedyDischargeComponentSolver : ComponentSolver
{
    public NeedyDischargeComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _dischargeButton = (MonoBehaviour)_dischargeButtonField.GetValue(bombComponent);
        
        helpMessage = "!{0} hold 7 [hold the lever for 7 seconds]";
        manualCode = "Capacitor Discharge";
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        string[] commandParts = inputCommand.Split(' ');

        if (commandParts.Length != 2)
        {
            yield break;
        }

        if (!commandParts[0].Equals("hold", StringComparison.InvariantCultureIgnoreCase))
        {
            yield break;
        }

        int holdTime = 0;
        if (!int.TryParse(commandParts[1], out holdTime))
        {
            yield break;
        }

        yield return "hold";

        if (holdTime > 10.0f)
        {
            _musicPlayer = MusicPlayer.StartRandomMusic();
        }

        DoInteractionStart(_dischargeButton);
        yield return new WaitForSecondsWithCancel(holdTime, Canceller);
        DoInteractionEnd(_dischargeButton);

        if (holdTime > 10.0f)
        {
            _musicPlayer.StopMusic();
        }
    }

    static NeedyDischargeComponentSolver()
    {
        _needyDischargeComponentType = ReflectionHelper.FindType("NeedyDischargeComponent");
        _dischargeButtonField = _needyDischargeComponentType.GetField("DischargeButton", BindingFlags.Public | BindingFlags.Instance);
    }

    private static Type _needyDischargeComponentType = null;
    private static FieldInfo _dischargeButtonField = null;

    private MonoBehaviour _dischargeButton = null;
    private MusicPlayer _musicPlayer = null;
}
