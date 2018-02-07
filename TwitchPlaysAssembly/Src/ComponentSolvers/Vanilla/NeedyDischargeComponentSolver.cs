using System;
using System.Collections;

public class NeedyDischargeComponentSolver : ComponentSolver
{
    public NeedyDischargeComponentSolver(BombCommander bombCommander, NeedyDischargeComponent bombComponent) :
        base(bombCommander, bombComponent)
	{
		_dischargeButton = bombComponent.DischargeButton;
        modInfo = ComponentSolverFactory.GetModuleInfo("NeedyDischargeComponentSolver");
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        string[] commandParts = inputCommand.Split(' ');

        if (commandParts.Length != 2 && !commandParts[0].Equals("hold", StringComparison.InvariantCultureIgnoreCase))
            yield break;

        if (!float.TryParse(commandParts[1], out float holdTime))  yield break;

        yield return "hold";

        if (holdTime > 9) yield return "elevator music";

        DoInteractionStart(_dischargeButton);
        yield return new WaitForSecondsWithCancel(holdTime);
        DoInteractionEnd(_dischargeButton);
    }

    private SpringedSwitch _dischargeButton = null;
}
