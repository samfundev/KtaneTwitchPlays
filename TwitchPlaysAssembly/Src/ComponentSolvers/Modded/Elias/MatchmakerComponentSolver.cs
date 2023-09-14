using System.Collections;
using System.Linq;

public class MatchmakerComponentSolver : ReflectionComponentSolver
{
	public MatchmakerComponentSolver(TwitchModule module) :
		base(module, "HumanResourcesModule", "matchmaker", "!{0} cycle [See the people in each list] | !{0} cycle <top/bottom> [See the people in a specific list] | !{0} match <name> <name> [Selects the two specified people and presses the match button] | !{0} reset [Presses the reset button]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (command.StartsWith("match "))
		{
			if (split.Length != 3) yield break;
			object[] people = _component.GetValue<object[]>("_people");
			int[] names1 = _component.GetValue<int[]>("_availableNames");
			int[] names2 = _component.GetValue<int[]>("_availableDescs");
			bool[] inNames1 = new bool[2];
			bool[] inNames2 = new bool[2];
			for (int i = 1; i < 3; i++)
			{
				if (names1.Any(x => people[x].GetValue<string>("Name").ToLower() == split[i]))
					inNames1[i - 1] = true;
				else if (names2.Any(x => people[x].GetValue<string>("Name").ToLower() == split[i]))
					inNames2[i - 1] = true;
			}
			if ((inNames1[0] && inNames1[1]) || (inNames2[0] && inNames2[1]))
			{
				yield return "sendtochaterror Two people from the same list cannot be matched!";
				yield break;
			}
			if ((!inNames1[0] && !inNames2[0]) || (!inNames1[1] && !inNames2[1]))
			{
				yield return "sendtochaterror One or both people are not present on either list!";
				yield break;
			}
			int curName1Index = _component.GetValue<int>("_nameIndex");
			int curName2Index = _component.GetValue<int>("_descIndex");
			int target1 = inNames1[0] ? 1 : 2;
			int target2 = inNames1[1] ? 1 : 2;

			yield return null;
			yield return SelectIndex(curName1Index, names1.IndexOf(x => people[x].GetValue<string>("Name").ToLower() == split[target1]), names1.Length, Selectables[1], Selectables[0]);
			yield return SelectIndex(curName2Index, names2.IndexOf(x => people[x].GetValue<string>("Name").ToLower() == split[target2]), names2.Length, Selectables[3], Selectables[2]);
			yield return Click(4, 0);
		}
		else if (command.StartsWith("cycle "))
		{
			if (split.Length != 2) yield break;
			if (!split[1].EqualsAny("top", "bottom")) yield break;

			yield return null;
			for (int i = 0; i < 5; i++)
			{
				yield return "trycancel";
				yield return Click(split[1].Equals("top") ? 1 : 3, 1.75f);
			}
		}
		else if (command.Equals("cycle"))
		{
			yield return null;
			for (int i = 0; i < 5; i++)
			{
				yield return "trycancel";
				yield return Click(1, 1.75f);
			}
			for (int i = 0; i < 5; i++)
			{
				yield return "trycancel";
				yield return Click(3, 1.75f);
			}
		}
		else if (command.Equals("reset"))
		{
			yield return null;
			yield return Click(5, 0);
		}
	}
}