using System;
using System.Collections;
using UnityEngine;

public class TaxReturnsShim : ComponentSolverShim
{
	public TaxReturnsShim(TwitchModule module)
		: base(module, "taxReturns")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_keypadButtons = _component.GetValue<KMSelectable[]>("keypad");
		_toggleButton = _component.GetValue<KMSelectable>("toggleSwitch");
		_submitButton = _component.GetValue<KMSelectable>("submitBut");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		if (_component.GetValue<bool>("page1"))
			yield return DoInteractionClick(_toggleButton, .2f);
		string curr = _component.GetValue<TextMesh>("amount").text;
		string ans = _component.GetValue<string>("correctAnswer");
		if (curr.Length > ans.Length)
		{
			while (curr.Length != ans.Length)
			{
				yield return DoInteractionClick(_keypadButtons[10]);
				curr = curr.Substring(0, curr.Length - 1);
			}
		}
		for (int i = 0; i < curr.Length; i++)
		{
			if (i == ans.Length)
				break;
			if (curr[i] != ans[i])
			{
				int target = curr.Length - i;
				for (int j = 0; j < target; j++)
				{
					yield return DoInteractionClick(_keypadButtons[10]);
					curr = curr.Remove(curr.Length - 1);
				}
				break;
			}
		}
		int start = curr.Length;
		for (int j = start; j < ans.Length; j++)
			yield return DoInteractionClick(_keypadButtons[int.Parse(ans[j].ToString())]);
		yield return DoInteractionClick(_submitButton, 0);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("taxReturnsScript", "taxReturns");

	private readonly object _component;

	private readonly KMSelectable[] _keypadButtons;
	private readonly KMSelectable _toggleButton;
	private readonly KMSelectable _submitButton;
}
