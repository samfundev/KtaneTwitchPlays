using System.Collections;

[ModuleID("NeedyScreensaver")]
public class ScreensaverComponentSolver : ReflectionComponentSolver
{
	public ScreensaverComponentSolver(TwitchModule module) :
		base(module, "NeedyScreensaver", "!{0} disarm [Clicks the \"DISARM\" button]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (!command.Equals("disarm")) yield break;
		if (!_component.GetValue<bool>("IsActive"))
		{
			yield return "sendtochaterror You can't interact with the module right now.";
			yield break;
		}

		yield return null;
		yield return Click(0, 0);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		var needyComponent = Module.BombComponent.GetComponent<NeedyComponent>();

		while (true)
		{
			if (needyComponent.State != NeedyComponent.NeedyStateEnum.Running)
			{
				yield return true;
				continue;
			}

			yield return null;
			yield return Click(0);
		}
	}
}