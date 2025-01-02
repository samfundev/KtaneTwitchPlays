using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class EmojiMathComponentSolver : ComponentSolver
{
	public EmojiMathComponentSolver(TwitchModule module) :
		base(module)
	{
		_buttons = (KMSelectable[]) _buttonsField.GetValue(module.BombComponent.GetComponent(_componentType));
		SetHelpMessage("Submit an answer using !{0} submit -47.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] commands = inputCommand.ToLowerInvariant().Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if (commands.Length != 2 || commands[0] != "submit" || !int.TryParse(commands[1], out int _)) yield break;
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

	private static readonly Type _componentType = ReflectionHelper.FindType("EmojiMathModule");
	private static readonly FieldInfo _buttonsField = _componentType.GetField("Buttons", BindingFlags.Public | BindingFlags.Instance);

	private readonly KMSelectable[] _buttons;
	private bool _negativeActive;
}