using System;
using System.Collections;
using System.Linq;
using UnityEngine;

[ModuleID("timezone")]
public class TimezoneShim : ComponentSolverShim
{
	public TimezoneShim(TwitchModule module)
		: base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<KMSelectable[]>("buttons");
		_submit = _component.GetValue<KMSelectable>("InputButton");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		string target = "";
		if (_component.GetValue<bool>("is12"))
		{
			int twelveH = ComponentType.CallMethod<int>("Format12h", _component, _component.GetValue<int>("toHour"));
			target = ComponentType.CallMethod<string>("FormatTwoDigits", _component, twelveH);
		}
		else
			target = ComponentType.CallMethod<string>("FormatTwoDigits", _component, _component.GetValue<int>("toHour"));
		target += ComponentType.CallMethod<string>("FormatTwoDigits", _component, _component.GetValue<int>("toMinutes"));
		string ans = _component.GetValue<TextMesh>("TextDisplay").text;
		int start = 0;
		if (ans.Select((x, a) => x == target[a]).All(x => x))
			start = 4;
		else if (ans[1] == target[0] && ans[2] == target[1] && ans[3] == target[2])
			start = 3;
		else if (ans[2] == target[0] && ans[3] == target[1])
			start = 2;
		else if (ans[3] == target[0])
			start = 1;
		for (int j = start; j < 4; j++)
			yield return DoInteractionClick(_buttons[int.Parse(target[j].ToString())]);
		yield return DoInteractionClick(_submit, 0);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("TimezoneScript", "timezones");

	private readonly object _component;

	private readonly KMSelectable[] _buttons;
	private readonly KMSelectable _submit;
}