using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

public class ThreeDMazeComponentSolver : ComponentSolver
{
	public ThreeDMazeComponentSolver(BombCommander bombCommander, BombComponent bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
		base(bombCommander, bombComponent, ircConnection, canceller)
	{
		object _component = bombComponent.GetComponent(_componentType);
		_buttonLeft = (KMSelectable) _buttonLeftField.GetValue(_component);
		_buttonRight = (KMSelectable) _buttonRightField.GetValue(_component);
		_buttonStraight = (KMSelectable) _buttonStraightField.GetValue(_component);
	    modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
	}

	private KMSelectable[] ShortenDirection(string direction)
	{
		switch (direction)
		{
			case "l":
			case "left":
				return new[] {_buttonLeft};
			case "r":
			case "right":
				return new[] {_buttonRight};
			case "f":
			case "forward":
				return new[] {_buttonStraight};
			case "u":
			case "u-turn":
	        case "uturn":
		    case "turnaround":
		    case "turn-around":
				return new[] {_buttonRight, _buttonRight};
			default:
				return new KMSelectable[] {null};
		}
	}
	
	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] commands = inputCommand.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if (commands.Length <= 1 || !commands[0].EqualsAny("move", "m", "walk", "w")) yield break;

		List<KMSelectable> moves = commands.Where((_, i) => i > 0).SelectMany(ShortenDirection).ToList();
		if (moves.Any(m => m == null)) yield break;
		yield return null;

		bool moving = commands[0].EqualsAny("move", "m");
		if (moves.Count > (moving ? 64 : 16))
		{
			yield return "elevator music";
		}

		float moveDelay = moving ? 0.1f : 0.4f;
				
		foreach (KMSelectable move in moves)
		{
			yield return DoInteractionClick(move, moveDelay);
		}
	}

	static ThreeDMazeComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("ThreeDMazeModule");
		_buttonLeftField = _componentType.GetField("ButtonLeft", BindingFlags.Public | BindingFlags.Instance);
		_buttonRightField = _componentType.GetField("ButtonRight", BindingFlags.Public | BindingFlags.Instance);
		_buttonStraightField = _componentType.GetField("ButtonStraight", BindingFlags.Public | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static FieldInfo _buttonLeftField = null;
	private static FieldInfo _buttonRightField = null;
	private static FieldInfo _buttonStraightField = null;

	private KMSelectable _buttonLeft = null;
	private KMSelectable _buttonRight = null;
	private KMSelectable _buttonStraight = null;
}
