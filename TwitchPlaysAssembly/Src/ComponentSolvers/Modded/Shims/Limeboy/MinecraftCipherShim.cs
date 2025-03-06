using System;
using System.Collections;

[ModuleID("minecraftCipher")]
public class MinecraftCipherShim : ComponentSolverShim
{
	public MinecraftCipherShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_keypadButtons = _component.GetValue<KMSelectable[]>("Button");
		_clearButton = _component.GetValue<KMSelectable>("clear");
		_submitButton = _component.GetValue<KMSelectable>("submit");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		string curr = _component.GetValue<string>("input");
		string ans = _component.GetValue<string>("answer");
		if (curr.Length > ans.Length)
		{
			yield return DoInteractionClick(_clearButton, 0.125f);
			curr = "";
		}
		for (int i = 0; i < curr.Length; i++)
		{
			if (i == ans.Length)
				break;
			if (curr[i] != ans[i])
			{
				yield return DoInteractionClick(_clearButton, 0.125f);
				curr = "";
				break;
			}
		}
		char[] alphabet = _component.GetValue<char[]>("alphabets_exist");
		int start = curr.Length;
		for (int j = start; j < ans.Length; j++)
			yield return DoInteractionClick(_keypadButtons[Array.IndexOf(alphabet, ans[j])], 0.125f);
		yield return DoInteractionClick(_submitButton, 0);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("MinecraftCipher", "minecraftCipher");

	private readonly object _component;

	private readonly KMSelectable[] _keypadButtons;
	private readonly KMSelectable _clearButton;
	private readonly KMSelectable _submitButton;
}
