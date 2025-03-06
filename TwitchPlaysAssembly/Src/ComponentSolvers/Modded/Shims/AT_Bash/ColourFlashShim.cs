using System;
using System.Collections;

[ModuleID("ColourFlash")]
[ModuleID("ColourFlashPL")]
public class ColourFlashShim : ReflectionComponentSolverShim
{
	public ColourFlashShim(TwitchModule module)
		: base(module, "ColourFlashModule" + (module.BombComponent.GetModuleID().Contains("PL") ? "PL" : string.Empty))
	{
		_yes = _component.GetValue<object>("ButtonYes").GetValue<KMSelectable>("KMSelectable");
		_no = _component.GetValue<object>("ButtonNo").GetValue<KMSelectable>("KMSelectable");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		var _ruleButtonPressHandler = _component.GetValue<Delegate>("_ruleButtonPressHandler");
		while (_ruleButtonPressHandler == null) yield return true;
		while (!(bool) _ruleButtonPressHandler.DynamicInvoke(true) && !(bool) _ruleButtonPressHandler.DynamicInvoke(false)) yield return true;
		if ((bool) _ruleButtonPressHandler.DynamicInvoke(true))
			yield return DoInteractionClick(_yes, 0);
		else
			yield return DoInteractionClick(_no, 0);
	}

	private readonly KMSelectable _yes;
	private readonly KMSelectable _no;
}