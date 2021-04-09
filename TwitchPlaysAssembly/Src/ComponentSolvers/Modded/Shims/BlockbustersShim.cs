using System;
using System.Collections;

public class BlockbustersShim : ComponentSolverShim
{
	public BlockbustersShim(TwitchModule module)
		: base(module, "blockbusters")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		yield return RespondToCommandUnshimmed(inputCommand);
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
