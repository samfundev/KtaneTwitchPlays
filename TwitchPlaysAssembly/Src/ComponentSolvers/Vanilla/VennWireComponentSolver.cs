using System;
using System.Collections;
using System.Text.RegularExpressions;
using Assets.Scripts.Components.VennWire;
using Assets.Scripts.Rules;

public class VennWireComponentSolver : ComponentSolver
{
	public VennWireComponentSolver(TwitchModule module) :
		base(module)
	{
		_wires = ((VennWireComponent) module.BombComponent).ActiveWires;
		ModInfo = ComponentSolverFactory.GetModuleInfo("Venn", "!{0} cut 3 [cut wire 3] | !{0} cut 2 3 6 [cut multiple wires] | Wires are ordered from left to right | Empty spaces are not counted");
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.Trim();
		if (!inputCommand.StartsWith("cut ", StringComparison.InvariantCultureIgnoreCase))
		{
			yield break;
		}
		inputCommand = inputCommand.Substring(4);

		foreach (Match wireIndexString in Regex.Matches(inputCommand, @"[1-6]"))
		{
			if (!int.TryParse(wireIndexString.Value, out int wireIndex))
			{
				continue;
			}
			wireIndex--;

			if (wireIndex < 0 || wireIndex >= _wires.Length) continue;
			if (_wires[wireIndex].Snipped)
				continue;

			yield return wireIndexString.Value;

			yield return "trycancel";
			VennSnippableWire wire = _wires[wireIndex];
			yield return DoInteractionClick(wire, $"cutting wire {wireIndexString.Value}");
		}
	}

	protected override IEnumerator ForcedSolveIEnumerator()
	{
		yield return null;
		VennWireComponent vwc = (VennWireComponent) Module.BombComponent;
		VennWireRuleSet ruleSet = RuleManager.Instance.VennWireRuleSet;
		foreach (VennSnippableWire wire in _wires)
		{
			if (ruleSet.ShouldWireBeSnipped(vwc, wire.WireIndex, false) && !wire.Snipped)
				yield return DoInteractionClick(wire);
		}
	}

	private readonly VennSnippableWire[] _wires;
}
