using System;
using System.Collections;
using System.Collections.Generic;

public class MastermindShim : ComponentSolverShim
{
	public MastermindShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType[GetModuleType()]);
		_slots = _component.GetValue<KMSelectable[]>("Slot");
		_submit = _component.GetValue<KMSelectable>("Submit");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		int[] currSlots = _component.GetValue<int[]>("SlotNR");
		int[] corrSlots = _component.GetValue<int[]>("corrSlot");
		for (int i = 0; i < 5; i++)
		{
			while (currSlots[i] != corrSlots[i])
				yield return DoInteractionClick(_slots[i]);
		}
		yield return DoInteractionClick(_submit, 0);
	}

	private static readonly string simple = "Mastermind Simple", cruel = "Mastermind Cruel";
	private static readonly Dictionary<string, Type> ComponentType = new Dictionary<string, Type> {
		{ simple, ReflectionHelper.FindType("Mastermind", simple) },
		{ cruel, ReflectionHelper.FindType("Mastermind", cruel) }
	};

	private readonly object _component;
	private readonly KMSelectable[] _slots;
	private readonly KMSelectable _submit;
}
