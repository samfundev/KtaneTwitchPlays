using System;
using System.Collections;
using System.Collections.Generic;

public class IdentityParadeShim : ComponentSolverShim
{
	public IdentityParadeShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_submit = _component.GetValue<KMSelectable>("convictBut");
	}

	IEnumerator SelectOption(string type)
	{
		List<string> choices = _component.GetValue<List<string>>(type + "Entries");
		string answer = _component.GetValue<string>(type + "Answer");
		int current = _component.GetValue<int>(type + "Index");
		var right = _component.GetValue<KMSelectable>(type + "Right");
		var left = _component.GetValue<KMSelectable>(type + "Left");
		return SelectIndex(current, choices.IndexOf(answer), choices.Count, right, left);
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;
		yield return SelectOption("hair");
		yield return SelectOption("build");
		yield return SelectOption("attire");
		yield return SelectOption("suspect");
		yield return DoInteractionClick(_submit, 0);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("identityParadeScript", "identityParade");

	private readonly object _component;
	private readonly KMSelectable _submit;
}