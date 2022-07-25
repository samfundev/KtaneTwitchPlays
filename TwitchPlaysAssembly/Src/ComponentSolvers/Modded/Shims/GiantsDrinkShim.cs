using System;
using System.Collections;

public class GiantsDrinkShim : ComponentSolverShim
{
	public GiantsDrinkShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = new KMSelectable[] { _component.GetValue<KMSelectable>("btnLeft"), _component.GetValue<KMSelectable>("btnRight") };
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		int evenStrikes = _component.GetValue<int>("evenStrikes");
		int oddStrikes = _component.GetValue<int>("oddStrikes");
		if (Module.Bomb.Bomb.NumStrikes % 2 == 0)
			yield return DoInteractionClick(_buttons[evenStrikes]);
		else
			yield return DoInteractionClick(_buttons[oddStrikes]);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("giantsDrinkScript", "giantsDrink");

	private readonly object _component;
	private readonly KMSelectable[] _buttons;
}
