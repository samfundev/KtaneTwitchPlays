using System;
using System.Collections;

[ModuleID("jackOLantern")]
public class JackOLanternShim : ComponentSolverShim
{
	public JackOLanternShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		yield return DoInteractionClick(_component.GetValue<KMSelectable>("correctButton"));
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("jackOLanternScript", "jackOLantern");

	private readonly object _component;
}
