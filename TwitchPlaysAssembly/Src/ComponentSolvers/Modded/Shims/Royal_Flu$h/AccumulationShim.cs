using System;
using System.Collections;
using UnityEngine;

public class AccumulationShim : ComponentSolverShim
{
	public AccumulationShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_keypadButtons = _component.GetValue<KMSelectable[]>("keypad");
		_clearButton = _component.GetValue<KMSelectable>("clearButton");
		_submitButton = _component.GetValue<KMSelectable>("submitButton");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		while (_component.GetValue<bool>("moduleSolved"))
			yield return true;
		string curr = _component.GetValue<TextMesh>("screenInput").text;
		string ans = _component.GetValue<int>("targetAnswer").ToString();
		bool clrPress = false;
		if (curr.Length > ans.Length)
		{
			yield return DoInteractionClick(_clearButton);
			clrPress = true;
		}
		else
		{
			for (int i = 0; i < curr.Length; i++)
			{
				if (i == ans.Length)
					break;
				if (curr[i] != ans[i])
				{
					yield return DoInteractionClick(_clearButton);
					clrPress = true;
					break;
				}
			}
		}
		int stage = _component.GetValue<int>("stage");
		for (int i = stage; i < 5; i++)
		{
			int start = 0;
			if (i == stage && !clrPress)
				start = curr.Length;
			else
				ans = _component.GetValue<int>("targetAnswer").ToString();
			for (int j = start; j < ans.Length; j++)
			{
				if (ans[j] == '0')
					yield return DoInteractionClick(_keypadButtons[9]);
				else
					yield return DoInteractionClick(_keypadButtons[int.Parse(ans[j].ToString()) - 1]);
			}
			yield return DoInteractionClick(_submitButton, 0);
			if (i != 4)
			{
				while (_component.GetValue<bool>("moduleSolved"))
					yield return true;
			}
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("accumulationScript", "accumulation");

	private readonly object _component;

	private readonly KMSelectable[] _keypadButtons;
	private readonly KMSelectable _clearButton;
	private readonly KMSelectable _submitButton;
}
