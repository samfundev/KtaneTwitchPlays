using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class ChordProgressionsComponentSolver : ComponentSolver
{
	public ChordProgressionsComponentSolver(TwitchModule module) :
		base(module)
	{
		component = Module.BombComponent.GetComponent(componentType);
		selectables = Module.BombComponent.GetComponent<KMSelectable>().Children;
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "!{0} submit <note> <type> [submit a chord] | b and # can be used for flat and sharp. | Types: major, minor and diminished");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = Regex.Replace(inputCommand.ToLowerInvariant().Trim(), "^(press|hit|enter|push|submit)", "").Replace('♯', '#').Replace('♭', 'b');
		string[] split = inputCommand.SplitFull(' ', ',', ';');

		if (split.Length == 2 && notes.Contains(split[0]) && split[1] != "m" && chordTypes.Any(chord => chord.StartsWith(split[1])))
		{
			yield return null;
			yield return SelectIndex(component.GetValue<int>("responseKeyOne"), notes.IndexOf(split[0]), notes.Count, selectables[0], selectables[3]);
			yield return SelectIndex(component.GetValue<int>("responseKeyTwo"), chordTypes.IndexOf(chord => chord.StartsWith(split[1])), chordTypes.Count, selectables[1], selectables[4]);
			yield return DoInteractionClick(selectables[2]);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		var ans = component.GetValue<int>("ans");

		yield return null;
		yield return RespondToCommandInternal($"{notes[ans / 3]} {chordTypes[ans.Mod(3)]}");
	}

	private static Type componentType = ReflectionHelper.FindType("ChordProgressions");

	private readonly object component;
	private readonly KMSelectable[] selectables;
	private List<string> notes = new List<string> { "c", "c#", "d", "eb", "e", "f", "f#", "g", "g#", "ab", "a", "bb", "b" };
	private List<string> chordTypes = new List<string> { "major", "minor", "diminished" };
}
