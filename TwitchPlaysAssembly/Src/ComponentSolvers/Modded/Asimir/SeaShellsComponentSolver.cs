using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeaShellsComponentSolver : ComponentSolver
{
	public SeaShellsComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		_component = bombComponent.GetComponent(_componentType);
		_buttons = (KMSelectable[]) _buttonsField.GetValue(_component);
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Press buttons by typing !{0} press alar llama. You can submit partial text as long it only matches one button. NOTE: Each button press is separated by a space so typing \"burglar alarm\" will press a button twice.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string[] commands = inputCommand.ToLowerInvariant().Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		if (commands.Length < 2 || !commands[0].Equals("press")) yield break;
		IEnumerable<string> buttonLabels = _buttons.Select(button => button.GetComponentInChildren<TextMesh>().text.ToLowerInvariant());

		if (buttonLabels.Any(label => label == " ")) yield break;
		{
			yield return null;

			IEnumerable<string> submittedText = commands.Skip(1);
			List<string> fixedLabels = new List<string>();
			foreach (string text in submittedText)
			{
				IEnumerable<string> matchingLabels = buttonLabels.Where(label => label.Contains(text));

				int matchedCount = matchingLabels.Count();
				if (buttonLabels.Contains(text))
				{
					fixedLabels.Add(text);
				}
				else switch (matchedCount)
				{
					case 1:
						fixedLabels.Add(matchingLabels.First());
						break;
					case 0:
						yield return $"sendtochaterror There isn't any label that contains \"{text}\".";
						yield break;
					default:
						yield return
							$"sendtochaterror There are multiple labels that contain \"{text}\": {string.Join(", ", matchingLabels.ToArray())}.";
						yield break;
				}
			}
				
			int startingStage = (int) _stageField.GetValue(_component);
			foreach (string fixedLabel in fixedLabels)
			{
				KMSelectable button = _buttons[buttonLabels.ToList().IndexOf(fixedLabel)];
				DoInteractionClick(button);

				yield return new WaitForSeconds(0.1f);

				if (startingStage != (int) _stageField.GetValue(_component))
					yield break;
			}
		}
	}

	static SeaShellsComponentSolver()
	{
		_componentType = ReflectionHelper.FindType("SeaShellsModule");
		_buttonsField = _componentType.GetField("buttons", BindingFlags.Public | BindingFlags.Instance);
		_stageField = _componentType.GetField("stage", BindingFlags.NonPublic | BindingFlags.Instance);
	}

	private static Type _componentType = null;
	private static FieldInfo _buttonsField = null;
	private static FieldInfo _stageField = null;

	private readonly object _component = null;
	private KMSelectable[] _buttons = null;
}
