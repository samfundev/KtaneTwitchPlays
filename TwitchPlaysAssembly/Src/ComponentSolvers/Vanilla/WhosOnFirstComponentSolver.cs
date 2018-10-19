using Assets.Scripts.Rules;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class WhosOnFirstComponentSolver : ComponentSolver
{
	public WhosOnFirstComponentSolver(BombCommander bombCommander, WhosOnFirstComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		_buttons = bombComponent.Buttons;
		ModInfo = ComponentSolverFactory.GetModuleInfo("WhosOnFirstComponentSolver", "!{0} what? [press the button that says \"WHAT?\"] | The phrase must match exactly | Not case sensitive", "Who%E2%80%99s on First");
	}

	private static readonly string[] Phrases = { "ready", "first", "no", "blank", "nothing", "yes", "what", "uhhh", "left", "right", "middle", "okay", "wait", "press", "you", "you are", "your", "you're", "ur", "u", "uh huh", "uh uh", "what?", "done", "next", "hold", "sure", "like" };

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		string word = inputCommand.ToLowerInvariant().Trim();
		if (!Phrases.Contains(word))
		{
			yield return null;
			yield return $"sendtochaterror The word \"{word}\" isn't a valid word.";
			yield break;
		}

		foreach (KeypadButton button in _buttons)
		{
			if (!inputCommand.Equals(button.GetText(), StringComparison.InvariantCultureIgnoreCase)) continue;
			yield return null;
			button.Interact();
			yield return new WaitForSeconds(0.1f);
			yield break;
		}

		yield return null;
		yield return "unsubmittablepenalty";
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		while (!BombComponent.IsActive)
			yield return true;
		while (!BombComponent.IsSolved)
		{
			while (!((WhosOnFirstComponent) BombComponent).ButtonsEmerged || ((WhosOnFirstComponent) BombComponent).CurrentDisplayWordIndex < 0)
				yield return true;
			string displayText = ((WhosOnFirstComponent) BombComponent).DisplayText.text;
			var buttonText = _buttons.Select(x => x.GetText()).ToList();

			var precedenceList = RuleManager.Instance.WhosOnFirstRuleSet.precedenceMap[buttonText[RuleManager.Instance.WhosOnFirstRuleSet.displayWordToButtonIndexMap[displayText]]];
			int index = int.MaxValue;
			for (int i = 0; i < 6; i++)
			{
				if (precedenceList.IndexOf(buttonText[i]) < index)
					index = precedenceList.IndexOf(buttonText[i]);
			}
			yield return DoInteractionClick(_buttons[buttonText.IndexOf(precedenceList[index])]);
		}
	}

	private readonly KeypadButton[] _buttons;
}
