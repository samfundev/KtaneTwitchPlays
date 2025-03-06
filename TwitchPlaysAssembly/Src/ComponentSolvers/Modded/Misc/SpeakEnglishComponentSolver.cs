using System;
using System.Collections;

[ModuleID("speakEnglish")]
public class SpeakEnglishComponentSolver : ReflectionComponentSolver
{
	public SpeakEnglishComponentSolver(TwitchModule module)
		: base(module, "speakEnglishScript", "Press the top button with !{0} press top.")
	{
	}

	private readonly string[] buttonNames = new string[] { "top", "middle", "bottom" };

	public override IEnumerator Respond(string[] split, string command)
	{
		if (split.Length != 2 || split[0] != "press" || !split[1].EqualsAny(buttonNames))
			yield break;

		yield return null;
		yield return Click(Array.IndexOf(buttonNames, split[1]));
	}
}
