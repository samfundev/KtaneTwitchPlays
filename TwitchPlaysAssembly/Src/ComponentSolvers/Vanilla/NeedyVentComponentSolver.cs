using System.Collections;

public class NeedyVentComponentSolver : ComponentSolver
{
	public NeedyVentComponentSolver(BombCommander bombCommander, NeedyVentComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		_yesButton = bombComponent.YesButton;
		_noButton = bombComponent.NoButton;
		modInfo = ComponentSolverFactory.GetModuleInfo("NeedyVentComponentSolver", "!{0} yes, !{0} y [answer yes] | !{0} no, !{0} n [answer no]");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.ToLowerInvariant().Trim();
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

	private readonly KeypadButton _yesButton = null;
	private readonly KeypadButton _noButton = null;
}
