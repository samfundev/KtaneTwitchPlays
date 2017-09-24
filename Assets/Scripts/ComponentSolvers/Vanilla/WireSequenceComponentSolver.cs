using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class WireSequenceComponentSolver : ComponentSolver
{
    public WireSequenceComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _wireSequence = (IList)_wireSequenceField.GetValue(bombComponent);
        _upButton = (MonoBehaviour)_upButtonField.GetValue(bombComponent);
        _downButton = (MonoBehaviour)_downButtonField.GetValue(bombComponent);
        modInfo = ComponentSolverFactory.GetModuleInfo("WireSequenceComponentSolver");
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        List<MonoBehaviour> buttons = new List<MonoBehaviour>();
        List<string> strikemessages = new List<string>();

        if (inputCommand.Equals("up", StringComparison.InvariantCultureIgnoreCase) ||
            inputCommand.Equals("u", StringComparison.InvariantCultureIgnoreCase))
        {
            yield return "up";
            yield return DoInteractionClick(_upButton);
        }
        else if (inputCommand.Equals("down", StringComparison.InvariantCultureIgnoreCase) ||
                inputCommand.Equals("d", StringComparison.InvariantCultureIgnoreCase))
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
                Debug.LogFormat("Wire Sequence Solver: '{0}'",wireIndexString);
                if (wireIndexString.Equals("up", StringComparison.InvariantCultureIgnoreCase) ||
                    wireIndexString.Equals("u", StringComparison.InvariantCultureIgnoreCase))
                {
                    buttons.Add(_upButton);
                    strikemessages.Add("strikemessage This will never cause a strike Kappa");
                    break;
                }

                if (wireIndexString.Equals("down", StringComparison.InvariantCultureIgnoreCase) ||
                    wireIndexString.Equals("d", StringComparison.InvariantCultureIgnoreCase))
                {
                    buttons.Add(_downButton);
                    strikemessages.Add("strikemessage attempting to move down.");
                    break;
                }

                int wireIndex;
                if (!int.TryParse(wireIndexString, out wireIndex))
                {
                    Debug.Log("Invalid Integer - Aborting");
                    yield break;
                }
                wireIndex--;
                if (!CanInteractWithWire(wireIndex))
                {
                    Debug.LogFormat("Cannot Interact with wire {0} as it doesn't exist on current page. Aborting.", wireIndex + 1);
                    yield break;
                }

                MonoBehaviour wire = GetWire(wireIndex);
                if (wire == null)
                {
                    Debug.LogFormat("Wire {0} doesn't exist. Aborting.", wireIndex + 1);
                    yield break;
                }
                buttons.Add(wire);
                strikemessages.Add(string.Format("strikemessage cutting Wire {0}.", wireIndex + 1));
            }

            yield return "wire sequence";
            for (int i = 0; i < buttons.Count; i++)
            {
                Debug.LogFormat("Interaction {0}/{1} - Strike message: {2}", i + 1, buttons.Count, strikemessages[i]);
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

    private MonoBehaviour GetWire(int wireIndex)
    {
        return (MonoBehaviour)_wireField.GetValue(_wireSequence[wireIndex]);
    }

    static WireSequenceComponentSolver()
    {
        _wireSequenceComponentType = ReflectionHelper.FindType("WireSequenceComponent");
        _wireSequenceField = _wireSequenceComponentType.GetField("wireSequence", BindingFlags.NonPublic | BindingFlags.Instance);
        _currentPageField = _wireSequenceComponentType.GetField("currentPage", BindingFlags.NonPublic | BindingFlags.Instance);
        _upButtonField = _wireSequenceComponentType.GetField("UpButton", BindingFlags.Public | BindingFlags.Instance);
        _downButtonField = _wireSequenceComponentType.GetField("DownButton", BindingFlags.Public | BindingFlags.Instance);

        _wireConfigurationType = ReflectionHelper.FindType("WireSequenceComponent+WireConfiguration");
        _wireField = _wireConfigurationType.GetField("Wire", BindingFlags.Public | BindingFlags.Instance);
    }

    private static Type _wireSequenceComponentType = null;
    private static Type _wireConfigurationType = null;
    private static FieldInfo _wireSequenceField = null;
    private static FieldInfo _currentPageField = null;
    private static FieldInfo _upButtonField = null;
    private static FieldInfo _downButtonField = null;
    private static FieldInfo _wireField = null;

    private IList _wireSequence = null;
    private MonoBehaviour _upButton = null;
    private MonoBehaviour _downButton = null;
}
