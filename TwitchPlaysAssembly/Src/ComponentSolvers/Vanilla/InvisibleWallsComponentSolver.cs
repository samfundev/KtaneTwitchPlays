using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class InvisibleWallsComponentSolver : ComponentSolver
{
	public InvisibleWallsComponentSolver(TwitchModule module) :
		base(module)
	{
		_buttons = ((InvisibleWallsComponent) module.BombComponent).Buttons;
		ModInfo = ComponentSolverFactory.GetModuleInfo("Maze", "!{0} move up down left right, !{0} move udlr [make a series of white icon moves]");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.Trim();
		if (!inputCommand.StartsWith("move ", StringComparison.InvariantCultureIgnoreCase))
			yield break;

		inputCommand = inputCommand.Substring(5);
		MatchCollection matches = Regex.Matches(inputCommand, @"[udlr]", RegexOptions.IgnoreCase);
		if (matches.Count > 35)
		{
			yield return null;
			yield return "elevator music";
		}

		foreach (Match move in matches)
		{
			KeypadButton button = _buttons[ButtonIndex[move.Value.ToLowerInvariant()]];

			if (button == null) continue;
			yield return move.Value;
			yield return "trycancel";
			yield return DoInteractionClick(button);
		}
	}

	private static readonly Dictionary<string, int> ButtonIndex = new Dictionary<string, int>
	{
		{"u", 0}, {"l", 1}, {"r", 2}, {"d", 3}
	};

	private static int GetLocationFromCell(MazeCell cell) => cell.Y * 10 + cell.X;

	private readonly Stack<int> _mazeStack = new Stack<int>();
	private bool[] _explored;
	private bool GenerateMazeSolution(int startXY)
	{
		var component = (InvisibleWallsComponent) Module.BombComponent;
		if (startXY == 77)
		{
			_explored = new bool[60];
			startXY = GetLocationFromCell(component.CurrentCell);
		}
		int endXY = GetLocationFromCell(component.GoalCell);

		int x = startXY % 10;
		int y = startXY / 10;

		if (x > 5 || y > 5 || endXY == 66) return false;
		//var directions = _mazes[maze, y, x];
		MazeCell cell = component.Maze.GetCell(x, y);
		bool[] directions = { cell.WallAbove, cell.WallBelow, cell.WallLeft, cell.WallRight };
		if (startXY == endXY) return true;
		_explored[startXY] = true;

		int[] directionInt = { -10, 10, -1, 1 };
		int[] directionReturn = { 0, 3, 1, 2 };

		for (int i = 0; i < 4; i++)
		{
			if (directions[i]) continue;
			if (_explored[startXY + directionInt[i]]) continue;
			if (!GenerateMazeSolution(startXY + directionInt[i])) continue;
			_mazeStack.Push(directionReturn[i]);
			return true;
		}
		return false;
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		while (!Module.BombComponent.IsActive) yield return true;
		if (Module.Solved) yield break;
		if (!GenerateMazeSolution(77)) yield break;
		while (_mazeStack.Count > 0)
			yield return DoInteractionClick(_buttons[_mazeStack.Pop()]);
	}

	private readonly List<KeypadButton> _buttons;
}
