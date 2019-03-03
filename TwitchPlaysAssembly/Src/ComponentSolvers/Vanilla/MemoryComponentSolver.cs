using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Rules;

public class MemoryComponentSolver : ComponentSolver
{
	public MemoryComponentSolver(TwitchModule module) :
		base(module)
	{
		_buttons = ((MemoryComponent) module.BombComponent).Buttons;
		ModInfo = ComponentSolverFactory.GetModuleInfo("MemoryComponentSolver", "!{0} position 2, !{0} pos 2, !{0} p 2 [2nd position] | !{0} label 3, !{0} lab 3, !{0} l 3 [label 3]");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.Trim();
		string[] commandParts = inputCommand.ToLowerInvariant().Split(' ');

		if (commandParts.Length != 2)
			yield break;

		if (!int.TryParse(commandParts[1], out int buttonNumber))
			yield break;

		if (buttonNumber < 1 || buttonNumber > 4) yield break;
		if (commandParts[0].EqualsAny("position", "pos", "p"))
		{
			yield return "position";

			yield return DoInteractionClick(_buttons[buttonNumber - 1]);
		}
		else if (commandParts[0].EqualsAny("label", "lab", "l"))
		{
			foreach (KeypadButton button in _buttons)
			{
				if (!button.Text.text.Equals(buttonNumber.ToString())) continue;
				yield return "label";
				yield return DoInteractionClick(button);
				break;
			}
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		MemoryComponent mc = (MemoryComponent) Module.BombComponent;
		while (!Module.BombComponent.IsActive) yield return true;
		while (!Module.Solved)
		{
			while (!mc.IsInputValid) yield return true;
			List<Rule> ruleList = RuleManager.Instance.MemoryRuleSet.RulesDictionary[mc.CurrentStage];
			yield return DoInteractionClick(_buttons[RuleManager.Instance.MemoryRuleSet.ExecuteRuleList(mc, ruleList)]);
		}
	}

	private readonly KeypadButton[] _buttons;
}
