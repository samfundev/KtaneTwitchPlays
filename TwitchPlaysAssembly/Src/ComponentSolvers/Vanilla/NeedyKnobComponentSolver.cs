using System.Collections;

public class NeedyKnobComponentSolver : ComponentSolver
{
	public NeedyKnobComponentSolver(TwitchModule module) :
		base(module)
	{
		_pointingKnob = ((NeedyKnobComponent) module.BombComponent).PointingKnob;
		SetHelpMessage("!{0} rotate 3, !{0} turn 3 [rotate the knob 3 quarter-turns]");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] commandParts = inputCommand.ToLowerInvariant().Trim().Split(' ');

		if (commandParts.Length != 2)
		{
			yield break;
		}

		if (!commandParts[0].EqualsAny("rotate", "turn"))
		{
			yield break;
		}

		if (!int.TryParse(commandParts[1], out int totalTurnCount))
		{
			yield break;
		}

		totalTurnCount %= 4;

		yield return "rotate";

		for (int turnCount = 0; turnCount < totalTurnCount; ++turnCount)
		{
			yield return "trycancel";
			yield return DoInteractionClick(_pointingKnob);
		}
	}

	private readonly PointingKnob _pointingKnob;
}
