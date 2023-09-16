using System;
using UnityEngine;

public class ChipShim : ComponentSolverShim
{
	public ChipShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		if (TwitchPlaySettings.data.DisableCopyrightChipMusic)
			module.BombComponent.GetComponent(ComponentType).gameObject.GetComponentInChildren<AudioSource>().mute = true;
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("chip", "chip");
}
