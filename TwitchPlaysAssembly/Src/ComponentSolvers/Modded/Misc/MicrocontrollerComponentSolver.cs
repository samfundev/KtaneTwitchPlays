using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class MicrocontrollerComponentSolver : ComponentSolver
{
	public MicrocontrollerComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		object component = bombComponent.GetComponent(ComponentType);
		_buttonOK = (KMSelectable) ButtonOKField.GetValue(component);
		_buttonUp = (KMSelectable) ButtonUpField.GetValue(component);
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Set the current pin color with !{0} set red. Cycle the current pin !{0} cycle. Valid colors: white, red, yellow, magenta, blue, green.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] commands = inputCommand.ToLowerInvariant().Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		// ReSharper disable once SwitchStatementMissingSomeCases
		switch (commands.Length)
		{
			case 2 when commands[0].Equals("set"):
			{
				int colorIndex = Array.IndexOf(Colors, commands[1]);
				if (colorIndex > -1)
				{
					yield return null;

					while (_currentIndex != colorIndex)
					{
						DoInteractionClick(_buttonUp);
						_currentIndex = (_currentIndex + 1) % 6;

						yield return new WaitForSeconds(0.1f);
					}

					int lastStrikeCount = StrikeCount;

					DoInteractionClick(_buttonOK);
					yield return new WaitForSeconds(0.1f);

					if (lastStrikeCount == StrikeCount)
					{
						_currentIndex = 0;
					}
				}

				break;
			}
			case 1 when commands[0].Equals("cycle"):
			{
				yield return null;

				for (int i = 0; i < 6; i++)
				{
					DoInteractionClick(_buttonUp);
					yield return new WaitForSeconds(0.2f);
				}

				break;
			}
		}
	}

	static MicrocontrollerComponentSolver()
	{
		ComponentType = ReflectionHelper.FindType("Micro");
		ButtonOKField = ComponentType.GetField("buttonOK", BindingFlags.Public | BindingFlags.Instance);
		ButtonUpField = ComponentType.GetField("buttonUp", BindingFlags.Public | BindingFlags.Instance);
	}

	private static readonly Type ComponentType;
	private static readonly FieldInfo ButtonOKField;
	private static readonly FieldInfo ButtonUpField;

	private static readonly string[] Colors = { "white", "red", "yellow", "magenta", "blue", "green" };
	private int _currentIndex;

	private readonly KMSelectable _buttonOK;
	private readonly KMSelectable _buttonUp;
}
