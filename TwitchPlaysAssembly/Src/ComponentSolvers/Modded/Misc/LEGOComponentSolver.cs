using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class LEGOComponentSolver : ComponentSolver
{
	public LEGOComponentSolver(TwitchModule module) :
		base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		GridButtons = _component.GetValue<KMSelectable[]>("GridButtons");
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Commands: select <column><row>, color <index>, left (times), right (times), clear and submit. The first two commands can be shortened to their first letter. Color indexes are specified in english reading order. You can also select the grid relative to your last position using a string of udlr characters. Commands are chainable using semicolons.");
	}

	int CharacterToIndex(char character) => character >= 'a' ? character - 'a' : character - '1';
	bool FirstOrWhole(string value, string match) => value[0] == match[0] || value == match;

	Vector2Int SelectedPosition = Vector2Int.zero;
	readonly Dictionary<char, Vector2Int> CharacterToDirection = new Dictionary<char, Vector2Int>
	{
		{ 'u', new Vector2Int( 0, -1) },
		{ 'd', new Vector2Int( 0,  1) },
		{ 'l', new Vector2Int(-1,  0) },
		{ 'r', new Vector2Int( 1,  0) },
	};

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = Regex.Replace(inputCommand.ToLowerInvariant().Trim(), "^(press|hit|enter|push)", "");
		string[] split = inputCommand.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

		List<object> toYield = new List<object>();
		foreach (string command in split)
		{
			string[] commandSplit = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			toYield.Add(((Func<object>) (() =>
			{
				int count = 1;
				if ((commandSplit[0] == "left" || commandSplit[0] == "right") && (commandSplit.Length == 1 || commandSplit.Length == 2 && int.TryParse(commandSplit[1], out count)))
				{
					if (!count.InRange(1, 8)) return false;

					bool leftButton = commandSplit[0] == "left";
					return ClickButton(_component.GetValue<KMSelectable>($"{(leftButton ? "Left" : "Right")}Button"), count);
				}
				else if (commandSplit.Length == 1)
				{
					if (command == "submit")
					{
						return _component.GetValue<KMSelectable>("SubmitButton");
					}
					else if (command == "clear")
					{
						return ClearGrid();
					}
					else if (command.RegexMatch("^[lrud]+$"))
					{
						return command.Select(character => MoveSelected(CharacterToDirection[character], false)).GetEnumerator();
					}
				}
				else if (commandSplit.Length == 2)
				{
					if (FirstOrWhole(commandSplit[0], "select") && commandSplit[1].Length == 2)
					{
						int column = CharacterToIndex(commandSplit[1][0]);
						int row = CharacterToIndex(commandSplit[1][1]);

						if (column.InRange(0, 7) && row.InRange(0, 7))
						{
							return MoveSelected(new Vector2Int(column, row));
						}
					}
					else if (FirstOrWhole(commandSplit[0], "color") && int.TryParse(commandSplit[1], out int colorPosition))
					{
						return _component.GetValue<KMSelectable[]>("ColorButtons")[colorPosition - 1];
					}
				}

				return false;
			}))());

			if (toYield.Contains(false)) yield break;
		}

		yield return null;
		foreach (object yieldObject in toYield) 
			yield return (yieldObject is KMSelectable selectable) ? DoInteractionClick(selectable) : yieldObject;
	}

	IEnumerator MoveSelected(Vector2Int position, bool absolute = true)
	{
		yield return null;
		SelectedPosition = absolute ? position : SelectedPosition + position;
		// If we go out of bounds, wrap.
		SelectedPosition.x = (SelectedPosition.x % 8 + 8) % 8;
		SelectedPosition.y = (SelectedPosition.y % 8 + 8) % 8;

		yield return DoInteractionClick(GridButtons[(7 - SelectedPosition.y) * 8 + SelectedPosition.x], 0.05f);
	}

	IEnumerator ClearGrid()
	{
		yield return null;
		int CurrentColor = _component.GetValue<int>("CurrentColor");
		System.Random random = new System.Random(); // This is totally unnecessary, but it looks cooler if it's all scrambled up.

		// The grid gets cleared by clicking everything one color and then twice with another color.
		// The first click ensures nothing is the second color, the second makes everything the second color and the third makes everything white.
		for (int i = 0; i < 3; i++)
		{
			yield return RespondToCommandInternal($"color {Math.Min(i + 1, 2)}");
			yield return Enumerable.Range(0, 64).OrderBy(_ => random.NextDouble()).Select(index => DoInteractionClick(GridButtons[index], 0.0025f)).GetEnumerator();
		}

		yield return RespondToCommandInternal($"color {CurrentColor}"); // Restore the original color.
	}

	IEnumerator ClickButton(KMSelectable selectable, int count)
	{
		yield return null;
		for (int i = 0; i < count; i++)
			yield return DoInteractionClick(selectable);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		yield return RespondToCommandInternal("right 8;clear"); // Move to the build page and clear.

		int[] shiftedSolution = _component.CallMethod<int[]>("ShiftGrid", _component.GetValue<int[]>("SolutionDisplay"));
		for (int i = 1; i <= shiftedSolution.Max(); i++)
		{
			var gridIndexes = Enumerable.Range(0, 64).Where(index => shiftedSolution[index] == i).ToArray();
			if (gridIndexes.Length == 0) continue;

			yield return RespondToCommandInternal($"color {i}");
			yield return gridIndexes.Select(index => DoInteractionClick(GridButtons[index])).GetEnumerator();
		}

		yield return RespondToCommandInternal("submit");
	}

	static LEGOComponentSolver()
	{
		ComponentType = ReflectionHelper.FindType("LEGOModule");
	}

	private static readonly Type ComponentType;
	private readonly object _component;

	private readonly KMSelectable[] GridButtons;
}
