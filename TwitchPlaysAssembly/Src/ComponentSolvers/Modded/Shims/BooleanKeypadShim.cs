using System.Collections;

public class BooleanKeypadShim : ComponentSolverShim
{
	public BooleanKeypadShim(TwitchModule module)
		: base(module, "BooleanKeypad")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Use '!{0} press 2 4' to press buttons 2 and 4. | Buttons are indexed 1-4 in reading order.");
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		inputCommand = inputCommand.ToLowerInvariant().Trim().Replace("press", "solve").Replace("submit", "solve");
		IEnumerator command = RespondToCommandUnshimmed(inputCommand.ToLowerInvariant().Trim());
		while (command.MoveNext())
			yield return command.Current;
	}
}
