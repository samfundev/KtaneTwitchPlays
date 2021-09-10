using System;
using System.Collections;

public class PeriodicTableShim : ComponentSolverShim
{
	public PeriodicTableShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<KMSelectable[]>("buttons");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		yield return DoInteractionClick(_buttons[_component.GetValue<int>("solutionButtonModII")]);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("periodicTableScript", "periodicTable");

	private readonly object _component;

	private readonly KMSelectable[] _buttons;
}
