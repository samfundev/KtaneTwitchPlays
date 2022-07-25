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
		_component = Module.BombComponent.GetComponent(ReflectionHelper.FindType(modType));
		ModInfo = ComponentSolverFactory.GetModuleInfo(modType, "Submit your answer with !{0} submit poodle");
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

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		string curr = _component.GetValue<TextMesh>("AnswerDisplay").text;
		IList ans = new List<string>();
		if (GetModuleType().Equals("AnagramsModule"))
		{
			IList temp = _component.GetValue<IList>("_solution");
			for (int i = 0; i < temp.Count; i++)
				ans.Add(temp[i]);
		}
		else
			ans.Add(_component.GetValue<string>("_solution"));
		if (curr.Length > 6)
		{
			yield return DoInteractionClick(_buttons[3]);
			curr = "";
		}
		for (int j = 0; j < ans.Count; j++)
		{
			for (int i = 0; i < curr.Length; i++)
			{
				if (curr[i] != ans[j].ToString()[i])
				{
					ans.RemoveAt(j);
					j--;
					break;
				}
			}
		}
		if (ans.Count == 0)
		{
			yield return DoInteractionClick(_buttons[3]);
			if (GetModuleType().Equals("AnagramsModule"))
				ans = _component.GetValue<IList>("_solution");
			else
				ans.Add(_component.GetValue<string>("_solution"));
			curr = "";
		}
		int start = curr.Length;
		int ansIndex = Random.Range(0, ans.Count);
		for (int j = start; j < 6; j++)
			yield return DoInteractionClick(_buttons.Where(button => button.GetComponentInChildren<TextMesh>().text.ToLowerInvariant() == ans[ansIndex].ToString()[j].ToString().ToLowerInvariant()).ToList()[0]);
		yield return DoInteractionClick(_buttons[7], 0);
	}

	private readonly object _component;
	private readonly KMSelectable[] _buttons;
}