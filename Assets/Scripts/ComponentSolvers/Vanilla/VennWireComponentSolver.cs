using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class VennWireComponentSolver : ComponentSolver
{
    public VennWireComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _wires = (Array)_activeWiresProperty.GetValue(bombComponent, null);
        _cutWires = new bool[6];
        
        helpMessage = "!{0} cut 3 [cut wire 3] | !{0} cut 2 3 6 [cut multiple wires] | Wires are ordered from left to right | Empty spaces are not counted"; 
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        if (!inputCommand.StartsWith("cut ", StringComparison.InvariantCultureIgnoreCase))
        {
            yield break;
        }
        inputCommand = inputCommand.Substring(4);

        int beforeButtonStrikeCount = StrikeCount;

        foreach (Match wireIndexString in Regex.Matches(inputCommand, @"[1-6]"))
        {
            int wireIndex = 0;
            if (!int.TryParse(wireIndexString.Value, out wireIndex))
            {
                continue;
            }

            wireIndex--;

            if (wireIndex >= 0 && wireIndex < _wires.Length)
            {
                if (_cutWires[wireIndex])
                    continue;
                _cutWires[wireIndex] = true;

                yield return wireIndexString.Value;

                if (Canceller.ShouldCancel)
                {
                    Canceller.ResetCancel();
                    yield break;
                }

                MonoBehaviour wire = (MonoBehaviour)_wires.GetValue(wireIndex);

                DoInteractionStart(wire);
                yield return new WaitForSeconds(0.1f);
                DoInteractionEnd(wire);

                //Escape the sequence if a part of the given sequence is wrong
                if (StrikeCount != beforeButtonStrikeCount || Solved)
                {
                    break;
                }
            }
        }
    }

    static VennWireComponentSolver()
    {
        _vennWireComponentType = ReflectionHelper.FindType("Assets.Scripts.Components.VennWire.VennWireComponent");
        _activeWiresProperty = _vennWireComponentType.GetProperty("ActiveWires", BindingFlags.Public | BindingFlags.Instance);
    }

    private static Type _vennWireComponentType = null;
    private static PropertyInfo _activeWiresProperty = null;

    private Array _wires = null;
    private bool[] _cutWires = null;
}
