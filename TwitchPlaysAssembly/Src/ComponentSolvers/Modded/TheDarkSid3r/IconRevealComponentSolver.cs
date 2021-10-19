using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class IconRevealComponentSolver : ReflectionComponentSolver
{
	public IconRevealComponentSolver(TwitchModule module) :
		base(module, "IconReveal", "!{0} submit <symbol> [Submits the specified symbol]")
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || !command.StartsWith("submit ")) yield break;
		List<string> symbols = _component.GetValue<List<string>>("Symbols");
		if (symbols.Count(x => x.Equals(split[1], StringComparison.OrdinalIgnoreCase)) == 0) yield break;

		yield return null;
		yield return SelectIndex(_component.GetValue<int>("NumCounter"), symbols.FindIndex(x => x.Equals(split[1], StringComparison.OrdinalIgnoreCase)), symbols.Count, _component.GetValue<KMSelectable>("RightButton"), _component.GetValue<KMSelectable>("LeftButton"));
		yield return Click(2, 0);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		yield return RespondToCommandInternal($"submit " + _component.GetValue<string>("SelectedModuleSymbol"));
	}
}