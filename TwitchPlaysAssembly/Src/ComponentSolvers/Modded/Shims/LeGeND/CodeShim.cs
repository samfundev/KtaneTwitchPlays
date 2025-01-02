using System;
using System.Collections;

public class CodeShim : ComponentSolverShim
{
	public CodeShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<KMSelectable[]>("NumberButtons");
		_clear = _component.GetValue<KMSelectable>("ButtonR");
		_submit = _component.GetValue<KMSelectable>("ButtonS");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		string curr = _component.GetValue<int>("shownnum").ToString();
		if (curr.Equals("0"))
			curr = "";
		string ans = _component.GetValue<int>("solution").ToString();
		bool clrPress = false;
		if (curr.Length > ans.Length)
		{
			yield return DoInteractionClick(_clear);
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
					yield return DoInteractionClick(_clear);
					clrPress = true;
					break;
				}
			}
		}
		int start = 0;
		if (!clrPress)
			start = curr.Length;
		for (int j = start; j < ans.Length; j++)
			yield return DoInteractionClick(_buttons[int.Parse(ans[j].ToString())]);
		yield return DoInteractionClick(_submit, 0);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("TheCodeModule", "theCodeModule");

	private readonly object _component;
	private readonly KMSelectable[] _buttons;
	private readonly KMSelectable _clear;
	private readonly KMSelectable _submit;
}
