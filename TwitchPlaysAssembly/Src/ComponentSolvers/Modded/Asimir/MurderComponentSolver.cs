using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class MurderComponentSolver : ComponentSolver
{
	public MurderComponentSolver(TwitchModule module) :
		base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
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
					yield return DoInteractionClick((MonoBehaviour) item);
					yield return "trywaitcancel " + j + " The murder cycle command was cancelled";
				}
			}
			yield break;
		}

		var matches = Regex.Matches(inputCommand, @"(" + string.Join("|", Commands) + ") ([a-z ]+)");
		var present = new bool[3];
		for (int i = 0; i < matches.Count; i++)
		{
			int catIndex = Array.IndexOf(Commands, matches[i].Groups[1].ToString());
			if (catIndex == -1)
				continue;
			string value = matches[i].Groups[2].ToString().Trim();

			yield return null;
			if (!NameSpellings[catIndex].Any(x => x.EndsWith(value, StringComparison.InvariantCultureIgnoreCase)))
			{
				yield return $"sendtochat {string.Format(NameMisspelled[catIndex], value, string.Join(", ", NameSpellings[catIndex]))}";
				continue;
			}

			var found = false;
			foreach (object item in CycleThroughCategory(catIndex, value))
			{
				if (item is bool b && b)
				{
					found = true;
					break;
				}
				yield return DoInteractionClick((MonoBehaviour) item);
			}
			if (!found)
			{
				yield return "unsubmittablepenalty";
				yield break;
			}
			present[catIndex] = true;
		}

		if (present.All(b => b))
			yield return DoInteractionClick(_buttons[6]); // In this case, yield return null already happened.
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
	private static readonly string[] NameMisspelled = { "Who the hell is {0}? The only people I know about are {1}", "What the hell is a {0}? The only weapons I know about are {1}.", "Where in the hell is {0}? The only rooms I know about are {1}." };

	private readonly object _component;
	private readonly KMSelectable[] _buttons;
	private readonly TextMesh[] _display;

	private int[] DisplayValue => (int[]) DisplayValueField.GetValue(_component);
	private int[] SolutionValue => (int[]) SolutionValueField.GetValue(_component);
	private bool IsActivated => (bool) IsActivatedField.GetValue(_component);
}
