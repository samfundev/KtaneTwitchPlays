using System;
using System.Collections;
using UnityEngine;

public class BinaryLEDsShim : ComponentSolverShim
{
	public BinaryLEDsShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		while (!_component.GetValue<bool>("isActivated")) yield return true;
		object[] wireInfo = _component.GetValue<object[]>("TwitchPlayWires");
		int[] wireColors = new int[3];
		for (int i = 0; i < 3; i++)
			wireColors[i] = wireInfo[i].GetValue<int>("color");
		int[,] solutions = _component.GetValue<int[,]>("solutions");
		int seqIndex = _component.GetValue<int>("sequenceIndex");
		while (true)
		{
			int timeIndex = ComponentType.CallMethod<int>("GetIndexFromTime", _component, Time.time, _component.GetValue<float>("blinkDelay"));
			for (int i = 0; i < 3; i++)
			{
				if (solutions[seqIndex, wireColors[i]] == timeIndex && !wireInfo[i].GetValue<bool>("isCut"))
				{
					yield return DoInteractionClick(wireInfo[i].GetValue<KMSelectable>("wire"), 0);
					yield break;
				}
			}
			yield return true;
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("BinaryLeds", "BinaryLEDs");

	private readonly object _component;
}
