using System;
using System.Collections;

public class ShakerComponentSolver : ReflectionComponentSolver
{
	public ShakerComponentSolver(TwitchModule module) :
		base(module, "ShakerScript", "!{0} toggle <p1> (p2)... [Toggles the light(s) in the specified position(s)] | !{0} submit [Presses the center of the module] | Valid positions are tl, tr, bl, br, or 1-4 in reading order | To shake the balls around use TP's tilt command")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (command.Equals("submit"))
		{
			yield return null;
			yield return Click(4, 0);
		}
		else
		{
			if (split.Length < 2 || !command.StartsWith("toggle ")) yield break;
			for (int i = 1; i < split.Length; i++)
			{
				if (!split[i].EqualsAny("tl", "tr", "bl", "br", "1", "2", "3", "4")) yield break;
			}

			yield return null;
			string[] positions = new string[] { "bl", "tl", "br", "tr" };
			string[] positions2 = new string[] { "3", "1", "4", "2" };
			for (int i = 1; i < split.Length; i++)
			{
				int index = Array.IndexOf(positions, split[i]);
				if (index == -1)
					index = Array.IndexOf(positions2, split[i]);
				yield return Click(index, .2f);
			}
		}
	}
}