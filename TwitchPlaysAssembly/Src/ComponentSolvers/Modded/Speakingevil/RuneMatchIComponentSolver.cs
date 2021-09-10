using System;
using System.Collections;
using System.Linq;

public class RuneMatchIComponentSolver : ReflectionComponentSolver
{
	public RuneMatchIComponentSolver(TwitchModule module) :
		base(module, "RuneMatchScript", "!{0} <a-e><1-5> [Selects the orb at the specified coordinate] | On Twitch Plays this module has an additional 15 seconds")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 1) yield break;
		string[] coords = { "a1", "b1", "c1", "d1", "e1", "a2", "b2", "c2", "d2", "e2", "a3", "b3", "c3", "d3", "e3", "a4", "b4", "c4", "d4", "e4", "a5", "b5", "c5", "d5", "e5" };
		if (!coords.Contains(command)) yield break;
		bool[] active = _component.GetValue<bool[]>("activeorbs");
		if (!active.Contains(true))
		{
			yield return "sendtochaterror You can't interact with the module right now.";
			yield break;
		}
		if (!active[Array.IndexOf(coords, command)])
		{
			yield return "sendtochaterror You can't select a deactivated orb.";
			yield break;
		}

		yield return null;
		int[] runes = _component.GetValue<int[]>("activerunes");
		int[] targets = _component.GetValue<int[]>("targets");
		int breakCt = _component.GetValue<int>("breakcount");
		if (targets[breakCt] != runes[Array.IndexOf(coords, command)])
			yield return "strike";
		yield return Click(Array.IndexOf(coords, command), 0);
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

			bool[] active = _component.GetValue<bool[]>("activeorbs");
			int[] runes = _component.GetValue<int[]>("activerunes");
			int[] targets = _component.GetValue<int[]>("targets");
			int breakCt = _component.GetValue<int>("breakcount");
			for (int i = breakCt; i < targets.Length; i++)
			{
				for (int j = 0; j < 25; j++)
				{
					if (runes[j] == targets[i] && active[j])
					{
						yield return Click(j);
						break;
					}
				}
			}
		}
	}
}