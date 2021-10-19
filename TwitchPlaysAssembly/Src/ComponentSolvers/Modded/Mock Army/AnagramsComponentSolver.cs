using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AnagramsComponentSolver : ComponentSolver
{
	public AnagramsComponentSolver(TwitchModule module) :
		base(module)
	{
		_buttons = module.BombComponent.GetComponent<KMSelectable>().Children;
		string modType = GetModuleType();
		ModInfo = ComponentSolverFactory.GetModuleInfo(modType, "Submit your answer with !{0} submit poodle", null);
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.Trim();
		object[] anagramWords =
		{
			"stream", "master", "tamers", "looped", "poodle", "pooled",
			"cellar", "caller", "recall", "seated", "sedate", "teased",
			"rescue", "secure", "recuse", "rashes", "shears", "shares",
			"barely", "barley", "bleary", "duster", "rusted", "rudest"
		};

		object[] wordscrambleWords =
		{
			"archer", "attack", "banana", "blasts", "bursts", "button",
			"cannon", "casing", "charge", "damage", "defuse", "device",
			"disarm", "flames", "kaboom", "kevlar", "keypad", "letter",
			"module", "mortar", "napalm", "ottawa", "person", "robots",
			"rocket", "sapper", "semtex", "weapon", "widget", "wiring",
		};

		List<KMSelectable> buttons = new List<KMSelectable>();
		List<string> buttonLabels = _buttons.Select(button => button.GetComponentInChildren<TextMesh>().text.ToLowerInvariant()).ToList();
		if (!inputCommand.StartsWith("submit ", System.StringComparison.InvariantCultureIgnoreCase)) yield break;
		inputCommand = inputCommand.Substring(7).ToLowerInvariant();
		foreach (char c in inputCommand)
		{
			int index = buttonLabels.IndexOf(c.ToString());
			if (index < 0)
			{
				if (!inputCommand.EqualsAny(anagramWords) && !inputCommand.EqualsAny(wordscrambleWords)) yield break;
				yield return null;
				yield return "unsubmittablepenalty";
				yield break;
			}
			buttons.Add(_buttons[index]);
		}

		if (buttons.Count != 6) yield break;

		yield return null;
		yield return DoInteractionClick(_buttons[3]);
		foreach (KMSelectable b in buttons)
		{
			yield return DoInteractionClick(b);
		}
		yield return DoInteractionClick(_buttons[7]);
	}

	private readonly KMSelectable[] _buttons;
}