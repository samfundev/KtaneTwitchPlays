using System.Collections;

public class PatternLockComponentSolver : ReflectionComponentSolver
{
	public PatternLockComponentSolver(TwitchModule module) :
		base(module, "PatternLockModule", "!{0} connect <pos> (pos2)... [Connects the circle(s) in the specified position(s)] | !{0} submit [Presses the \"SUB\" button] | !{0} clear [Presses the \"CLR\" button] | Valid circles are 1-9 in reading order")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (command.Equals("submit"))
		{
			yield return null;
			yield return Click(1, 0);
		}
		else if (command.Equals("clear"))
		{
			yield return null;
			yield return Click(0, 0);
		}
		else if (command.StartsWith("connect ") && split.Length >= 2)
		{
			for (int i = 1; i < split.Length; i++)
			{
				if (!int.TryParse(split[i], out int check)) yield break;
				if (!check.InRange(1, 9)) yield break;
			}

			yield return null;
			for (int i = 1; i < split.Length; i++)
				yield return Click(int.Parse(split[i]) + 2);
		}
	}
}