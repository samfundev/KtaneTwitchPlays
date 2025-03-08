using System;
using System.Collections;

[ModuleID("SIHTS")]
public class SIHTSComponentSolver : CommandComponentSolver
{
	public SIHTSComponentSolver(TwitchModule module) :
		base(module, "SIHTS", "!{0} underhand/flick [Set whether you will flick or underhand toss the coin] | !{0} increased/decreased/unchanged [Sets the coin acceleration]")
	{
	}

	[Command("underhand")]
	private IEnumerator Underhand()
	{
		yield return null;
		yield return Click(3);
	}

	[Command("flick")]
	private IEnumerator Flick()
	{
		yield return null;
		yield return Click(4);
	}

	[Command("unchanged")]
	private IEnumerator Unchanged()
	{
		yield return null;
		yield return Click(0);
	}

	[Command("increased")]
	private IEnumerator Increased()
	{
		yield return null;
		yield return Click(2);
	}

	[Command("decreased")]
	private IEnumerator Decreased()
	{
		yield return null;
		yield return Click(1);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;

		int tossType = _component.GetValue<int>("requiredTossType");
		int tossValue = _component.GetValue<int>("requiredToss");
		bool vegemite = _component.GetValue<bool>("vegemite");
		if (tossType == 0)
		{
			if (!_component.GetValue<bool>("UHPressed"))
				yield return Click(3);
			if (ComponentType.CallMethod<double>("floatToEV", _component, _component.GetValue<double>("UH_noIntRot") % 1.0) == tossValue)
				yield return Click(0);
			else if (ComponentType.CallMethod<double>("floatToEV", _component, _component.GetValue<double>("UH_decAccRot") % 1.0) == tossValue)
				yield return Click(vegemite ? 2 : 1);
			else
				yield return Click(vegemite ? 1 : 2);
		}
		else if (tossType == 1)
		{
			if (!_component.GetValue<bool>("FPressed"))
				yield return Click(4);
			if (ComponentType.CallMethod<double>("floatToEV", _component, _component.GetValue<double>("F_noIntRot") % 1.0) == tossValue)
				yield return Click(0);
			else if (ComponentType.CallMethod<double>("floatToEV", _component, _component.GetValue<double>("F_decAccRot") % 1.0) == tossValue)
				yield return Click(vegemite ? 2 : 1);
			else
				yield return Click(vegemite ? 1 : 2);
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("SIHTS");
}