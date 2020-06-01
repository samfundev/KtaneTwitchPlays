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
		finalKeypad = _component.GetValue<KMSelectable[]>("solveKeypad");
		interKeypad = Module.BombComponent.GetComponent<KMSelectable>().Children;
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Press a button with '!{0} press 1' or '!{0} press TL'. When at the solving stage, press multiple buttons with '!{0} press 1 5 9 4 8 12 …'. Buttons are numbered in reading order at all times.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string command = inputCommand.ToLowerInvariant().Trim();
		bool solvingMode = finalKeypad[0].gameObject.activeInHierarchy;

		if (command.StartsWith("press "))
			command = command.Replace("press ", "");
		else if (command.StartsWith("select "))
			command = command.Replace("select ", "");
		else
			yield break;

		if (!solvingMode)
		{
			if (!interButtonOrder.ContainsKey(command))
			{
				yield return $"sendtochaterror I don't know what button '{command}' is. (Note: You're not at the solving stage yet.)";
				yield break;
			}

			yield return null;
			yield return DoInteractionClick(interKeypad[interButtonOrder[command]]);
		}
		else
		{
			string[] allCommands = command.SplitFull(' ', ';', ',');
			List<KMSelectable> inputList = new List<KMSelectable>();
			if (inputList.Count > 15)
				yield break;

			foreach (string sCmd in allCommands)
			{
				if (!finalButtonOrder.ContainsKey(sCmd))
				{
					yield return $"sendtochaterror I don't know what button '{sCmd}' is. (Note: You're at the solving stage.)";
					yield break;
				}

				inputList.Add(finalKeypad[finalButtonOrder[sCmd]]);
			}

			yield return null;

			int i = 0;
			foreach (KMSelectable input in inputList)
			{
				yield return $"strikemessage the {ordinals[i++]} input given";
				yield return DoInteractionClick(input);
			}
		}
	}

	static private readonly string[] ordinals = new string[]
	{
		"1st", "2nd", "3rd", "4th", "5th", "6th", "7th", "8th", "9th", "10th",
		"11th", "12th", "13th", "14th", "15th"
	};

	static private readonly Dictionary<string, int> interButtonOrder = new Dictionary<string, int>()
	{
		{ "1", 2}, { "2", 3}, { "3", 0}, { "4", 1},
		{"tl", 2}, {"tr", 3}, {"bl", 0}, {"br", 1},
	};

	static private readonly Dictionary<string, int> finalButtonOrder = new Dictionary<string, int>()
	{
		{ "1", 8},  { "2",  9}, { "3", 10}, { "4", 11},
		{ "5", 4},  { "6",  5}, { "7",  6}, { "8",  7},
		{ "9", 0},  {"10",  1}, {"11",  2}, {"12",  3},
	};

	private readonly KMSelectable[] interKeypad;
	private readonly KMSelectable[] finalKeypad;

	private readonly Component _component;
	private static readonly Type ComponentSolverType = ReflectionHelper.FindType("MemorableButtons");
}
