using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class BooleanMazeComponentSolver : ComponentSolver
{
	public BooleanMazeComponentSolver(TwitchModule module) :
		base(module)
	{
		selectables = Module.BombComponent.GetComponent<KMSelectable>().Children;
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Press buttons using !{0} press <button>. Buttons can be specified in full or based on their last character.");
	}

	readonly Dictionary<string, int> buttonMap = new Dictionary<string, int>()
	{
		{ "u", 1 },
		{ "l", 3 },
		{ "stuck?", 4 },
		{ "stuck", 4 },
		{ "r", 5 },
		{ "d", 7 },
		{ "reset!", 8 },
		{ "reset", 8 }
	};

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = Regex.Replace(inputCommand.ToLowerInvariant().Trim(), "^(press|hit|enter|push) ", "");

		foreach (KeyValuePair<string, int> pair in buttonMap)
		{
			if (pair.Key == inputCommand || pair.Key.Last().ToString() == inputCommand)
			{
				yield return null;
				yield return DoInteractionClick(selectables[pair.Value]);
				yield break;
			}
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		Type boolMazeType = ReflectionHelper.FindType("boolMaze");
		if (boolMazeType == null) yield break;

		object component = Module.BombComponent.GetComponent(boolMazeType);

		Vector2Int currentPosition = new Vector2Int(component.GetValue<int>("gridPosCol"), component.GetValue<int>("gridPosRow"));
		Vector2Int goal = new Vector2Int(component.GetValue<int>("correctgridcol"), component.GetValue<int>("correctgridrow"));
		int[,] grid = component.GetValue<int[,]>("grid");

		TextMesh NumDisplay = component.GetValue<TextMesh>("NumDisplay");

		Dictionary<char, Vector2Int> moves = new Dictionary<char, Vector2Int>
		{
			{ 'u', new Vector2Int( 0, -1) },
			{ 'd', new Vector2Int( 0,  1) },
			{ 'l', new Vector2Int(-1,  0) },
			{ 'r', new Vector2Int( 1,  0) },
		};

		bool IsLegalMove(Vector2Int position) => position.x.InRange(0, 9) && position.y.InRange(0, 9) && component.CallMethod<bool>("CheckLegalMove", grid[position.y, position.x]);

		yield return null;
		while (currentPosition != goal)
		{
			var legalMoves = moves.Where(pair => IsLegalMove(pair.Value + currentPosition)).ToList();
			if (legalMoves.Count == 0) // If we have no legal moves, we're stuck.
			{
				DoInteractionClick(selectables[buttonMap["stuck?"]]);
			}
			else
			{
				Vector2Int goalDirection = goal - currentPosition;
				var goodMoves = legalMoves.Where(pair =>
				{
					var direction = pair.Value;
					return direction.x != 0 && Math.Sign(direction.x) == Math.Sign(goalDirection.x) || direction.y != 0 && Math.Sign(direction.y) == Math.Sign(goalDirection.y);
				}).ToList();

				// If we have no good moves, just move somewhere.
				// But if a move that will move us towards the goal, use one of those.
				var move = goodMoves.Count == 0 ? legalMoves[0] : goodMoves[0];

				currentPosition += move.Value;
				DoInteractionClick(selectables[buttonMap[move.Key.ToString()]]);
			}

			yield return new WaitUntil(() => NumDisplay.text.Length == 0);
			yield return new WaitUntil(() => NumDisplay.text.Length != 0);
		}
	}

	private readonly KMSelectable[] selectables;
}
