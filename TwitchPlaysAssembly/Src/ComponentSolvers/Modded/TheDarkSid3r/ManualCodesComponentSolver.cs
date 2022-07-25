using System;
using System.Collections;
using System.Linq;

public class ManualCodesComponentSolver : ReflectionComponentSolver
{
	public ManualCodesComponentSolver(TwitchModule module) :
		base(module, "ManualCodeNeedy", "!{0} press <#/d/s> [Presses the specified button, where '#' is the digits 0-9, 'd' is delete and 's' is submit] | Presses can be chained, for ex: !{0} press 471s")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !command.StartsWith("press ")) yield break;
		for (int i = 0; i < split[1].Length; i++)
			if (!_order.Contains(split[1][i])) yield break;
		if (!_component.GetValue<bool>("_isActive"))
		{
			yield return "sendtochaterror You can't interact with the module right now.";
			yield break;
		}

		yield return null;
		for (int i = 0; i < split[1].Length; i++)
			yield return Click(Array.IndexOf(_order, split[1][i]));
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
			string ans = _component.GetValue<object>("_currentMetadata").GetValue<string>("LockCode");
			while (!ans.StartsWith(_component.GetValue<string>("_currentCode")))
				yield return Click(0);
			int start = _component.GetValue<string>("_currentCode").Length;
			for (int i = start; i < 3; i++)
				yield return Click(Array.IndexOf(_order, ans[i]));
			yield return Click(8);
		}
	}

	private readonly char[] _order = new char[] { 'd', '1', '2', '3', '0', '4', '5', '6', 's', '7', '8', '9' };
}