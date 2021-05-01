using System;
using System.Collections;
using System.Linq;

public class AlphabetNumbersShim : ComponentSolverShim
{
	public AlphabetNumbersShim(TwitchModule module)
		: base(module, "alphabetNumbers")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		IEnumerator command = RespondToCommandUnshimmed(inputCommand);
		while (command.MoveNext())
			yield return command.Current;
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		int stage = _component.GetValue<int>("stage");
		int presses = _component.GetValue<int>("numberOfPresses");
		object[] buttons = _component.GetValue<object[]>("buttons").OrderBy(o => o.GetValue<string>("containedNumber")).ToArray();
		bool falseDec = false;
		for (int i = 0; i < 6; i++)
		{
			if (!buttons[i].GetValue<bool>("pressed") && !falseDec)
				falseDec = true;
			else if (buttons[i].GetValue<bool>("pressed") && falseDec)
				yield break;
		}
		for (int i = stage; i < 4; i++)
		{
			if (i != stage)
			{
				buttons = _component.GetValue<object[]>("buttons").OrderBy(o => o.GetValue<string>("containedNumber")).ToArray();
				presses = 0;
			}
			for (int j = presses; j < 6; j++)
				yield return DoInteractionClick(buttons[j].GetValue<KMSelectable>("selectable"));
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("alphabeticalOrderScript", "alphabetNumbers");

	private readonly object _component;
}
