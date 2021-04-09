using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WireSpaghettiShim : ComponentSolverShim
{
	public WireSpaghettiShim(TwitchModule module)
		: base(module, "wireSpaghetti")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_wires = _component.GetValue<KMSelectable[]>("wireSelectables");
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		yield return RespondToCommandUnshimmed(inputCommand);
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		List<string> wireOrder = _component.GetValue<List<string>>("orderOfActiveWires");
		string[] wireColours = _component.GetValue<string[]>("wireColourName");
		GameObject[] cutWiresObj = _component.GetValue<GameObject[]>("cutWires");
		bool[] cutWires = new bool[cutWiresObj.Length];
		for (int i = 0; i < cutWiresObj.Length; i++)
		{
			if (cutWiresObj[i].activeSelf)
				cutWires[i] = true;
		}
		for (int i = 0; i < wireOrder.Count; i++)
		{
			for (int j = 0; j < wireColours.Length; j++)
			{
				if (wireOrder[i] == wireColours[j] && !cutWires[j])
				{
					yield return DoInteractionClick(_wires[j]);
					cutWires[j] = true;
					break;
				}
			}
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("messyWiresScript", "wireSpaghetti");

	private readonly object _component;
	private readonly KMSelectable[] _wires;
}
