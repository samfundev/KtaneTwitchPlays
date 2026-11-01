using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

[ModuleID("bamboozlingButtonGrid")]
internal class BamboozlingButtonGrid : ComponentSolverShim
{
	private static readonly Type ComponentType = ReflectionHelper.FindType("BamGScript");
	private readonly object _component;

	#region Moudule Stuff
	private List<KMSelectable> buttons;
	#endregion

	public BamboozlingButtonGrid(TwitchModule module) : base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		buttons = _component.GetValue<List<KMSelectable>>("buttons");
	}

	protected override IEnumerator ForcedSolveIEnumeratorShimmed()
	{
		yield return null;

		//reset module
		buttons[16].OnInteract();

		for(int i = 0; i < 4; i++)
		{
			yield return new WaitForSeconds(2.5f);

			int[] validButtons = FindCorrectButtons();

			foreach (int button in validButtons)
			{
				buttons[button].OnInteract();
				yield return new WaitForSeconds(0.1f);
			}

			yield return true;
		}



		while (!_component.GetValue<bool>("moduleSolved"))
		{
			yield return true;
		}
	}

	//Find a configuration of buttons to press to solve the stage
	private int[] FindCorrectButtons()
	{
		//Create a list of buttons. one array represents one grid of buttons, and number is position index 0
		List<List<int>> buttonGrids = new List<List<int>>();

		for(int i = 0; i < 4; i++)
		{
			buttonGrids.Add(Enumerable.Range(0, 16).ToList());
		}
		//Remove all elements that are already pressed
		List<int> alreadypressed = _component.GetValue<List<int>>("alreadypressed");

		for (int i = 0; i < 4; i++)
		{
			foreach (int index in alreadypressed)
			{
				buttonGrids[i].Remove(index);
			}
		}

		//Get the 8 properties
		string[][] propselect = _component.GetValue<string[][]>("propselect");
		//Get all the colors for the buttons in the button grid
		List<string>[] buttoncycle = _component.GetValue<List<string>[]>("buttoncycle");
		//Remove all the buttons that are invalid

		for (int i = 0; i < 4; i++)
		{
			buttonGrids[i] = buttonGrids[i].Where(buttonPos => ValidButton(buttoncycle[buttonPos], new string[2] { propselect[0][i], propselect[1][i] })).ToList();
		}

		//Find 4 distinct buttons that are valid to press
		foreach (int button1 in buttonGrids[0])
		{
			foreach (int button2 in buttonGrids[1])
			{
				foreach (int button3 in buttonGrids[2])
				{
					foreach (int button4 in buttonGrids[3])
					{
						int[] buttonArr = new[] { button1, button2, button3, button4 };

						if (buttonArr.Distinct().Count() == 4)
						{
							return buttonArr;
						}
					}
				}
			}
		}

		//if this is reached, there is an error
		throw new InvalidOperationException("Could not find valid BBG configuration. Contact TP Developers.");
	}

	//Check if a button is valid given the properites
	private bool ValidButton(List<string> colors, string[] properties)
	{
		foreach (string property in properties)
		{
			if (Regex.Match(property, @"[RGBY][1-5]", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase).Success)
			{
				if (!CheckValidColor(colors, property))
				{
					return false;
				}
			}

			if (Regex.Match(property, @"\d{2}", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase).Success)
			{
				if (!CheckSameButtonColor(colors, property))
				{
					return false;
				}
			}
		}

		return true;
	}

	//Check if the color of the button matches the property
	private bool CheckValidColor(List<string> colors, string property)
	{
		int displayIndex = int.Parse(property.Substring(1)) - 1;
		bool valid = colors[displayIndex] == property[0].ToString();
		return valid;

	}

	//Check if the button is the same on two different grids
	private bool CheckSameButtonColor(List<string> colors, string property)
	{
		int buttonIndex1 = int.Parse(property.Substring(0, 1)) - 1;
		int buttonIndex2 = int.Parse(property.Substring(1, 1)) - 1;
		return colors[buttonIndex1] == colors[buttonIndex2];
	}
}