using System.Collections;

public class SphereShim : ComponentSolverShim
{
	public SphereShim(TwitchModule module)
		: base(module, "sphere")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		// The implementation of sphere is missing a yield return null if the commands are chained.
		if (inputCommand.SplitFull(';', ',').Length > 0)
			yield return null;

		IEnumerator command = RespondToCommandUnshimmed(inputCommand);
		while (command.MoveNext())
			yield return command.Current;
	}
}
