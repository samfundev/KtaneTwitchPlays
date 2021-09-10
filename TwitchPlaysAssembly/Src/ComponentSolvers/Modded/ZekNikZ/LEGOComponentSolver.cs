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
		ChainableCommands = true;
	}

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

		if (inputCommand.RegexMatch("^[lrud ]+$"))
		{
			yield return null;
			yield return inputCommand.Replace(" ", "").Select(character => MoveSelected(CharacterToDirection[character], false)).GetEnumerator();
			yield break;
		}

		string[] split = inputCommand.SplitFull(' ');
		int count = 1;
		if ((split.Length == 1 || split.Length == 2 && int.TryParse(split[1], out count)) && (split[0] == "left" || split[0] == "right"))
		{
			if (!count.InRange(1, 8)) yield break;

			bool leftButton = split[0] == "left";
			KMSelectable selectable = _component.GetValue<KMSelectable>($"{(leftButton ? "Left" : "Right")}Button");

			yield return null;
			for (int i = 0; i < count; i++)
				yield return DoInteractionClick(selectable);
		}
		else if (split.Length == 1)
		{
			if (split[0] == "submit")
			{
				yield return null;
				yield return DoInteractionClick(_component.GetValue<KMSelectable>("SubmitButton"));
			}
			else if (split[0] == "clear")
			{
				yield return null;
				yield return ClearGrid();
			}
		}
		else if (split.Length.EqualsAny(2, 3) && split[0].FirstOrWhole("select"))
		{
			if (split.Length == 3) split[1] = split.Skip(1).Join("");

			if (split[1].Length != 2) yield break;

			int column = split[1][0].ToIndex();
			int row = split[1][1].ToIndex();

			if (column.InRange(0, 7) && row.InRange(0, 7))
			{
				yield return null;
				yield return MoveSelected(new Vector2Int(column, row));
			}
		}
		else if (split.Length == 2 && split[0].FirstOrWhole("color") && int.TryParse(split[1], out int colorPosition) && colorPosition.InRange(1, 6))
		{
			yield return null;
			yield return DoInteractionClick(_component.GetValue<KMSelectable[]>("ColorButtons")[colorPosition - 1]);
		}
	}

	IEnumerator MoveSelected(Vector2Int position, bool absolute = true)
	{
		SelectedPosition = absolute ? position : SelectedPosition + position;
		// If we go out of bounds, wrap.
		SelectedPosition.x = SelectedPosition.x.Mod(8);
		SelectedPosition.y = SelectedPosition.y.Mod(8);

		yield return DoInteractionClick(GridButtons[(7 - SelectedPosition.y) * 8 + SelectedPosition.x], 0.05f);
	}

	IEnumerator ClearGrid()
	{
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

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		yield return ChainCommand("right 8;clear"); // Move to the build page and clear.

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

	private static readonly Type ComponentType = ReflectionHelper.FindType("LEGOModule");
	private readonly object _component;

	private readonly KMSelectable[] GridButtons;
}
