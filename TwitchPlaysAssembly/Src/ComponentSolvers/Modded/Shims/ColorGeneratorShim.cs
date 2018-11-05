using System.Collections;

public class ColorGeneratorShim : ComponentSolverShim
{
	public ColorGeneratorShim(TwitchModule module)
		: base(module, "Color Generator")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		IEnumerator command = RespondToCommandUnshimmed(inputCommand);
		while (command.MoveNext())
		{
			yield return command.Current;
			yield return "trycancel";
		}
	}
}
