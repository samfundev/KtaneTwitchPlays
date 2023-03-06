using System;
using System.Collections;
using UnityEngine;

public class NecronomiconComponentSolver : ComponentSolver
{
	public NecronomiconComponentSolver(TwitchModule module) :
		base(module)
	{
		_component = Module.BombComponent.GetComponent(ComponentType);
		selectables = Module.BombComponent.GetComponent<KMSelectable>().Children;
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Cycle all the pages using !{0} cycle or !{0} fastcycle. Submit a specific page using !{0} page 3.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.ToLowerInvariant().Trim();
		string[] split = inputCommand.SplitFull(' ', ',', ';');

		if (split.Length == 1 && split[0].EqualsAny("cycle", "c", "pages", "fastcycle", "fc"))
		{
			yield return null;

			yield return DoInteractionClick(selectables[0]);

			bool fast = split[0].EqualsAny("fastcycle", "fc");
			int pagesTurned = 0;
			for (int i = 0; i < 8; i++)
			{
				yield return new WaitUntil(() => !_component.GetValue<bool>("animating"));
				yield return new WaitForSecondsWithCancel(fast ? 1.25f : 2.25f, false, this);
				if (CoroutineCanceller.ShouldCancel)
					break;

				pagesTurned = i + 1;
				yield return DoInteractionClick(selectables[1]);
			}

			if (CoroutineCanceller.ShouldCancel)
			{
				for (int i = pagesTurned; i < 8; i++)
				{
					yield return new WaitUntil(() => !_component.GetValue<bool>("animating"));
					yield return DoInteractionClick(selectables[1]);
				}
			}
		}
		else if (split.Length == 2 && split[0].EqualsAny("page", "p") && int.TryParse(split[1], out int pageNumber) && pageNumber.InRange(1, 8))
		{
			yield return null;

			yield return DoInteractionClick(selectables[0]);

			int pagesTurned = 0;
			for (int i = 1; i < pageNumber; i++)
			{
				yield return new WaitUntil(() => !_component.GetValue<bool>("animating"));
				if (CoroutineCanceller.ShouldCancel)
					break;

				pagesTurned = i;
				yield return DoInteractionClick(selectables[1]);
			}

			if (CoroutineCanceller.ShouldCancel)
			{
				for (int i = pagesTurned; i < 8; i++)
				{
					yield return new WaitUntil(() => !_component.GetValue<bool>("animating"));
					yield return DoInteractionClick(selectables[1]);
				}
			}

			yield return "solve";
			yield return "strike";
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		yield return RespondToCommandInternal("page " + _component.GetValue<int>("correctPage"));
		while (_component.GetValue<bool>("isOpen"))
			yield return true;
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("necronomiconScript");

	private readonly object _component;
	private readonly KMSelectable[] selectables;
}
