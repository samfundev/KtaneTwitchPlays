using System;
using System.Collections;
using System.Collections.Generic;

public class HeraldryShim : ComponentSolverShim
{
	public HeraldryShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_pageTurn = _component.GetValue<KMSelectable[]>("pageTurners");
		_crests = _component.GetValue<KMSelectable[]>("crestSelectors");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		List<int> order = _component.GetValue<List<int>>("order");
		reCalc:
		int curSolves = Module.Bomb.Bomb.GetSolvedComponentCount();
		int sol = 5;
		if (_component.GetValue<bool>("unicorn"))
			sol = 1;
		else if (curSolves % 4 == 0)
			sol = 2;
		else if (curSolves % 4 == 1)
			sol = 3;
		else if (curSolves % 4 == 2)
			sol = 4;
		while (_component.GetValue<int>("currentCrest") + 1 < order[sol])
		{
			while (_component.GetValue<int>("animating") < 0) yield return sol == 1 ? true : (object) null;
			if (curSolves != Module.Bomb.Bomb.GetSolvedComponentCount() && sol != 1)
				goto reCalc;
			yield return DoInteractionClick(_pageTurn[1]);
		}
		while (_component.GetValue<int>("currentCrest") > order[sol])
		{
			while (_component.GetValue<int>("animating") > 0) yield return sol == 1 ? true : (object) null;
			if (curSolves != Module.Bomb.Bomb.GetSolvedComponentCount() && sol != 1)
				goto reCalc;
			yield return DoInteractionClick(_pageTurn[0]);
		}
		while (_component.GetValue<int>("animating") != 0) yield return sol == 1 ? true : (object) null;
		if (curSolves != Module.Bomb.Bomb.GetSolvedComponentCount() && sol != 1)
			goto reCalc;
		yield return DoInteractionClick(_crests[order[sol] % 2], 0);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("Heraldry", "heraldry");

	private readonly object _component;
	private readonly KMSelectable[] _pageTurn;
	private readonly KMSelectable[] _crests;
}
