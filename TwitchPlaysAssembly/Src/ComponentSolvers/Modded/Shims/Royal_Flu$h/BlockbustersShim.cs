using System;
using System.Collections;

[ModuleID("blockbusters")]
public class BlockbustersShim : ComponentSolverShim
{
	public BlockbustersShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		pressAgain:
		object[] btns = _component.GetValue<object[]>("tiles");
		for (int i = 0; i < btns.Length; i++)
		{
			if (!btns[i].GetValue<bool>("tileTaken") && btns[i].GetValue<bool>("legalTile"))
			{
				yield return DoInteractionClick(btns[i].GetValue<KMSelectable>("selectable"));
				if (!_component.GetValue<bool>("moduleSolved"))
					goto pressAgain;
				else
					break;
			}
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("blockbustersScript", "blockbusters");

	private readonly object _component;
}
