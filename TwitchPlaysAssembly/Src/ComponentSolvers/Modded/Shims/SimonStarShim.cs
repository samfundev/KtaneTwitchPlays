using System;
using System.Collections;

public class SimonStarShim : ComponentSolverShim
{
	public SimonStarShim(TwitchModule module)
		: base(module, "simonsStar")
	{
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		_component = module.BombComponent.GetComponent(ComponentType);
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		yield return RespondToCommandUnshimmed(inputCommand);
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		KMSelectable[] btns = new KMSelectable[5];
		string[] pos = { "first", "second", "third", "fourth", "fifth" };
		for (int i = 0; i < 5; i++)
			btns[i] = _component.GetValue<object>(pos[i]+"CorrectButton").GetValue<KMSelectable>("selectable");
		while (_component.GetValue<bool>("striking"))
			yield return true;
		int stage = _component.GetValue<int>("stage");
		int sub = _component.GetValue<int>("substage");
		for (int i = stage; i < 5; i++)
		{
			if (i != stage)
				sub = 0;
			for (int j = sub; j < i + 1; j++)
				yield return DoInteractionClick(btns[j], .4f);
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("simonsStarScript", "simonsStar");

	private readonly object _component;
}
