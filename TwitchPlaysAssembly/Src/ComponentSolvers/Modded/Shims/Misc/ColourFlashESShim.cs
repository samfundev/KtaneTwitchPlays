using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

[ModuleID("ColourFlashES")]
public class ColourFlashESShim : ComponentSolverShim
{
	public ColourFlashESShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_yes = _component.GetValue<object>("ButtonYes").GetValue<KMSelectable>("KMSelectable");
		_no = _component.GetValue<object>("ButtonNo").GetValue<KMSelectable>("KMSelectable");
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		Match modulesMatch = Regex.Match(inputCommand, "^press (yes|s�|y|s) ([1-8]|any|cualq)$", RegexOptions.IgnoreCase);
		if (modulesMatch.Success)
		{
			string position = modulesMatch.Groups[2].Value;
			if (int.TryParse(position, out int positionIndex))
			{
				yield return null;
				positionIndex--;
				while (positionIndex != _component.GetValue<int>("_currentColourSequenceIndex"))
					yield return new WaitForSeconds(0.1f);

				_yes.OnInteract();
			}
			else if (position.ToLowerInvariant().EqualsAny("any", "cualq"))
			{
				yield return null;
				_yes.OnInteract();
			}
		}
		else
		{
			inputCommand = inputCommand.ToLowerInvariant().Replace("any", "cualq");
			yield return RespondToCommandUnshimmed(inputCommand);
		}
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

	private static readonly Type ComponentType = ReflectionHelper.FindType("ColourFlashModuleES");

	private readonly object _component;
	private readonly KMSelectable _yes;
	private readonly KMSelectable _no;
}