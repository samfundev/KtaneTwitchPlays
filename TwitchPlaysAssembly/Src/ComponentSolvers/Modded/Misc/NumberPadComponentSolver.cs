using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NumberPadComponentSolver : ComponentSolver
{
	public NumberPadComponentSolver(BombCommander bombCommander, BombComponent bombComponent, CoroutineCanceller canceller) :
		base(bombCommander, bombComponent, canceller)
	{
	    Component component = bombComponent.GetComponent("NumberPadModule");
	    if (component == null)
	    {
	        throw new NotSupportedException("Could not get NumberPadModule Component from bombComponent");
	    }

        Type componentType = component.GetType();
	    if (componentType == null)
	    {
	        throw new NotSupportedException("Could not get componentType from NumberPadModule Component");
	    }

        FieldInfo buttonsField = componentType.GetField("buttons", BindingFlags.Public | BindingFlags.Instance);
        if(buttonsField == null)
        {
            throw new NotSupportedException("Could not find the KMSelectable fields in component Type");
        }

        _buttons = (KMSelectable[]) buttonsField.GetValue(component);
        if(_buttons == null)
        {
            throw new NotSupportedException("Component had null KMSelectables.");
        }

        modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
	}

	int? ButtonToIndex(string button)
	{
		switch (button)
		{
			case "enter":
				return 0;
			case "0":
				return 1;
			case "clear":
				return 2;
			default:
			    if (int.TryParse(button, out int i))
				{
					return i + 2;
				}
				else
				{
					return null;
				}
		}
	}
	
	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		var commands = inputCommand.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if (commands.Length == 2 && commands[0].Equals("submit"))
		{
			List<string> buttons = commands[1].Select(c => c.ToString()).ToList();

			if (buttons.Count() == 4 && buttons.All(num => ButtonToIndex(num) != null))
			{
				yield return null;

				buttons.Insert(0, "clear");
				buttons.Add("enter");
				foreach (string button in buttons)
				{
					int? buttonIndex = ButtonToIndex(button);
					if (buttonIndex != null)
					{
						KMSelectable buttonSelectable = _buttons[(int) buttonIndex];

						DoInteractionClick(buttonSelectable);
						yield return new WaitForSeconds(0.1f);
					}
				}
			}
		}
	}

	private KMSelectable[] _buttons = null;
}
