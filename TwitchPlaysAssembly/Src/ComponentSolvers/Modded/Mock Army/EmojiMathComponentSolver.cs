using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

public class EmojiMathComponentSolver : ComponentSolver
{
	public EmojiMathComponentSolver(BombCommander bombCommander, BombComponent bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
		base(bombCommander, bombComponent, ircConnection, canceller)
	{
		_buttons = (KMSelectable[]) _buttonsField.GetValue(bombComponent.GetComponent(_componentType));
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
    }

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		var commands = inputCommand.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if (commands.Length == 2 && commands[0] == "submit")
		{
			List<int> buttonIndexes = new List<int>();
			bool negitive = false;

			int index = 0;
			foreach (char c in commands[1].ToCharArray())
			{
				if (c == '-' && index == 0)
				{
					negitive = true;
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
			
			if (negitive != negitiveActive)
			{
				yield return DoInteractionClick(_buttons[10]);
				negitiveActive = negitive;
			}

			foreach (int ind in buttonIndexes) yield return DoInteractionClick(_buttons[ind]);
				
			yield return DoInteractionClick(_buttons[11]);
		}
	}

	static EmojiMathComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("EmojiMathModule");
		_buttonsField = _componentType.GetField("Buttons", BindingFlags.Public | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static FieldInfo _buttonsField = null;

	private KMSelectable[] _buttons = null;
	private bool negitiveActive = false;
}