using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HangoverComponentSolver : ComponentSolver
{
	public HangoverComponentSolver(TwitchModule module) :
		base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		actionOptions = _component.GetValue<string[]>("actionOptions").ToList();
		actionText = _component.GetValue<TextMesh>("actionText");
		ingredientOptions = _component.GetValue<string[]>("ingredientOptions").ToList();
		ingredientText = _component.GetValue<TextMesh>("ingredientText");
		selectables = Module.BombComponent.GetComponent<KMSelectable>().Children;
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Toggle the note using !{0} note. Submit the elixir using !{0} submit. Add an ingredient or action using !{0} add <items>. An item name can be partial but must it be unique. Separate multiple items using semicolons.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] split = inputCommand.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		// The third condition makes it so the note command can only be used to toggle the note. It also does the same for submit, only allowing it to submit.
		if (split.Length == 1 && split[0].EqualsAny("note", "submit") && (split[0] == "note") == (_component.GetValue<int>("stage") == 0))
		{
			yield return null;
			yield return DoInteractionClick(selectables[6]);
		}
		else if (split.Length >= 2 && split[0] == "add")
		{
			List<string> validOptions = new List<string>();
			var options = actionOptions.Concat(ingredientOptions);

			foreach (string item in split.Skip(1).Join(" ").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(item => item.Trim()))
			{
				var matchingOptions = options.Where(option => option.ContainsIgnoreCase(item));
				switch (matchingOptions.Count())
				{
					case 0:
						yield return $"sendtochaterror!f There is no item called \"{item}\".";
						yield break;
					case 1:
						validOptions.Add(matchingOptions.First());
						break;
					default:
						string exactOption = matchingOptions.FirstOrDefault(option => option.ToLowerInvariant() == item);
						if (exactOption != null)
						{
							validOptions.Add(exactOption);
							break;
						}

						yield return $"sendtochaterror!f There are multiple items that match \"{item}\": {matchingOptions.Take(3).Join(", ")}.";
						yield break;
				}
			}

			yield return null;
			foreach (string option in validOptions)
			{
				if (ingredientOptions.Contains(option))
				{
					int difference = ingredientOptions.IndexOf(ingredientText.text) - ingredientOptions.IndexOf(option);
					for (int i = 0; i < Math.Abs(difference); i++)
					{
						yield return DoInteractionClick(selectables[difference < 0 ? 2 : 3], 0.05f);
					}

					yield return DoInteractionClick(selectables[0], 0.25f);
				}
				else
				{
					int difference = actionOptions.IndexOf(actionText.text) - actionOptions.IndexOf(option);
					for (int i = 0; i < Math.Abs(difference); i++)
					{
						yield return DoInteractionClick(selectables[difference < 0 ? 4 : 5], 0.05f);
					}

					yield return DoInteractionClick(selectables[1], 0.25f);
				}
			}
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		if (!_component.GetValue<bool>("correct"))
			yield break;

		yield return null;
		yield return RespondToCommandInternal($"add {_component.GetValue<List<string>>("correctIngredients").Except(_component.GetValue<List<string>>("ingredientsAdded")).Join(";")}");
		yield return RespondToCommandInternal("submit");
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("HangoverScript");
	private readonly object _component;

	private readonly List<string> actionOptions;
	private readonly TextMesh actionText;
	private readonly List<string> ingredientOptions;
	private readonly TextMesh ingredientText;
	private readonly KMSelectable[] selectables;
}
