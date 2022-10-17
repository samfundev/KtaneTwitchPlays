using System;
using System.Collections;

public class LEDGridShim : ComponentSolverShim
{
	public LEDGridShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_btns = new KMSelectable[] { _component.GetValue<KMSelectable>("aButton"), _component.GetValue<KMSelectable>("bButton"), _component.GetValue<KMSelectable>("cButton"), _component.GetValue<KMSelectable>("dButton") };
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		char[] labels = { 'A', 'B', 'C', 'D' };
		string input = _component.GetValue<string>("input") ?? "";
		string answer = _component.GetValue<string>("correctAnswer");
		for (int i = 0; i < input.Length; i++)
		{
			if (input[i] != answer[i])
				yield break;
		}
		int start = input.Length;
		for (int i = start; i < answer.Length; i++)
			yield return DoInteractionClick(_btns[Array.IndexOf(labels, answer[i])]);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("ledGridScript", "ledGrid");

	private readonly object _component;

	private readonly KMSelectable[] _btns;
}