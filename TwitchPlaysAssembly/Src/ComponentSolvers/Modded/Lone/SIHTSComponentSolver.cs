using System;
using System.Collections;

public class SIHTSComponentSolver : CommandComponentSolver
{
	public SIHTSComponentSolver(TwitchModule module) :
		base(module, "SIHTS", "!{0} underhand/flick [Set whether you will flick or underhand toss the coin] | !{0} increased/decreased/unchanged [Sets the coin acceleration]")
	{
	}

	private IEnumerator Underhand(CommandParser _)
	{
		_.Literal("underhand");

		yield return null;
		yield return Click(3);
	}

	private IEnumerator Flick(CommandParser _)
	{
		_.Literal("flick");

		yield return null;
		yield return Click(4);
	}
	private IEnumerator Unchanged(CommandParser _)
	{
		_.Literal("unchanged");

		yield return null;
		yield return Click(0);
	}

	private IEnumerator Increased(CommandParser _)
	{
		_.Literal("increased");

		yield return null;
		yield return Click(2);
	}

	private IEnumerator Decreased(CommandParser _)
	{
		_.Literal("decreased");

		yield return null;
		yield return Click(1);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		int tossType = _component.GetValue<int>("requiredTossType");
		double tossValue = _component.GetValue<double>("requiredToss");
		bool vegemite = _component.GetValue<bool>("vegemite");
		if (tossType == 0 && !_component.GetValue<bool>("UHPressed"))
			yield return Click(3);
		else if (tossType == 1 && !_component.GetValue<bool>("FPressed"))
			yield return Click(4);

		if (ComponentType.CallMethod<double>("floatToEV", _component, _component.GetValue<double>("UH_noIntRot") % 1.0) == tossValue || ComponentType.CallMethod<double>("floatToEV", _component, _component.GetValue<double>("F_noIntRot") % 1.0) == tossValue)
			yield return Click(0);
		else if (ComponentType.CallMethod<double>("floatToEV", _component, _component.GetValue<double>("UH_decAccRot") % 1.0) == tossValue || ComponentType.CallMethod<double>("floatToEV", _component, _component.GetValue<double>("F_decAccRot") % 1.0) == tossValue)
			yield return Click(vegemite ? 1 : 2);
		else
			yield return Click(vegemite ? 2 : 1);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("SIHTS");
}