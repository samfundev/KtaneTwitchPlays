using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MemorableButtonsComponentSolver : ComponentSolver
{
	public MemorableButtonsComponentSolver(TwitchModule module) :
		base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentSolverType);
		solveKeypad = _component.GetValue<KMSelectable[]>("solveKeypad");
		buttons = Module.BombComponent.GetComponent<KMSelectable>().Children;
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Press the x button !{0} press x; Buttons can be: 1; 2; 3; 4; TL; TR; BL; BR; (Only use this before solve stage!) On solve stage: '!{0} solve x y' where x and y are numbers between 1 and 12 (The buttons are numbered in reading order.)");
		buttonOrder = new Dictionary<int, int>()
		{
			{1, 9},
			{2, 10},
			{3, 11},
			{4, 12},
			{5, 5},
			{6, 6},
			{7, 7},
			{8, 8},
			{9, 1},
			{10, 2},
			{11, 3},
			{12, 4},
		};
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string command = inputCommand.ToLowerInvariant();
		KMSelectable btn;
		if (command.StartsWith("press"))
		{
			command = command.Replace("press ", "");
			switch (command)
			{
				case "1":
				case "TL":
					btn = buttons[2];
					break;
				case "2":
				case "TR":
					btn = buttons[3];
					break;
				case "3":
				case "BL":
					btn = buttons[0];
					break;
				case "4":
				case "BR":
					btn = buttons[1];
					break;
				default:
					yield return $"sendtochaterror {command} is not a valid button.";
					yield break;
			}
			yield return null;
			yield return DoInteractionClick(btn);
		}
		else if (command.StartsWith("solve"))
		{
			command = command.Replace("solve ", "");
			string[] btns = command.SplitFull(' ', ';', ',');
			List<KMSelectable> btnstopress = new List<KMSelectable>();
			foreach (string input in btns)
			{
				if (!int.TryParse(input, out int output))
				{
					yield return "sendtochaterror Number not valid!";
					yield break;
				}

				if (!output.InRange(1, 12))
				{
					yield return "sendtochaterror Number out of range!";
					yield break;
				}
				btnstopress.Add(solveKeypad[buttonOrder[output] - 1]);
			}

			yield return null;
			foreach (KMSelectable topress in btnstopress)
			{
				yield return DoInteractionClick(topress);
			}
		}
	}

	private KMSelectable[] buttons;
	private KMSelectable[] solveKeypad;
	private Dictionary<int, int> buttonOrder;

	private readonly Component _component;
	private static readonly Type ComponentSolverType = ReflectionHelper.FindType("MemorableButtons");
}