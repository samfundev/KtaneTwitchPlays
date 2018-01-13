using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class WireSequenceComponentSolver : ComponentSolver
{
    public WireSequenceComponentSolver(BombCommander bombCommander, WireSequenceComponent bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _wireSequence = (List<WireSequenceComponent.WireConfiguration>) _wireSequenceField.GetValue(bombComponent);
		_upButton = bombComponent.UpButton;
		_downButton = bombComponent.DownButton;
        modInfo = ComponentSolverFactory.GetModuleInfo("WireSequenceComponentSolver");
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        inputCommand = inputCommand.ToLowerInvariant();
        List<MonoBehaviour> buttons = new List<MonoBehaviour>();
        List<string> strikemessages = new List<string>();

        if (inputCommand.EqualsAny("up", "u"))
        {
            yield return "up";
            yield return DoInteractionClick(_upButton);
        }
        else if (inputCommand.EqualsAny("down", "d"))
        {
            yield return "down";
            yield return DoInteractionClick(_downButton, "attempting to move down.");
        }
        else
        {
            if (!inputCommand.StartsWith("cut ", StringComparison.InvariantCultureIgnoreCase) &&
                !inputCommand.StartsWith("c ", StringComparison.InvariantCultureIgnoreCase))
            {
                yield break;
            }
            string[] sequence = inputCommand.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string wireIndexString in sequence.Skip(1))
            {
                if (wireIndexString.EqualsAny("up", "u"))
                {
                    buttons.Add(_upButton);
                    strikemessages.Add("This will never cause a strike Kappa");
                    break;
                }

                if (wireIndexString.EqualsAny("down", "d"))
                {
                    buttons.Add(_downButton);
                    strikemessages.Add("attempting to move down.");
                    break;
                }

                int wireIndex;
                if (!int.TryParse(wireIndexString, out wireIndex)) yield break;

                wireIndex--;
                if (!CanInteractWithWire(wireIndex)) yield break;

                WireSequenceWire wire = GetWire(wireIndex);
                if (wire == null) yield break;
                buttons.Add(wire);
                strikemessages.Add(string.Format("cutting Wire {0}.", wireIndex + 1));
            }

            yield return "wire sequence";
            for (int i = 0; i < buttons.Count; i++)
            {
                yield return DoInteractionClick(buttons[i], strikemessages[i]);
                if (Canceller.ShouldCancel)
                {
                    Canceller.ResetCancel();
                    yield break;
                }
            }
            if (Canceller.ShouldCancel)
                Canceller.ResetCancel();
        }
    }

    private bool CanInteractWithWire(int wireIndex)
    {
        int wirePageIndex = wireIndex / 3;
        return wirePageIndex == (int)_currentPageField.GetValue(BombComponent);
    }

    private WireSequenceWire GetWire(int wireIndex)
    {
		return _wireSequence[wireIndex].Wire;
    }

    static WireSequenceComponentSolver()
    {
		_wireSequenceComponentType = typeof(WireSequenceComponent);
        _wireSequenceField = _wireSequenceComponentType.GetField("wireSequence", BindingFlags.NonPublic | BindingFlags.Instance);
        _currentPageField = _wireSequenceComponentType.GetField("currentPage", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private static Type _wireSequenceComponentType = null;
    private static FieldInfo _wireSequenceField = null;
    private static FieldInfo _currentPageField = null;

    private List<WireSequenceComponent.WireConfiguration> _wireSequence = null;
    private Selectable _upButton = null;
    private Selectable _downButton = null;
}
