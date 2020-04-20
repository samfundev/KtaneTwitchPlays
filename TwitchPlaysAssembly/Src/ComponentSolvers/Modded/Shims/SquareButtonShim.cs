using System.Collections;

public class SquareButtonShim : ComponentSolverShim
{
	public SquareButtonShim(TwitchModule module)
		: base(module, "ButtonV2")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		var pressOrTap = inputCommand.StartsWith("press ") || inputCommand.StartsWith("tap ");

		IEnumerator command = RespondToCommandUnshimmed(inputCommand);
		while (command.MoveNext())
		{
			yield return command.Current;

			if (command.Current is string message && message == "sendtochaterror No valid times." && pressOrTap)
			{
				yield break;
			}
		}
	}
}
