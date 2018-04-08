using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

public class ColorMorseComponentSolver : ComponentSolver
{
	public ColorMorseComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		_buttons = (KMSelectable[]) _buttonsField.GetValue(bombComponent.GetComponent(_componentType));
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Submit some morse code using !{0} transmit ....- --...");
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		var commands = inputCommand.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if (commands.Length >= 2 && commands[0].EqualsAny("transmit", "submit", "trans", "tx", "xmit"))
		{
			List<int> buttonIndexes = new List<int>();
			foreach (string morse in commands.Skip(1))
			{
				foreach (char character in morse)
				{

					int index = 0;
					switch (character)
					{
						case '.':
							index = 0;
							break;
						case '-':
							index = 1;
							break;
						default:
							yield break;
					}

					buttonIndexes.Add(index);
				}

				buttonIndexes.Add(2);
			}

			foreach (int index in buttonIndexes.Take(buttonIndexes.Count - 1))
			{
				yield return DoInteractionClick(_buttons[index]);
			}
		}
	}

	static ColorMorseComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("FlashingMathModule");
		_buttonsField = _componentType.GetField("Buttons");
	}
	
	private static Type _componentType = null;
	private static FieldInfo _buttonsField = null;

	private KMSelectable[] _buttons = null;
}
