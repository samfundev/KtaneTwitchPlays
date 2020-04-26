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

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		IEnumerator command = RespondToCommandUnshimmed(inputCommand);
		while (command.MoveNext())
			yield return command.Current;
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

		CoveredModules[mystified.GetComponent<BombComponent>()] = component.GetValue<GameObject>("Cover");
	}
}
