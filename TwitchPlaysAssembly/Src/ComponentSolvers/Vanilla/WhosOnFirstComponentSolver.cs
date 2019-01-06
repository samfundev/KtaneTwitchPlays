using System;
using System.Collections;
using System.Linq;
using Assets.Scripts.Rules;
using UnityEngine;

public class WhosOnFirstComponentSolver : ComponentSolver
{
	public WhosOnFirstComponentSolver(TwitchModule module) :
		base(module)
	{
		_buttons = ((WhosOnFirstComponent) module.BombComponent).Buttons;
		ModInfo = ComponentSolverFactory.GetModuleInfo("WhosOnFirstComponentSolver", "!{0} what? [press the button that says \"WHAT?\"] | !{0} press 3 [press the third button in english reading order] | The phrase must match exactly | Not case sensitive", "Who%E2%80%99s on First");
	}

	private static readonly string[] Phrases = { "ready", "first", "no", "blank", "nothing", "yes", "what", "uhhh", "left", "right", "middle", "okay", "wait", "press", "you", "you are", "your", "you're", "ur", "u", "uh huh", "uh uh", "what?", "done", "next", "hold", "sure", "like" };

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.ToLowerInvariant().Trim();

		string[] split = inputCommand.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (split.Length == 2 && split[0] == "press" && int.TryParse(split[1], out int buttonIndex) && buttonIndex > 0 && buttonIndex < 7)
		{
			_buttons[buttonIndex - 1].Interact();
		}
		else
		{
			if (!Phrases.Contains(inputCommand))
			{
				yield return null;
				yield return $"sendtochaterror The word \"{inputCommand}\" isn't a valid word.";
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
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		while (!Module.BombComponent.IsActive)
			yield return true;
		var wof = (WhosOnFirstComponent) Module.BombComponent;
		while (!Module.Solved)
		{
			while (!wof.ButtonsEmerged || wof.CurrentDisplayWordIndex < 0)
				yield return true;
			string displayText = wof.DisplayText.text;
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
