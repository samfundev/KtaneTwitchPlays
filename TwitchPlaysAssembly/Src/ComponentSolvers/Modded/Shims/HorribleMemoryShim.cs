using System;
using System.Collections;

public class HorribleMemoryShim : ComponentSolverShim
{
	public HorribleMemoryShim(TwitchModule module)
		: base(module, "horribleMemory")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = _component.GetValue<object[]>("buttons");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		while (_component.GetValue<bool>("striking"))
			yield return true;
		int stage = _component.GetValue<int>("stage");
		for (int i = stage - 1; i < 5; i++)
		{
			int correctPosition = _component.GetValue<int>("correctPosition");
			int correctLabel = _component.GetValue<int>("correctLabel");
			string correctColour = _component.GetValue<string>("correctColour");
			if (correctPosition != 0)
				yield return DoInteractionClick(_buttons[correctPosition - 1].GetValue<KMSelectable>("selectable"));
			else if (correctLabel != 0)
			{
				for (int j = 0; j < 6; j++)
				{
					if (_buttons[j].GetValue<int>("labelName") == correctLabel)
					{
						yield return DoInteractionClick(_buttons[j].GetValue<KMSelectable>("selectable"));
						break;
					}
				}
			}
			else
			{
				for (int j = 0; j < 6; j++)
				{
					if (_buttons[j].GetValue<string>("colourName") == correctColour)
					{
						yield return DoInteractionClick(_buttons[j].GetValue<KMSelectable>("selectable"));
						break;
					}
				}
			}
			if (i != 4)
			{
				while (_component.GetValue<bool>("striking"))
					yield return true;
			}
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("cruelMemoryScript", "horribleMemory");

	private readonly object _component;
	private readonly object[] _buttons;
}
