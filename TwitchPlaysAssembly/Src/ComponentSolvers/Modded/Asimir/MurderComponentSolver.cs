using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class MurderComponentSolver : ComponentSolver
{
	public MurderComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		_component = bombComponent.GetComponent(ComponentType);
		_buttons = (KMSelectable[]) ButtonsField.GetValue(_component);
		_display = (TextMesh[]) DisplayField.GetValue(_component);
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Cycle the options with !{0} cycle or !{0} cycle people (also weapons and rooms). Make an accusation with !{0} It was Peacock, with the candlestick, in the kitchen. Or you can set the options individually, and accuse with !{0} accuse.");
	}

	private IEnumerable CycleThroughCategory(int index, string search = null)
	{
		int length = index == 2 ? 9 : 4;
		//float delay = (search != null) ? 0.05f : 1.0f; // Doesn't seem to be used.
		KMSelectable button = _buttons[index * 2 + 1];
		for (int i = 0; i < length; i++)
		{
			if (search != null &&
				_display[index].text.ToLowerInvariant().EndsWith(search))
			{
				yield return true;
				break;
			}
			yield return button;
		}
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.ToLowerInvariant().Trim();

		if (inputCommand.Equals("accuse"))
		{
			yield return "accuse";
			yield return DoInteractionClick(_buttons[6]);
			yield break;
		}

		if (inputCommand.StartsWith("cycle"))
		{
			bool cycleAll = inputCommand.Equals("cycle");
			for (int i = 0; i < 3; i++)
			{
				if (!cycleAll && !inputCommand.EndsWith(NameTypes[i])) continue;

				yield return inputCommand;
				yield return null;
				foreach (object item in CycleThroughCategory(i))
				{
					double j = i == 2 ? 0.8 : 1.5;
					DoInteractionClick((MonoBehaviour) item);
					yield return "trywaitcancel " + j + " The murder cycle command was cancelled";
				}
			}
			yield break;
		}

		bool[] set = { false, false, false };
		bool[] tried = { false, false, false };

		List<Match> matches = Regex.Matches(inputCommand, @"(" + string.Join("|", Commands) + ") ([a-z ]+)").Cast<Match>()
			.Where(match => Array.IndexOf(Commands, match.Groups[1].ToString()) > -1).ToList();

		int[] catIndexes = matches.Select(match => Array.IndexOf(Commands, match.Groups[1].ToString())).ToArray();
		string[] values = matches.Select(match => match.Groups[2].ToString().Trim()).ToArray();

		bool misspelled = false;
		for (int i = 0; i < catIndexes.Length; i++)
		{
			int catIndex = catIndexes[i];
			string value = values[i];

			if (set[catIndex]) continue;

			misspelled |= !NameSpellings[catIndex].Any(x => x.EndsWith(value, StringComparison.InvariantCultureIgnoreCase));
			if (!misspelled) continue;

			yield return null;
			yield return $"sendtochat {string.Format(NameMisspelled[catIndex], value, string.Join(", ", NameSpellings[catIndex]))}";
			set[catIndex] = true;
		}
		if (misspelled) yield break;

		for (int i = 0; i < catIndexes.Length; i++)
		{
			int catIndex = catIndexes[i];
			string value = values[i];
			if (set[catIndex]) continue;

			tried[catIndex] = true;

			foreach (object item in CycleThroughCategory(catIndex, value))
			{
				if (item is bool b && b)
				{
					yield return null;
					yield return null;
					set[catIndex] = true;
					break;
				}
				yield return DoInteractionClick((MonoBehaviour) item);
			}
		}

		if (set[0] && set[1] && set[2])
		{
			yield return DoInteractionClick(_buttons[6]);
		}
		else
		{
			for (int i = 0; i < 3; i++)
			{
				if (!tried[i] || set[i]) continue;
				yield return "unsubmittablepenalty";
				yield break;
			}
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		while (!IsActivated)
		{
			yield return true;
		}

		DebugHelper.Log($"Display Values = {DisplayValue[0]}, {DisplayValue[1]}, {DisplayValue[2]},  Solution values = {SolutionValue[0]}, {SolutionValue[1]}, {SolutionValue[2]}");
		while (DisplayValue[0] != SolutionValue[0] || DisplayValue[1] != SolutionValue[1] || DisplayValue[2] != SolutionValue[2])
		{
			if (DisplayValue[0] != SolutionValue[0])
				yield return DoInteractionClick(_buttons[1]);
			else if (DisplayValue[1] != SolutionValue[1])
				yield return DoInteractionClick(_buttons[3]);
			else
				yield return DoInteractionClick(_buttons[5]);
		}

		yield return DoInteractionClick(_buttons[6]);
	}

	static MurderComponentSolver()
	{
		ComponentType = ReflectionHelper.FindType("MurderModule");
		ButtonsField = ComponentType.GetField("buttons", BindingFlags.Public | BindingFlags.Instance);
		DisplayField = ComponentType.GetField("Display", BindingFlags.Public | BindingFlags.Instance);

		DisplayValueField = ComponentType.GetField("displayVal", BindingFlags.NonPublic | BindingFlags.Instance);
		SolutionValueField = ComponentType.GetField("solution", BindingFlags.NonPublic | BindingFlags.Instance);
		IsActivatedField = ComponentType.GetField("isActivated", BindingFlags.NonPublic | BindingFlags.Instance);
	}

	private static readonly Type ComponentType;
	private static readonly FieldInfo ButtonsField;
	private static readonly FieldInfo DisplayField;
	private static readonly FieldInfo DisplayValueField;
	private static readonly FieldInfo SolutionValueField;
	private static readonly FieldInfo IsActivatedField;

	private static readonly string[] People = { "Colonel Mustard", "Miss Scarlett", "Mrs Peacock", "Mrs White", "Professor Plum", "Reverend Green" };
	private static readonly string[] Weapons = { "Dagger", "Candlestick", "Lead Pipe", "Revolver", "Rope", "Spanner" };
	private static readonly string[] Rooms = { "Ballroom", "Billiard Room", "Conservatory", "Dining Room", "Hall", "Kitchen", "Library", "Lounge", "Study" };

	private static readonly string[] Commands = { "it was", "with the", "in the" };
	private static readonly string[] NameTypes = { "people", "weapons", "rooms" };
	private static readonly string[][] NameSpellings = { People, Weapons, Rooms };
	private static readonly string[] NameMisspelled = { "Who the hell is {0}? The only people I know about are {1}", "What the hell is a {0}? The only weapons I know about are {1}.", "Where in the hell is {0}? The Only rooms I know about are {1}." };

	private readonly object _component;
	private readonly KMSelectable[] _buttons;
	private readonly TextMesh[] _display;

	private int[] DisplayValue => (int[]) DisplayValueField.GetValue(_component);
	private int[] SolutionValue => (int[]) SolutionValueField.GetValue(_component);
	private bool IsActivated => (bool) IsActivatedField.GetValue(_component);
}
