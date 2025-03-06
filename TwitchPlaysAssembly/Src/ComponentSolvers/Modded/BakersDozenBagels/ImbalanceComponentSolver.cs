using System.Collections;
using UnityEngine;

[ModuleID("imbalance")]
public class ImbalanceComponentSolver : ReflectionComponentSolver
{
	public ImbalanceComponentSolver(TwitchModule module) :
		base(module, "ImbalanceScript", "!{0} press <#> [Presses the button when the digit on it is '#'] | Multiple presses can be done in one command, for ex: !{0} press 1836")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !command.StartsWith("press ")) yield break;
		if (!int.TryParse(split[1], out int check)) yield break;
		if (check < 0) yield break;

		yield return null;
		TimerComponent timerComponent = Module.Bomb.Bomb.GetTimer();
		for (int i = 0; i < split[1].Length; i++)
		{
			while ((int) timerComponent.TimeRemaining % 10 != (split[1][i] - '0')) yield return "trycancel";
			yield return Click(0);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		int ansInt = _component.GetValue<int>("_solution");
		string ans = ansInt.ToString();
		int start = 0;
		string disp = _component.GetValue<TextMesh>("_textA").text;
		if (!disp.EndsWith(".«"))
			start = disp.Length;
		TimerComponent timerComponent = Module.Bomb.Bomb.GetTimer();
		for (int i = start; i < ans.Length; i++)
		{
			while ((int) timerComponent.TimeRemaining % 10 != (ans[i] - '0')) yield return true;
			yield return Click(0);
		}
	}
}