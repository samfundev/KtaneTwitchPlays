using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using UnityEngine;

public class ChordQualitiesComponentSolver : ComponentSolver
{
	public ChordQualitiesComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
		base(bombCommander, bombComponent, ircConnection, canceller)
	{
		object _component = bombComponent.GetComponent(_componentType);
		_wheelButton = (KMSelectable) _wheelButtonField.GetValue(_component);
		_selectButton = (KMSelectable) _selectButtonField.GetValue(_component);
		_submitButton = (KMSelectable) _submitButtonField.GetValue(_component);
		currentPosition = (int) _positionField.GetValue(_component);
	    modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
	}

	private IEnumerable ToggleNotes(string[] notes)
	{
		foreach (string note in notes)
		{
			int notePosition = Array.IndexOf(noteIndexes, note);
			while (currentPosition != notePosition)
			{
				DoInteractionClick(_wheelButton);
				currentPosition = (currentPosition + 1) % 12;

				yield return new WaitForSeconds(0.1f);
			}

			DoInteractionClick(_selectButton);
			yield return new WaitForSeconds(0.1f);
		}
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		var commands = inputCommand.ToLowerInvariant().Replace('♯', '#').Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if (commands.Length == 5 && (commands[0].Equals("submit") || commands[0].Equals("play")))
		{
			string[] notes = commands.Where((_, i) => i > 0).ToArray();
			if (notes.All(note => Array.IndexOf(noteIndexes, note) > -1))
			{
				if (notes.Distinct().Count() == 4)
				{
					yield return null;

					if (previousNotes != null) // Reset the previously set notes.
					{
						foreach (object obj in ToggleNotes(previousNotes)) yield return obj;
						previousNotes = null;
					}

					foreach (object obj in ToggleNotes(notes)) yield return obj;

					int lastStrikeCount = StrikeCount;
					
					DoInteractionClick(_submitButton);
					yield return new WaitForSeconds(0.8f);

					if (lastStrikeCount != StrikeCount)
					{
						previousNotes = notes;
					}
				}
			}
		}
	}

	static ChordQualitiesComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("ChordQualities");
		_wheelButtonField = _componentType.GetField("WheelButton", BindingFlags.Public | BindingFlags.Instance);
		_selectButtonField = _componentType.GetField("SelectButton", BindingFlags.Public | BindingFlags.Instance);
		_submitButtonField = _componentType.GetField("SubmitButton", BindingFlags.Public | BindingFlags.Instance);
		_positionField = _componentType.GetField("position", BindingFlags.NonPublic | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static FieldInfo _wheelButtonField = null;
	private static FieldInfo _selectButtonField = null;
	private static FieldInfo _submitButtonField = null;
	private static FieldInfo _positionField = null;

	private static string[] noteIndexes = { "a", "a#", "b", "c", "c#", "d", "d#", "e", "f", "f#", "g", "g#" };
	private int currentPosition = 0;
	private string[] previousNotes = null;

	private KMSelectable _wheelButton = null;
	private KMSelectable _selectButton = null;
	private KMSelectable _submitButton = null;
}
