using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JukeboxShim : ComponentSolverShim
{
	public JukeboxShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_selectables = Module.BombComponent.GetComponent<KMSelectable>().Children;
		_component = module.BombComponent.GetComponent(ComponentType);
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		// If an incorrect answer has already been submitted, we can't solve it without taking a strike.
		List<string> chosenLyrics = _component.GetValue<List<string>>("chosenLyrics");
		List<string> lyricOptions = _component.GetValue<List<string>>("lyricOptions");
		for (int i = 0; i < chosenLyrics.Count; i++)
		{
			if (lyricOptions[i] != chosenLyrics[i])
				yield break;
		}

		yield return null;
		var lyricsText = new string[] { _component.GetValue<TextMesh>("lyric1Text").text, _component.GetValue<TextMesh>("lyric2Text").text, _component.GetValue<TextMesh>("lyric3Text").text };
		int stage = _component.GetValue<int>("stage");
		for (int i = stage; i < 3; i++)
		{
			yield return DoInteractionClick(_selectables[lyricsText.IndexOf(text => text == lyricOptions[i])]);
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("jukeboxScript", "jukebox");

	private readonly object _component;
	private readonly KMSelectable[] _selectables;
}
