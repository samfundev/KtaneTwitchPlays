using System;
using System.Collections;
using UnityEngine;

public class CubeShim : ComponentSolverShim
{
	public CubeShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_numberButtons = _component.GetValue<KMSelectable[]>("numberButtons");
		_executeButton = _component.GetValue<KMSelectable>("executeButton");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		bool[] pushed = _component.GetValue<bool[]>("buttonPushed");
		bool[] answer = _component.GetValue<bool[]>("correctButtons");
		if (_component.GetValue<bool>("executeLock") && _component.GetValue<bool>("generalButtonLock") && _component.GetValue<int>("buttonDistance") == 0)
		{
			for (int i = 0; i < 8; i++)
			{
				if (pushed[i] != answer[i])
				{
					((MonoBehaviour) _component).StopAllCoroutines();
					yield break;
				}
			}
		}
		for (int i = 0; i < 8; i++)
		{
			if (pushed[i] && !answer[i])
				yield break;
		}
		while (_component.GetValue<bool>("generalButtonLock") || _component.GetValue<bool>("executeLock") || !_component.GetValue<bool>("rotationComplete"))
			yield return true;
		int stage = _component.GetValue<int>("stage");
		for (int i = stage - 1; i < 8; i++)
		{
			if (i != stage - 1)
				answer = _component.GetValue<bool[]>("correctButtons");
			for (int j = 0; j < 8; j++)
			{
				if (answer[j] && !pushed[j])
				{
					yield return DoInteractionClick(_numberButtons[j], 0);
					while (_component.GetValue<bool>("generalButtonLock") || _component.GetValue<bool>("executeLock"))
						yield return null;
					yield return new WaitForSeconds(0.1f);
				}
			}
			yield return DoInteractionClick(_executeButton);
			if (i != 7)
			{
				while (_component.GetValue<bool>("generalButtonLock") || _component.GetValue<bool>("executeLock"))
					yield return true;
			}
		}
		while (!_component.GetValue<bool>("moduleSolved"))
			yield return true;
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("theCubeScript", "cube");

	private readonly object _component;
	private readonly KMSelectable[] _numberButtons;
	private readonly KMSelectable _executeButton;
}
