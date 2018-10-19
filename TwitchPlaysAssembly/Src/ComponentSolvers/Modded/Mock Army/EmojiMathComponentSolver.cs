using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class EmojiMathComponentSolver : ComponentSolver
{
	public EmojiMathComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		_buttons = (KMSelectable[]) _buttonsField.GetValue(bombComponent.GetComponent(_componentType));
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Submit an answer using !{0} submit -47.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] commands = inputCommand.ToLowerInvariant().Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if (commands.Length != 2 || commands[0] != "submit") yield break;
		List<int> buttonIndexes = new List<int>();
		bool negative = false;

		int index = 0;
		foreach (char c in commands[1])
		{
			if (c == '-' && index == 0)
			{
				negative = true;
			}
			else
			{
				if (int.TryParse(c.ToString(), out int num))
				{
					buttonIndexes.Add(num);
				}
				else
				{
					yield break;
				}
			}

			index++;
		}

		if (negative != _negativeActive)
		{
			yield return DoInteractionClick(_buttons[10]);
			_negativeActive = negative;
		}

		foreach (int ind in buttonIndexes) yield return DoInteractionClick(_buttons[ind]);

		yield return DoInteractionClick(_buttons[11]);
	}

	static EmojiMathComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("EmojiMathModule");
		_buttonsField = _componentType.GetField("Buttons", BindingFlags.Public | BindingFlags.Instance);
	}

	private static readonly Type _componentType;
	private static readonly FieldInfo _buttonsField;

	private readonly KMSelectable[] _buttons;
	private bool _negativeActive;
}
