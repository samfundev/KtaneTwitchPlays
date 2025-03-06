using System;
using System.Collections;

[ModuleID("osu")]
public class OsuShim : ComponentSolverShim
{
	public OsuShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_change = _component.GetValue<KMSelectable>("canvas");
		_submit = _component.GetValue<KMSelectable>("textbox");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		int correctIndex = _component.GetValue<int>("random_mapinfo");
		while (correctIndex != _component.GetValue<int>("canvas_currentimagecounter"))
			yield return DoInteractionClick(_change);
		yield return DoInteractionClick(_submit, 0);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("Osu", "osu");

	private readonly object _component;
	private readonly KMSelectable _change;
	private readonly KMSelectable _submit;
}
