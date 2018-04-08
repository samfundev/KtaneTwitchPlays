using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnagramsComponentSolver : ComponentSolver
{
	public AnagramsComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		_buttons = bombComponent.GetComponent<KMSelectable>().Children;
		string modType = GetModuleType();
		modInfo = ComponentSolverFactory.GetModuleInfo(modType, "Submit your answer with !{0} submit poodle", null, modType.Equals("AnagramsModule"));
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
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
		if (inputCommand.StartsWith("submit ", System.StringComparison.InvariantCultureIgnoreCase))
		{
			inputCommand = inputCommand.Substring(7).ToLowerInvariant();
			foreach (char c in inputCommand)
			{
				int index = buttonLabels.IndexOf(c.ToString());
				if (index < 0)
				{
					if (inputCommand.EqualsAny(anagramWords) || inputCommand.EqualsAny(wordscrambleWords))
					{
						yield return null;
						yield return "unsubmittablepenalty";
					}
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
		
	}

	private KMSelectable[] _buttons = null;
}
