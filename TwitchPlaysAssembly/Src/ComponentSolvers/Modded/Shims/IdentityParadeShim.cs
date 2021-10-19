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
		_hairLeft = _component.GetValue<KMSelectable>("hairLeft");
		_hairRight = _component.GetValue<KMSelectable>("hairRight");
		_buildLeft = _component.GetValue<KMSelectable>("buildLeft");
		_buildRight = _component.GetValue<KMSelectable>("buildRight");
		_attireLeft = _component.GetValue<KMSelectable>("attireLeft");
		_attireRight = _component.GetValue<KMSelectable>("attireRight");
		_suspectLeft = _component.GetValue<KMSelectable>("suspectLeft");
		_suspectRight = _component.GetValue<KMSelectable>("suspectRight");
		_submit = _component.GetValue<KMSelectable>("convictBut");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		List<string> hairChoices = _component.GetValue<List<string>>("hairEntries");
		List<string> buildChoices = _component.GetValue<List<string>>("buildEntries");
		List<string> attireChoices = _component.GetValue<List<string>>("attireEntries");
		List<string> suspectChoices = _component.GetValue<List<string>>("suspectEntries");
		string ansHair = _component.GetValue<string>("hairAnswer");
		string ansBuild = _component.GetValue<string>("buildAnswer");
		string ansAttire = _component.GetValue<string>("attireAnswer");
		string ansSuspect = _component.GetValue<string>("suspectAnswer");
		int curHair = _component.GetValue<int>("hairIndex");
		int curBuild = _component.GetValue<int>("buildIndex");
		int curAttire = _component.GetValue<int>("attireIndex");
		int curSuspect = _component.GetValue<int>("suspectIndex");
		yield return SelectIndex(curHair, hairChoices.IndexOf(ansHair), hairChoices.Count, _hairRight, _hairLeft);
		yield return SelectIndex(curBuild, buildChoices.IndexOf(ansBuild), buildChoices.Count, _buildRight, _buildLeft);
		yield return SelectIndex(curAttire, attireChoices.IndexOf(ansAttire), attireChoices.Count, _attireRight, _attireLeft);
		yield return SelectIndex(curSuspect, suspectChoices.IndexOf(ansSuspect), suspectChoices.Count, _suspectRight, _suspectLeft);
		yield return DoInteractionClick(_submit, 0);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("identityParadeScript", "identityParade");

	private readonly object _component;
	private readonly KMSelectable _hairLeft;
	private readonly KMSelectable _hairRight;
	private readonly KMSelectable _buildLeft;
	private readonly KMSelectable _buildRight;
	private readonly KMSelectable _attireLeft;
	private readonly KMSelectable _attireRight;
	private readonly KMSelectable _suspectLeft;
	private readonly KMSelectable _suspectRight;
	private readonly KMSelectable _submit;
}