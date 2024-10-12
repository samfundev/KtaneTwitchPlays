using System;
using System.Collections;
using System.Linq;

public class PouComponentSolver : ReflectionComponentSolver
{
	public PouComponentSolver(TwitchModule module) :
		base(module, "NeedyPouModule", "!{0} medicine/pizza/soap/ball/lamp/? [Presses the specified item]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (!items.Contains(command))
			yield break;

		yield return null;
		yield return Click(Array.IndexOf(items, command), 0);
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
			int corItem = _component.GetValue<int>("status");
			yield return Click(corItem == 0 ? corItem : corItem + 1);
		}
	}

	private readonly string[] items = { "soap", "?", "pizza", "lamp", "medicine", "ball" };
}