using System;
using System.Collections;

public class NeedyDischargeComponentSolver : ComponentSolver
{
	public NeedyDischargeComponentSolver(TwitchModule module) :
		base(module)
	{
		_dischargeButton = ((NeedyDischargeComponent) module.BombComponent).DischargeButton;
		ModInfo = ComponentSolverFactory.GetModuleInfo("NeedyCapacitor", "!{0} hold 7 [hold the lever for 7 seconds]");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] commandParts = inputCommand.Trim().Split(' ');

		if (commandParts.Length != 2 || !commandParts[0].Equals("hold", StringComparison.InvariantCultureIgnoreCase))
			yield break;

		if (!float.TryParse(commandParts[1], out float holdTime)) yield break;

		yield return "hold";

		if (holdTime > 9) yield return "elevator music";

		DoInteractionStart(_dischargeButton);
		yield return new WaitForSecondsWithCancel(holdTime);
		DoInteractionEnd(_dischargeButton);
	}

	private readonly SpringedSwitch _dischargeButton;
}
