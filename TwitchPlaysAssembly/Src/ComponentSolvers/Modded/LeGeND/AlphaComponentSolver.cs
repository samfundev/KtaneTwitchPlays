using System.Collections;

public class AlphaComponentSolver : ReflectionComponentSolver
{
	public AlphaComponentSolver(TwitchModule module) :
		base(module, "AlphaModuleScript", "!{0} submit <#> [Submits the specified number]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !command.StartsWith("submit ")) yield break;
		if (!int.TryParse(split[1], out _)) yield break;
		if (int.Parse(split[1]) < 0 || int.Parse(split[1]) > 20) yield break;
		if (!_component.GetValue<bool>("active"))
		{
			yield return "sendtochaterror You can't interact with the module right now.";
			yield break;
		}

		yield return null;
		int inputValue = _component.GetValue<int>("inputValue");
		if (int.Parse(split[1]) < inputValue)
		{
			int times = inputValue - int.Parse(split[1]);
			for (int i = 0; i < times; i++)
				yield return Click(0);
		}
		else if (int.Parse(split[1]) > inputValue)
		{
			int times = int.Parse(split[1]) - inputValue;
			for (int i = 0; i < times; i++)
				yield return Click(1);
		}
		yield return Click(2, 0);
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

			if (_component.GetValue<bool>("active"))
				yield return RespondToCommandInternal("submit " + _component.GetValue<int>("ACount"));
		}
	}
}