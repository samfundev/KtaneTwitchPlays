using System.Collections;

public class AstrologyShim : ComponentSolverShim
{
	public AstrologyShim(TwitchModule module)
		: base(module, "spwizAstrology")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());

		module.BombComponent.OnStrike += _ =>
		{
			ReleaseHeldButtons();
			return false;
		};
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		IEnumerator command = RespondToCommandUnshimmed(inputCommand);
		while (command.MoveNext())
			yield return command.Current;
	}
}
