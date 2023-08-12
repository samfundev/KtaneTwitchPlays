using System;
using System.Collections;
using System.Linq;

public class BlaseballComponentSolver : ReflectionComponentSolver
{
	public BlaseballComponentSolver(TwitchModule module) :
		base(module, "blaseballScript", "!{0} away/home <team> [Sets the away or home team to the specified team] | !{0} submit [Presses the BLASEBALL button]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (command.Equals("submit"))
		{
			yield return null;
			yield return Click(0, 0);
		}
		else if (split[0].Equals("away"))
		{
			if (split.Length == 1)
				yield return "sendtochaterror Please specify a team to set as the away team!";
			else
			{
				string team = split.Join(" ").Substring(5).ToLower();
				string[] awayOptions = _component.GetValue<string[]>("awayTeamSelected").Select(x => x.ToLower()).ToArray();
				if (!awayOptions.Contains(team))
				{
					yield return "sendtochaterror!f The specified team '" + split.Join(" ").Substring(5) + "' is invalid!";
					yield break;
				}
				yield return null;
				yield return SelectIndex(_component.GetValue<int>("awayTeamMenu"), Array.IndexOf(awayOptions, team), awayOptions.Length, selectables[2], selectables[1]);
			}
		}
		else if (split[0].Equals("home"))
		{
			if (split.Length == 1)
				yield return "sendtochaterror Please specify a team to set as the home team!";
			else
			{
				string team = split.Join(" ").Substring(5).ToLower();
				string[] homeOptions = _component.GetValue<string[]>("homeTeamSelected").Select(x => x.ToLower()).ToArray();
				if (!homeOptions.Contains(team))
				{
					yield return "sendtochaterror!f The specified team '" + split.Join(" ").Substring(5) + "' is invalid!";
					yield break;
				}
				yield return null;
				yield return SelectIndex(_component.GetValue<int>("homeTeamMenu"), Array.IndexOf(homeOptions, team), homeOptions.Length, selectables[4], selectables[3]);
			}
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		string[] teamOptions = _component.GetValue<string[]>("teamOptions");
		int awaySolve = _component.GetValue<int>("awaySolve");
		int homeSolve = _component.GetValue<int>("homeSolve");
		yield return RespondToCommandInternal($"away " + teamOptions[awaySolve]);
		yield return RespondToCommandInternal($"home " + teamOptions[homeSolve]);
		yield return Click(0, 0);
	}
}