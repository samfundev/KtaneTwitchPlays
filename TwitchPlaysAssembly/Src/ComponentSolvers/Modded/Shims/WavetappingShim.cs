using System;
using System.Collections;

public class WavetappingShim : ComponentSolverShim
{
	public WavetappingShim(TwitchModule module)
		: base(module)
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<KMSelectable[]>("ModuleButtons");
		_submit = _component.GetValue<KMSelectable>("SubmitButton");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		string[] correctPatterns = _component.GetValue<string[]>("correctPatterns");
		int start = _component.GetValue<int>("nowStage");
		for (int i = start; i < 3; i++)
		{
			while (_component.GetValue<bool>("beatStage")) { yield return true; }
			string nowPattern = _component.GetValue<string>("nowPattern");
			for (int j = 0; j < correctPatterns[i].Length; j++)
			{
				if (correctPatterns[i][j] != nowPattern[j])
				{
					yield return DoInteractionClick(_buttons[j]);
				}
			}
			yield return DoInteractionClick(_submit, 0);
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("scr_wavetapping", "Wavetapping");

	private readonly object _component;

	private readonly KMSelectable[] _buttons;
	private readonly KMSelectable _submit;
}
