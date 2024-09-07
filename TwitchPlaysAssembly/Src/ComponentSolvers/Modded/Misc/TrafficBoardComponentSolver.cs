using System;
using System.Collections;
using System.Text.RegularExpressions;

public class TrafficBoardComponentSolver : ReflectionComponentSolver
{
	public TrafficBoardComponentSolver(TwitchModule module) :
		base(module, "trafficBoardScript", "Use '!{0} <sign1> <sign2>' to submit signs (sign1 - top, sign2 - bottom). Possible signs: f - forward, fl - forward/left, fr - forward/right, l - left, lr - left/right, ls - left side (bottom-left arrow), r - right, rs - right side (bottom-right arrow)")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		var signRE = string.Join("|", sign_commands);
		var match = Regex.Match(command, string.Format("^({0}) ({0})$", signRE));
		if (match.Success)
		{
			yield return null;
			while (_component.GetValue<int>("selrow") != Array.IndexOf(sign_commands, match.Groups[1].Value))
				yield return Click(0);
			while (_component.GetValue<int>("selcolumn") != Array.IndexOf(sign_commands, match.Groups[2].Value))
				yield return Click(1);
			yield return Click(2, 0);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		while (_component.GetValue<int>("active") == 0) yield return true;
		int ansrow = _component.GetValue<int>("ansrow");
		int anscolumn = _component.GetValue<int>("anscolumn");
		while (_component.GetValue<int>("selrow") != ansrow)
			yield return Click(0);
		while (_component.GetValue<int>("selcolumn") != anscolumn)
			yield return Click(1);
		yield return Click(2, 0);
	}

	private readonly string[] sign_commands = new[] { "f", "fl", "fr", "l", "lr", "ls", "r", "rs" };
}