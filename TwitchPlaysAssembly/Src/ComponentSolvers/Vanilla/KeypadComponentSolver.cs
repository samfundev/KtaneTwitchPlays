using System;
using System.Collections;
using System.Text.RegularExpressions;
using Assets.Scripts.Rules;

public class KeypadComponentSolver : ComponentSolver
{
	public KeypadComponentSolver(TwitchModule module) :
		base(module)
	{
		_buttons = ((KeypadComponent) module.BombComponent).buttons;
		SetHelpMessage("!{0} press 3 1 2 4 | The buttons are 1=TL, 2=TR, 3=BL, 4=BR");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.Trim();
		if (!inputCommand.StartsWith("press ", StringComparison.InvariantCultureIgnoreCase))
			yield break;
		inputCommand = inputCommand.Substring(6);

		foreach (Match buttonIndexString in Regex.Matches(inputCommand, @"[1-4]"))
		{
			if (!int.TryParse(buttonIndexString.Value, out int buttonIndex))
				continue;

			buttonIndex--;

			if (buttonIndex < 0 || buttonIndex >= _buttons.Length) continue;
			if (_buttons[buttonIndex].IsStayingDown)
				continue;

			yield return buttonIndexString.Value;
			yield return "trycancel";
			yield return DoInteractionClick(_buttons[buttonIndex]);
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		while (!Module.Solved)
			yield return DoInteractionClick(_buttons[
				RuleManager.Instance.KeypadRuleSet.GetNextSolutionIndex(((KeypadComponent) Module.BombComponent).pListIndex,
					_buttons)]);
	}

	private readonly KeypadButton[] _buttons;
}
