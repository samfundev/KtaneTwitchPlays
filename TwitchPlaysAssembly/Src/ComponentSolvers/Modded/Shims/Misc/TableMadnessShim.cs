using System;
using System.Collections;
using UnityEngine;

public class TableMadnessShim : ReflectionComponentSolverShim
{
	public TableMadnessShim(TwitchModule module)
		: base(module, "TableMadness", "TableMadness")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_buttons = _component.GetValue<KMSelectable[]>("buttons");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		TextMesh[] texts = _component.GetValue<TextMesh[]>("answertexts");
		char[] letters = _component.GetValue<char[]>("letters");
		char[] numbers = _component.GetValue<char[]>("numbers");
		string solutionCoord = _component.CallMethod<string>("ConvertToCoordinate", _component.GetValue<int>("solution"));
		int letInd = Array.IndexOf(letters, solutionCoord[0]);
		int numInd = Array.IndexOf(numbers, solutionCoord[1]);
		yield return SelectIndex(Array.IndexOf(letters, texts[0].text[0]), letInd, letters.Length, _buttons[1], _buttons[0]);
		yield return SelectIndex(Array.IndexOf(numbers, texts[1].text[0]), numInd, numbers.Length, _buttons[3], _buttons[2]);
		yield return DoInteractionClick(_buttons[4], 0);
	}

	private readonly KMSelectable[] _buttons;
}
