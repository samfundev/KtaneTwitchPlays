using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class WireSetComponentSolver : ComponentSolver
{
    public WireSetComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _wires = (IList)_wiresField.GetValue(bombComponent);
        
        helpMessage = "!{0} cut 3 [cut wire 3] | Wires are ordered from top to bottom | Empty spaces are not counted";
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        if (!inputCommand.StartsWith("cut ", StringComparison.InvariantCultureIgnoreCase))
        {
            yield break;
        }
        inputCommand = inputCommand.Substring(4);

        int wireIndex = 0;
        if (!int.TryParse(inputCommand, out wireIndex))
        {
            yield break;
        }

        wireIndex--;

        if (wireIndex >= 0 && wireIndex < _wires.Count)
        {
            yield return inputCommand;

            MonoBehaviour wireToCut = (MonoBehaviour)_wires[wireIndex];
            DoInteractionStart(wireToCut);
            yield return new WaitForSeconds(0.1f);
            DoInteractionEnd(wireToCut);
        }
    }

    static WireSetComponentSolver()
    {
        _wireSetComponentType = ReflectionHelper.FindType("WireSetComponent");
        _wiresField = _wireSetComponentType.GetField("wires", BindingFlags.Public | BindingFlags.Instance);
    }

    private static Type _wireSetComponentType = null;
    private static FieldInfo _wiresField = null;

    private IList _wires = null;
}
