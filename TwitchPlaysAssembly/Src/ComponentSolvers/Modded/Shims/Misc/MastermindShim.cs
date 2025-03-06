using System.Collections;

[ModuleID("Mastermind Simple")]
[ModuleID("Mastermind Cruel")]
public class MastermindShim : ComponentSolverShim
{
	public MastermindShim(TwitchModule module)
		: base(module)
	{
		var type = ReflectionHelper.FindType("Mastermind", module.BombComponent.GetModuleID());
		_component = module.BombComponent.GetComponent(type);
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

	private readonly object _component;
	private readonly KMSelectable[] _slots;
	private readonly KMSelectable _submit;
}
