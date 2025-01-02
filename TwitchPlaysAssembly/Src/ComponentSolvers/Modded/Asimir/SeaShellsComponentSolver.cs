using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class SeaShellsComponentSolver : ComponentSolver
{
	public SeaShellsComponentSolver(TwitchModule module) :
		base(module)
	{
		_component = module.BombComponent.GetComponent(ComponentType);
		_buttons = (KMSelectable[]) ButtonsField.GetValue(_component);
		SetHelpMessage("Press buttons by their labels by typing !{0} label alar llama. You can submit partial text as long it only matches one button. Press buttons by their position using !{0} position 3 5 2 1 4. NOTE: Each button press is separated by a space so typing \"burglar alarm\" will press a button twice.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] commands = inputCommand.ToLowerInvariant().Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if (commands.Length < 2) yield break;
		IEnumerable<string> buttonLabels = _buttons.Select(button => button.GetComponentInChildren<TextMesh>().text.ToLowerInvariant()).ToList();

		if (buttonLabels.Any(label => label == " ")) yield break;

		IEnumerable<string> submittedText = commands.Skip(1);
		List<KMSelectable> selectables = new List<KMSelectable>();

		if (commands[0].EqualsAny("press", "label", "lab", "l"))
		{
			foreach (string text in submittedText)
			{
				IEnumerable<string> matchingLabels = buttonLabels.Where(label => label.Contains(text)).ToList();
				string fixedText = text;
				if (!buttonLabels.Contains(text))
				{
					switch (matchingLabels.Count())
					{
						case 1:
							fixedText = matchingLabels.First();
							break;
						case 0:
							yield return $"sendtochaterror!f There isn't any label that contains \"{text}\".";
							yield break;
						default:
							yield return
								$"sendtochaterror!f There are multiple labels that contain \"{text}\": {string.Join(", ", matchingLabels.ToArray())}.";
							yield break;
					}
				}

				selectables.Add(_buttons[buttonLabels.IndexOf(label => label == fixedText)]);
			}
		}
		else if (commands[0].EqualsAny("position", "pos", "index", "ind", "i"))
		{
			foreach (string text in submittedText)
			{
				if (int.TryParse(text, out int index))
				{
					if (index < 1 || index > 5) yield break;

					selectables.Add(_buttons[index - 1]);
				}
			}
		}
		else
		{
			yield break;
		}

		int startingStage = (int) StageField.GetValue(_component);
		foreach (KMSelectable selectable in selectables)
		{
			yield return null;
			yield return DoInteractionClick(selectable);

			if (startingStage != (int) StageField.GetValue(_component))
				yield break;
		}
	}

	private static readonly Type ComponentType = ReflectionHelper.FindType("SeaShellsModule");
	private static readonly FieldInfo ButtonsField = ComponentType.GetField("buttons", BindingFlags.Public | BindingFlags.Instance);
	private static readonly FieldInfo StageField = ComponentType.GetField("stage", BindingFlags.NonPublic | BindingFlags.Instance);

	private readonly object _component;
	private readonly KMSelectable[] _buttons;
}
