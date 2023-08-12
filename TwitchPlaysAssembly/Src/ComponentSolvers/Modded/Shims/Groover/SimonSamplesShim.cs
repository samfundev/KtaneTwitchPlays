using System;
using System.Collections;
using System.Collections.Generic;

public class SimonSamplesShim : ComponentSolverShim
{
	public SimonSamplesShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_pads = _component.GetValue<KMSelectable[]>("Pads");
		_record = _component.GetValue<KMSelectable>("RecordButton");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		List<string> _responses = _component.GetValue<List<string>>("_expectedResponses");
		List<int> _padOrder = _component.GetValue<List<int>>("_pads");
		while (!_component.GetValue<bool>("_isSolved"))
		{
			while (_component.GetValue<bool>("_isPlaying")) yield return true;
			if (!_component.GetValue<bool>("_isRecording"))
				yield return DoInteractionClick(_record);
			yield return DoInteractionClick(_pads[_padOrder.IndexOf(int.Parse(_responses[_component.GetValue<int>("_currentStage")][_component.GetValue<int>("_cursor")].ToString()))], .5f);
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("SimonSamples", "simonSamples");

	private readonly object _component;

	private readonly KMSelectable[] _pads;
	private readonly KMSelectable _record;
}
