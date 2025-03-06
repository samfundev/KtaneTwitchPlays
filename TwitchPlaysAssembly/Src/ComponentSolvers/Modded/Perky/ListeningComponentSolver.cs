using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[ModuleID("Listening")]
public class ListeningComponentSolver : ComponentSolver
{
	public ListeningComponentSolver(TwitchModule module) :
		base(module)
	{
		Component component = module.BombComponent.GetComponent("Listening");
		if (component == null)
		{
			throw new NotSupportedException("Could not get Listening Component from bombComponent");
		}
		_component = component;

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

		SetHelpMessage("Listen to the sound with !{0} press play. Enter the response with !{0} press $ & * * #.");
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

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		if (_component == null)
			yield break;
		while (!_component.GetValue<bool>("isActivated"))
			yield return true;
		int index = _component.GetValue<int>("codeInputPosition");
		char[] input = _component.GetValue<char[]>("codeInput");
		string ans = _component.GetValue<object>("sound").GetValue<string>("code");
		for (int i = 0; i < index; i++)
		{
			if (ans[i] != input[i])
			{
				while (!_component.GetValue<bool>("canPlayAgain"))
					yield return true;
				yield return DoInteractionClick(_play);
				index = 0;
				break;
			}
		}
		char[] btnLabels = { '$', '#', '*', '&' };
		for (int i = index; i < 5; i++)
			yield return DoInteractionClick(_buttons[Array.IndexOf(btnLabels, ans[i])]);
	}

	private readonly MonoBehaviour _play;
	private readonly MonoBehaviour[] _buttons;
	private readonly object _component;
}
