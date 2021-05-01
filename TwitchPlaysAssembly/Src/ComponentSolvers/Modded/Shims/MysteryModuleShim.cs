using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MysteryModuleShim : ComponentSolverShim
{
	public static readonly Dictionary<BombComponent, GameObject> CoveredModules = new Dictionary<BombComponent, GameObject>();

	public MysteryModuleShim(TwitchModule module)
		: base(module, "mysterymodule")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());

		module.StartCoroutine(WaitForMysteryModule());
	}

	public static bool IsHidden(BombComponent bombComponent) => CoveredModules.TryGetValue(bombComponent, out GameObject cover) && cover != null;

	IEnumerator WaitForMysteryModule()
	{
		var component = Module.BombComponent.GetComponent("MysteryModuleScript");

		KMBombModule mystified;
		do
		{
			mystified = component.GetValue<KMBombModule>("mystifiedModule");
			yield return null;
		} while (mystified == null);

		if (component.GetValue<bool>("failsolve"))
			yield break;

		CoveredModules[mystified.GetComponent<BombComponent>()] = component.GetValue<GameObject>("Cover");
	}
}
