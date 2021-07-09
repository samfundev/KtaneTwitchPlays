using System;
using System.Collections;

public class MastermindShim : ComponentSolverShim
{
	public MastermindShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
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

	private static readonly Type ComponentType = ReflectionHelper.FindType("Mastermind", "Mastermind Simple");

	private readonly object _component;
	private readonly KMSelectable[] _slots;
	private readonly KMSelectable _submit;
}
