using System.Collections;
using System.Collections.Generic;

public class LargePasswordComponentSolver : ReflectionComponentSolver
{
	public LargePasswordComponentSolver(TwitchModule module) :
		base(module, "LargeVanillaPassword", "!{0} cycle 1 13 8 [Cycle through the letters in columns 1, 13, and 8] | !{0} toggle [Move all columns down one letter] | !{0} world about still water [Try to submit words]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (command.Equals("toggle"))
		{
			yield return null;
			for (int i = 10; i < 20; i++)
				yield return Click(i);
			for (int i = 30; i < 40; i++)
				yield return Click(i);
		}
		else if (command.StartsWith("cycle "))
		{
			for (int i = 1; i < split.Length; i++)
			{
				if (!int.TryParse(split[i], out int temp) || temp < 1 || temp > 20)
				{
					yield return string.Format("sendtochaterror “{0}” is not a number from 1 to 20.", split[i]);
					yield break;
				}
			}

			yield return null;
			if (split.Length >= 4)
				yield return "waiting music";
			for (int i = 1; i < split.Length; i++)
			{
				for (int j = 0; j < 6; j++)
				{
					int slot = int.Parse(split[i]);
					if (slot <= 10)
						slot += 10;
					else
						slot += 20;
					yield return Click(slot - 1);
					yield return "trywaitcancel 1.0";
				}
			}
			yield return "end waiting music";
		}
		else if (command.Length == 23)
		{
			yield return null;
			command = command.Replace(" ", "").ToUpper();
			List<char>[] slotChars = _component.GetValue<List<char>[]>("spinnerchars");
			int[] slotPos = _component.GetValue<int[]>("spinnerpositions");
			KMSelectable[] tops = _component.GetValue<KMSelectable[]>("topButtons");
			KMSelectable[] bottoms = _component.GetValue<KMSelectable[]>("bottomButtons");
			bool passed = true;
			for (int i = 0; i < 20; i++)
			{
				var targetIndex = slotChars[i].IndexOf(command[i]);
				if (targetIndex == -1)
				{
					passed = false;
					break;
				}

				yield return SelectIndex(slotPos[i], targetIndex, slotChars[i].Count, bottoms[i], tops[i]);
			}
			if (passed)
				yield return Click(40, 0);
			else
				yield return "unsubmittablepenalty";
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		List<char> passwords = _component.GetValue<List<char>>("passwords");
		string ans = "";
		for (int i = 0; i < 20; i++)
		{
			if (i % 5 == 0 && i != 0)
				ans += " ";
			ans += passwords[i];
		}
		yield return RespondToCommandInternal(ans);
	}
}