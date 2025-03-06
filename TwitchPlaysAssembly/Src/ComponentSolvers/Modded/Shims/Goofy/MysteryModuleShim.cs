using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ModuleID("mysterymodule")]
public class MysteryModuleShim : ReflectionComponentSolverShim
{
	public static readonly Dictionary<BombComponent, GameObject> CoveredModules = new Dictionary<BombComponent, GameObject>();

	public MysteryModuleShim(TwitchModule module)
		: base(module, "MysteryModuleScript")
	{
		module.StartCoroutine(WaitForMysteryModule());
	}

	public static bool IsHidden(BombComponent bombComponent) => CoveredModules.TryGetValue(bombComponent, out GameObject cover) && cover != null;

	IEnumerator WaitForMysteryModule()
	{
		KMBombModule mystified;
		do
		{
			mystified = _component.GetValue<KMBombModule>("mystifiedModule");
			yield return null;
		} while (mystified == null);

		if (_component.GetValue<bool>("failsolve"))
			yield break;

		CoveredModules[mystified.GetComponent<BombComponent>()] = _component.GetValue<GameObject>("Cover");
	}
}
