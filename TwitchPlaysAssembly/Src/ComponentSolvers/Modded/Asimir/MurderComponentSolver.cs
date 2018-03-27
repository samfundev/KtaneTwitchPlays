using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class MurderComponentSolver : ComponentSolver
{
	public MurderComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		_component = bombComponent.GetComponent(_componentType);
		_buttons = (KMSelectable[]) _buttonsField.GetValue(_component);
		_display = (TextMesh[]) _displayField.GetValue(_component);
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Cycle the options with !{0} cycle or !{0} cycle people (also weapons and rooms). Make an accusation with !{0} It was Peacock, with the candlestick, in the kitchen. Or you can set the options individually, and accuse with !{0} accuse.");
	}

	IEnumerable CycleThroughCategory(int index, string search = null)
	{
		int length = (index == 2) ? 9 : 4;
		//float delay = (search != null) ? 0.05f : 1.0f; // Doesn't seem to be used.
		KMSelectable button = _buttons[(index * 2) + 1];
		for (int i = 0; i < length; i++)
		{
			if ((search != null) &&
				(_display[index].text.ToLowerInvariant().EndsWith(search)))
			{
				yield return true;
				break;
			}
			yield return button;
		}
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.ToLowerInvariant();

		if (inputCommand.Equals("accuse"))
		{
			yield return "accuse";
			yield return DoInteractionClick(_buttons[6]);
			yield break;
		}
		else if (inputCommand.StartsWith("cycle"))
		{
			bool cycleAll = (inputCommand.Equals("cycle"));
			for (int i = 0; i < 3; i++)
			{
				if ((!cycleAll) && (!inputCommand.EndsWith(NameTypes[i]))) continue;

				yield return inputCommand;
				yield return null;
				var j = 0.0;
				foreach (var item in CycleThroughCategory(i))
				{
					if (i == 2) j = 0.8;
					else j = 1.5;
					DoInteractionClick((MonoBehaviour) item);
					yield return "trywaitcancel " + j.ToString() + " The murder cycle command was cancelled";
				}
			}
			yield break;
		}

		bool[] set = new bool[3] { false, false, false };
		bool[] tried = new bool[3] { false, false, false };

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

		for(int i = 0; i < catIndexes.Length; i++)
		{
			int catIndex = catIndexes[i];
			string value = values[i];
			if (set[catIndex]) continue;

			tried[catIndex] = true;

			foreach (var item in CycleThroughCategory(catIndex, value))
			{
				if ((item is bool b) && b)
				{
					yield return null;
					yield return null;
					set[catIndex] = true;
					break;
				}
				yield return DoInteractionClick((MonoBehaviour) item);
			}
		}

		if ((set[0]) && (set[1]) && (set[2]))
		{
			yield return DoInteractionClick(_buttons[6]);
		}
		else
		{
			for (var i = 0; i < 3; i++)
			{
				if (!tried[i] || set[i]) continue;
				yield return "unsubmittablepenalty";
				yield break;
			}
		}
	}

	static MurderComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("MurderModule");
		_buttonsField = _componentType.GetField("buttons", BindingFlags.Public | BindingFlags.Instance);
		_displayField = _componentType.GetField("Display", BindingFlags.Public | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static FieldInfo _buttonsField = null;
	private static FieldInfo _displayField = null;

	private static readonly string[] People = new string[6] { "Colonel Mustard", "Miss Scarlett", "Mrs Peacock", "Mrs White", "Professor Plum", "Reverend Green" };
	private static readonly string[] Weapons = new string[6] { "Dagger", "Candlestick", "Lead Pipe", "Revolver", "Rope", "Spanner" };
	private static readonly string[] Rooms = new string[9] { "Ballroom", "Billiard Room", "Conservatory", "Dining Room", "Hall", "Kitchen", "Library", "Lounge", "Study"};

	private static readonly string[] Commands = new string[3] { "it was", "with the", "in the" };
	private static readonly string[] NameTypes = new string[3] { "people", "weapons", "rooms" };
	private static readonly string[][] NameSpellings = new string[3][] { People, Weapons, Rooms };
	private static readonly string[] NameMisspelled = new string[3] {"Who the hell is {0}? The only people I know about are {1}", "What the hell is a {0}? The only weapons I know about are {1}.", "Where in the hell is {0}? The Only rooms I know about are {1}."};

	

	private object _component = null;
	private KMSelectable[] _buttons = null;
	private TextMesh[] _display = null;
}
