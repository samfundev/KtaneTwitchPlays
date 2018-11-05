using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Rules;

public class WireSetComponentSolver : ComponentSolver
{
	public WireSetComponentSolver(TwitchModule module) :
		base(module)
	{
		_wires = ((WireSetComponent) module.BombComponent).wires;
		ModInfo = ComponentSolverFactory.GetModuleInfo("WireSetComponentSolver", "!{0} cut 3 [cut wire 3] | Wires are ordered from top to bottom | Empty spaces are not counted");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.Trim();
		if (!inputCommand.StartsWith("cut ", StringComparison.InvariantCultureIgnoreCase))
			yield break;
		inputCommand = inputCommand.Substring(4);

		if (!int.TryParse(inputCommand, out int wireIndex) || wireIndex < 1 || wireIndex > _wires.Count) yield break;

		yield return null;
		yield return DoInteractionClick(_wires[wireIndex - 1]);
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		yield return DoInteractionClick(_wires[RuleManager.Instance.WireRuleSet.GetSolutionIndex((WireSetComponent) Module.BombComponent)]);
	}

	private readonly List<SnippableWire> _wires;
}
