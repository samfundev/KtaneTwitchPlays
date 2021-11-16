using System;
using System.Collections;

public class PayRespectsComponentSolver : ReflectionComponentSolver
{
	public PayRespectsComponentSolver(TwitchModule module) :
		base(module, "PayRespectsScript", "!{0} f [Pay respects until timer is maxed]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (!command.Equals("f")) yield break;
		var needyComponent = Module.BombComponent.GetComponent<NeedyComponent>();
		if (needyComponent.State != NeedyComponent.NeedyStateEnum.Running)
		{
			yield return "sendtochaterror You can't interact with the module right now.";
			yield break;
		}

		yield return null;
		while ((int)Math.Ceiling(needyComponent.TimeRemaining) != 30)
			yield return Click(0);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		var needyComponent = Module.BombComponent.GetComponent<NeedyComponent>();

		while (true)
		{
			if (needyComponent.State != NeedyComponent.NeedyStateEnum.Running || (int)Math.Ceiling(needyComponent.TimeRemaining) == 30)
			{
				yield return true;
				continue;
			}

			yield return null;
			while ((int)Math.Ceiling(needyComponent.TimeRemaining) != 30)
				yield return Click(0);
		}
	}
}