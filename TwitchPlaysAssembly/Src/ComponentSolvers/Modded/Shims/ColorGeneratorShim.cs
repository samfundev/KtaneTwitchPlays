using System.Collections;
using TwitchPlaysAssembly.ComponentSolvers.Modded.Shims;

public class ColorGeneratorShim : ComponentSolverShim
{
	public ColorGeneratorShim(BombCommander bombCommander, BombComponent bombComponent)
		: base(bombCommander, bombComponent, "Color Generator")
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
