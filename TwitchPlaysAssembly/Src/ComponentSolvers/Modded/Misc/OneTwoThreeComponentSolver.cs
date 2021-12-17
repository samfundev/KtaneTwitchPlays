using System.Collections;

public class OneTwoThreeComponentSolver : ReflectionComponentSolver
{
	public OneTwoThreeComponentSolver(TwitchModule module) :
		base(module, "ModuleScript", "!{0} press <1-3> (1-3)... [Presses the button(s) with the specified label(s)]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length < 2 || !command.StartsWith("press ")) yield break;
		int[] presses = new int[split.Length - 1];
		for (int i = 1; i < split.Length; i++)
		{
			if (!int.TryParse(split[i], out presses[i - 1]))
				yield break;
			if (presses[i - 1] < 1 || presses[i - 1] > 3)
				yield break;
		}
		if (!_component.GetValue<bool>("isActivated"))
		{
			yield return "sendtochaterror You can't interact with the module right now.";
			yield break;
		}

		yield return null;
		for (int i = 1; i < split.Length; i++)
			yield return Click(presses[i - 1] - 1);
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
			string correctCode = _component.GetValue<string>("code");
			string inputCode = _component.GetValue<string>("inputCode");
			bool error = false;
			for (int i = 0; i < inputCode.Length; i++)
			{
				if (inputCode[i] != correctCode[i])
				{
					_component.GetValue<KMNeedyModule>("module").HandlePass();
					error = true;
					break;
				}
			}
			if (!error)
			{
				int start = inputCode.Length;
				for (int i = start; i < 3; i++)
					yield return Click(correctCode[i] - '0' - 1);
			}
		}
	}
}