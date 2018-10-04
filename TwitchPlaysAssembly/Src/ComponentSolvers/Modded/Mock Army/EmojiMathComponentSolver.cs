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
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Submit an answer using !{0} submit -47.");
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

		if (negative != negativeActive)
		{
			yield return DoInteractionClick(_buttons[10]);
			negativeActive = negative;
		}

		foreach (int ind in buttonIndexes) yield return DoInteractionClick(_buttons[ind]);

		yield return DoInteractionClick(_buttons[11]);
	}

	static EmojiMathComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("EmojiMathModule");
		_buttonsField = _componentType.GetField("Buttons", BindingFlags.Public | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static FieldInfo _buttonsField = null;

	private readonly KMSelectable[] _buttons = null;
	private bool negativeActive = false;
}
