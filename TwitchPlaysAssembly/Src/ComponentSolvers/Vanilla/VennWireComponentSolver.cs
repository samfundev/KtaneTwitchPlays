using Assets.Scripts.Components.VennWire;
using System;
using System.Collections;
using System.Text.RegularExpressions;

public class VennWireComponentSolver : ComponentSolver
{
    public VennWireComponentSolver(BombCommander bombCommander, VennWireComponent bombComponent) :
        base(bombCommander, bombComponent)
	{
        _wires = bombComponent.ActiveWires;
        modInfo = ComponentSolverFactory.GetModuleInfo("VennWireComponentSolver");
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
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

            if (wireIndex >= 0 && wireIndex < _wires.Length)
            {
                if (_wires[wireIndex].Snipped)
                    continue;

                yield return wireIndexString.Value;

	            yield return "trycancel";
                VennSnippableWire wire = _wires[wireIndex];
                yield return DoInteractionClick(wire, string.Format("cutting wire {0}", wireIndexString.Value));
            }
        }
    }

    private VennSnippableWire[] _wires = null;
}
