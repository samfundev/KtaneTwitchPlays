using System;
using System.Collections;
using UnityEngine;

public class AdditionComponentSolver : ComponentSolver
{
	public AdditionComponentSolver(TwitchModule module) :
		base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		numberSelectables = _component.GetValue<KMSelectable[]>("buttons");
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Submit or input a number using '!{0} <submit|input> <number>'. Cycle, clear, or submit by using '!{0} <cycle|clear|submit>'.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		var split = inputCommand.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if (split.Length == 2 && split[0] == "submit" || split[0] == "input" && split[1].RegexMatch("^[0-9]+$"))
		{
			yield return null;

			foreach (char c in split[1])
			{
				yield return DoInteractionClick(numberSelectables[c - '0']);
			}

			if (split[0] == "submit")
			{
				KMSelectable selectable = _component.GetValue<KMSelectable>("SubmitButton");
				yield return DoInteractionClick(selectable);
			}
		}
		else if (split.Length == 1 && split[0] == "cycle" || split[0] == "clear" || split[0] == "submit")
		{
			yield return null;

			bool cycle = split[0] == "cycle";
			bool clear = split[0] == "clear";
			int count = cycle ? 10 : 1;

			KMSelectable selectable = _component.GetValue<KMSelectable>(cycle ? "ScreenCycler" : clear ? "Clear" : "SubmitButton");

			for (int i = 0; i < count; i++)
			{
				yield return DoInteractionClick(selectable);
				yield return new WaitForSeconds(1.5f);
			}
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		string correctAnswer = _component.GetValue<long>("solution").ToString();

		yield return RespondToCommandInternal("clear");
		yield return RespondToCommandInternal($"submit {correctAnswer}");
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("AdditionScript");
	private readonly object _component;

	private readonly KMSelectable[] numberSelectables;
}
