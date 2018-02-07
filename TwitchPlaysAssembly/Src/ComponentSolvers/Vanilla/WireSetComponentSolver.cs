using System;
using System.Collections;
using System.Collections.Generic;

public class WireSetComponentSolver : ComponentSolver
{
    public WireSetComponentSolver(BombCommander bombCommander, WireSetComponent bombComponent, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, canceller)
	{
        _wires = bombComponent.wires;
        modInfo = ComponentSolverFactory.GetModuleInfo("WireSetComponentSolver");
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        if (!inputCommand.StartsWith("cut ", StringComparison.InvariantCultureIgnoreCase))
        {
            yield break;
        }
        inputCommand = inputCommand.Substring(4);

        if (!int.TryParse(inputCommand, out int wireIndex) || wireIndex < 1 || wireIndex > _wires.Count) yield break;

		yield return null;
		yield return DoInteractionClick(_wires[wireIndex - 1]);
    }

	private List<SnippableWire> _wires;
}
