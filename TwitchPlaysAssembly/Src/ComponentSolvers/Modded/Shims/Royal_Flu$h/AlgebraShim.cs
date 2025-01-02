using System;
using System.Collections;
using UnityEngine;

public class AlgebraShim : ComponentSolverShim
{
	public AlgebraShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_keypadButtons = new KMSelectable[] { _component.GetValue<KMSelectable>("but0"), _component.GetValue<KMSelectable>("but1"), _component.GetValue<KMSelectable>("but2"), _component.GetValue<KMSelectable>("but3"), _component.GetValue<KMSelectable>("but4"), _component.GetValue<KMSelectable>("but5"), _component.GetValue<KMSelectable>("but6"), _component.GetValue<KMSelectable>("but7"), _component.GetValue<KMSelectable>("but8"), _component.GetValue<KMSelectable>("but9") };
		_clearButton = _component.GetValue<KMSelectable>("clearBut");
		_submitButton = _component.GetValue<KMSelectable>("submitBut");
		_negButton = _component.GetValue<KMSelectable>("negativeBut");
		_decButton = _component.GetValue<KMSelectable>("decimalBut");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		bool hasMinus = false;
		string curr = _component.GetValue<TextMesh>("inputText").text;
		if (curr.StartsWith("-"))
		{
			hasMinus = true;
			curr = curr.Replace("-", "");
		}
		int stage = _component.GetValue<int>("stage");
		bool hasMinus2 = false;
		string[] values = { "valueA", "valueB", "valueC" };
		string ans = _component.GetValue<decimal>(values[stage - 1]).ToString("G0");
		if (ans.StartsWith("-"))
		{
			hasMinus2 = true;
			ans = ans.Replace("-", "");
		}
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
		for (int i = stage; i < 4; i++)
		{
			int start = 0;
			if (i == stage && !clrPress)
			{
				start = curr.Length;
				if (hasMinus ^ hasMinus2)
					yield return DoInteractionClick(_negButton);
			}
			else
			{
				ans = _component.GetValue<decimal>(values[i - 1]).ToString("G0");
				if (ans.StartsWith("-"))
				{
					ans = ans.Replace("-", "");
					yield return DoInteractionClick(_negButton);
				}
			}
			for (int j = start; j < ans.Length; j++)
			{
				if (ans[j] == '.')
					yield return DoInteractionClick(_decButton);
				else
					yield return DoInteractionClick(_keypadButtons[int.Parse(ans[j].ToString())]);
			}
			yield return DoInteractionClick(_submitButton);
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("algebraScript", "algebra");

	private readonly object _component;

	private readonly KMSelectable[] _keypadButtons;
	private readonly KMSelectable _clearButton;
	private readonly KMSelectable _submitButton;
	private readonly KMSelectable _negButton;
	private readonly KMSelectable _decButton;
}
