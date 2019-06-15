using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class CoffeebucksComponentSolver : ComponentSolver
{
	public CoffeebucksComponentSolver(TwitchModule module) :
		base(module)
	{
		_component = Module.BombComponent.GetComponent(ComponentType);
		selectables = Module.BombComponent.GetComponent<KMSelectable>().Children;
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "!{0} next [next customer] | !{0} name <name> [select name] | !{0} name <name> (pause) [pause on the name for (pause) seconds, then select] | !{0} sugar/time/stress/size/name [repeat info] | !{0} milk/cream/sprinkles/gluten [toggle a quirk] | !{0} submit <coffee> [partial names are fine]");
	}

	readonly Dictionary<string, int> buttonMap = new Dictionary<string, int>()
	{
		{ "sugar", 1 },
		{ "time", 2 },
		{ "stress", 3 },
		{ "size", 4 },
		{ "name", 15 },
		{ "milk", 11 },
		{ "cream", 12 },
		{ "sprinkles", 13 },
		{ "gluten", 14 },
	};

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = Regex.Replace(inputCommand.ToLowerInvariant().Trim(), "^(press|hit|enter|push) ", "");

		string[] split = inputCommand.SplitFull(' ');

		if (split.Length == 1 && split[0] == "next")
		{
			if (!selectables[0].gameObject.activeInHierarchy)
			{
				yield return "sendtochaterror There is already a customer.";
				yield break;
			}

			yield return null;
			yield return DoInteractionClick(selectables[0]);
			yield break;
		}

		float pause = -1;
		if (split.Length == 1 && buttonMap.TryGetValue(split[0], out int buttonIndex))
		{
			if (selectables[0].gameObject.activeInHierarchy)
			{
				yield return "sendtochaterror No customer is active.";
				yield break;
			}

			yield return null;
			yield return DoInteractionClick(selectables[buttonIndex]);
		}
		else if (split[0] == "name" && (split.Length == 2 || split.Length == 3 && float.TryParse(split[2], out pause)))
		{
			if (selectables[0].gameObject.activeInHierarchy)
			{
				yield return "sendtochaterror No customer is active.";
				yield break;
			}

			if (!selectables[5].gameObject.activeInHierarchy)
			{
				yield return "sendtochaterror You already submitted a name.";
				yield break;
			}

			string[] nameOptions = _component.GetValue<string[]>("nameOptions");
			int nameIndex = nameOptions.IndexOf(option => option.EqualsIgnoreCase(split[1]));
			if (nameIndex != -1)
			{
				yield return null;
				int diff = _component.GetValue<int>("startName") - nameIndex;
				for (int i = 0; i < Math.Abs(diff); i++)
				{
					yield return DoInteractionClick(selectables[diff > 0 ? 6 : 7]);
				}

				yield return new WaitForSecondsWithCancel(pause >= 0 ? pause : 2, true, this);
				yield return DoInteractionClick(selectables[5]);
			}
		}
		else if (split.Length >= 2 && split[0] == "submit")
		{
			if (selectables[0].gameObject.activeInHierarchy)
			{
				yield return "sendtochaterror No customer is active.";
				yield break;
			}

			if (!selectables[8].gameObject.activeInHierarchy)
			{
				yield return "sendtochaterror You haven't submitted a name.";
				yield break;
			}

			string[] coffeeOptions = _component.GetValue<string[]>("coffeeOptions");
			string target = split.Skip(1).Join();
			var matchingOptions = coffeeOptions.Where(option => option.ContainsIgnoreCase(target));

			switch (matchingOptions.Count())
			{
				case 0:
					yield return $"sendtochaterror None of the coffees match \"{target}\"";
					yield break;
				case 1:
					break;
				default:
					yield return $"sendtochaterror Multiple coffees match \"{target}\": {matchingOptions.Take(3).Join(", ")}";
					yield break;
			}

			yield return null;
			int coffeeIndex = Array.IndexOf(coffeeOptions, matchingOptions.First());
			int diff = _component.GetValue<int>("startCoffee") - coffeeIndex;
			for (int i = 0; i < Math.Abs(diff); i++)
			{
				yield return DoInteractionClick(selectables[diff > 0 ? 9 : 10]);
			}

			yield return DoInteractionClick(selectables[8]);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		while (!_component.GetValue<bool>("moduleSolved"))
		{
			// Get a customer.
			if (selectables[0].gameObject.activeInHierarchy) yield return RespondToCommandInternal("next");

			// Enter the customer's name.
			if (selectables[5].gameObject.activeInHierarchy) yield return RespondToCommandInternal($"name {_component.GetValue<string>("customerName")} 0");

			// Toggle the correct quirk.
			bool[] quirkStatus = _component.GetValue<bool[]>("quirkStatus");
			int customerQuirk = _component.GetValue<int>("customerQuirk");

			for (int i = 0; i < 4; i++)
			{
				if (quirkStatus[i] != (customerQuirk - 1 == i)) yield return RespondToCommandInternal(new[] { "milk", "cream", "sprinkles", "gluten" }[i]);
			}

			// Submit any correct drink.
			yield return RespondToCommandInternal($"submit {_component.GetValue<List<string>>("legalCoffees").First()}");
		}
	}

	static CoffeebucksComponentSolver()
	{
		ComponentType = ReflectionHelper.FindType("coffeebucksScript");
	}

	private static readonly Type ComponentType;
	private readonly object _component;

	private readonly KMSelectable[] selectables;
}
