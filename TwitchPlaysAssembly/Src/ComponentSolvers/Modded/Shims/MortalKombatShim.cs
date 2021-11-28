using System;
using System.Collections;

public class MortalKombatShim : ComponentSolverShim
{
	public MortalKombatShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_btns = new KMSelectable[] { _component.GetValue<KMSelectable>("up"), _component.GetValue<KMSelectable>("down"), _component.GetValue<KMSelectable>("left"), _component.GetValue<KMSelectable>("right"), _component.GetValue<KMSelectable>("aButton"), _component.GetValue<KMSelectable>("bButton"), _component.GetValue<KMSelectable>("cButton") };
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		char[] moves = { '⇧', '⇩', '⇦', '⇨', 'A', 'B', 'C' };
		string input = _component.GetValue<string>("move") ?? "";
		int stage = _component.GetValue<int>("stage");
		string answer = GetStageAnswer(stage);
		for (int i = 0; i < input.Length; i++)
		{
			if (input[i] != answer[i])
				yield break;
		}
		for (int j = stage; j < 5; j++)
		{
			int start = input.Length;
			for (int i = start; i < answer.Length; i++)
				yield return DoInteractionClick(_btns[Array.IndexOf(moves, answer[i])]);
			if (j != 4)
			{
				answer = GetStageAnswer(j + 1);
				input = "";
			}
		}
	}

	string GetStageAnswer(int st)
	{
		switch (st)
		{
			case 1:
			case 2:
			case 3:
				return _component.GetValue<string>("attack" + st);
			default:
				return _component.GetValue<string>("fatality");
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("mortalKombatScript", "mortalKombat");

	private readonly object _component;

	private readonly KMSelectable[] _btns;
}