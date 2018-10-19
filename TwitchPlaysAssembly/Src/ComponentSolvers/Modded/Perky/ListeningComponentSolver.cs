using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class ListeningComponentSolver : ComponentSolver
{
	public ListeningComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		Component component = bombComponent.GetComponent("Listening");
		if (component == null)
		{
			throw new NotSupportedException("Could not get Listening Component from bombComponent");
		}

		Type componentType = component.GetType();
		if (componentType == null)
		{
			throw new NotSupportedException("Could not get componentType from Listening Component");
		}

		FieldInfo playField = componentType.GetField("PlayButton", BindingFlags.Public | BindingFlags.Instance);
		FieldInfo dollarField = componentType.GetField("DollarButton", BindingFlags.Public | BindingFlags.Instance);
		FieldInfo poundField = componentType.GetField("PoundButton", BindingFlags.Public | BindingFlags.Instance);
		FieldInfo starField = componentType.GetField("StarButton", BindingFlags.Public | BindingFlags.Instance);
		FieldInfo ampersandField = componentType.GetField("AmpersandButton", BindingFlags.Public | BindingFlags.Instance);
		if (playField == null || dollarField == null || poundField == null || starField == null || ampersandField == null)
		{
			throw new NotSupportedException("Could not find the KMSelectable fields in component Type");
		}

		_buttons = new MonoBehaviour[4];
		_play = (MonoBehaviour) playField.GetValue(component);
		_buttons[0] = (MonoBehaviour) dollarField.GetValue(component);
		_buttons[1] = (MonoBehaviour) poundField.GetValue(component);
		_buttons[2] = (MonoBehaviour) starField.GetValue(component);
		_buttons[3] = (MonoBehaviour) ampersandField.GetValue(component);
		if (_play == null || _buttons.Any(x => x == null))
		{
			throw new NotSupportedException("Component had null KMSelectables.");
		}

		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "Listen to the sound with !{0} press play. Enter the response with !{0} press $ & * * #.");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.Trim();
		if (inputCommand.ToLowerInvariant().EqualsAny("play", "press play"))
		{
			yield return null;
			yield return DoInteractionClick(_play);
			yield break;
		}

		string[] split = inputCommand.Trim().ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (split.Length < 2 || split[0] != "press")
			yield break;

		string buttonLabels = _buttons.Select(button => button.GetComponentInChildren<TextMesh>().text.ToLower()).Join(string.Empty);
		List<int> buttons = (from cmd in split.Skip(1) from x in cmd select buttonLabels.IndexOf(x)).ToList();
		if (buttons.Any(x => x == -1)) yield break;
		//Check for any invalid commands.  Abort entire sequence if any invalid commands are present.

		yield return "Listening Solve Attempt";
		foreach (int button in buttons)
			yield return DoInteractionClick(_buttons[button]);
	}

	private readonly MonoBehaviour _play;
	private readonly MonoBehaviour[] _buttons;
}
