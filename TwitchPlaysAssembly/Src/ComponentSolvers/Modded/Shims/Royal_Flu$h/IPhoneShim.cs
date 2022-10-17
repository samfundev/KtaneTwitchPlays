using System;
using System.Collections;
using System.Collections.Generic;

public class IPhoneShim : ComponentSolverShim
{
	public IPhoneShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		string[] numNames = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" };
		_subBtns = new KMSelectable[10];
		for (int i = 0; i < 10; i++)
			_subBtns[i] = _component.GetValue<KMSelectable>(numNames[i] + "SButton");
		_settings = _component.GetValue<KMSelectable>("settings");
		_home = _component.GetValue<KMSelectable>("home");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		string enteredPIN = _component.GetValue<string>("enteredPIN");
		string correctPIN = _component.GetValue<List<string>>("pinDigits").Join("");
		int stage = _component.GetValue<int>("settingsStage");
		for (int i = 0; i < stage - 1; i++)
		{
			if (correctPIN[i] != enteredPIN[i])
				yield break;
		}
		if (!_subBtns[0].gameObject.activeSelf)
		{
			if (!_settings.gameObject.activeSelf)
				yield return DoInteractionClick(_home);
			yield return DoInteractionClick(_settings);
		}
		for (int i = stage; i <= 4; i++)
			yield return DoInteractionClick(_subBtns[int.Parse(correctPIN[i - 1].ToString())]);
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("iPhoneScript", "iPhone");

	private readonly object _component;
	private readonly KMSelectable[] _subBtns;
	private readonly KMSelectable _settings;
	private readonly KMSelectable _home;
}