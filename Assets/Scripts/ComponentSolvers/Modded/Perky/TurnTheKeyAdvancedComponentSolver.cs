using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class TurnTheKeyAdvancedComponentSolver : ComponentSolver
{
    public TurnTheKeyAdvancedComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _leftKey = (MonoBehaviour)_leftKeyField.GetValue(bombComponent.GetComponent(_componentType));
        _rightKey = (MonoBehaviour)_rightKeyField.GetValue(bombComponent.GetComponent(_componentType));
        modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());

        ((KMSelectable) _leftKey).OnInteract = () => HandleKey(LeftBeforeA, LeftAfterA, _leftKeyTurnedField, _rightKeyTurnedField, _beforeLeftKeyField, _onLeftKeyTurnMethod);
        ((KMSelectable) _rightKey).OnInteract = () => HandleKey(RightBeforeA, RightAfterA, _rightKeyTurnedField, _leftKeyTurnedField, _beforeRightKeyField, _onRightKeyTurnMethod);
    }

    private bool HandleKey(string[] modulesBefore, string[] modulesAfter, FieldInfo keyTurned, FieldInfo otherKeyTurned, FieldInfo beforeKeyField, MethodInfo onKeyTurn)
    {
        if (!GetValue(_activatedField) || GetValue(keyTurned)) return false;
        KMBombInfo bombInfo = BombComponent.GetComponent<KMBombInfo>();
        KMBombModule bombModule = BombComponent.GetComponent<KMBombModule>();

        if (TwitchPlaySettings.data.EnforceSolveAllBeforeTurningKeys &&
            modulesAfter.Any(x => bombInfo.GetSolvedModuleNames().Count(x.Equals) != bombInfo.GetSolvableModuleNames().Count(x.Equals)))
        {
            bombModule.HandleStrike();
            return false;
        }

        beforeKeyField.SetValue(null, TwitchPlaySettings.data.DisableTurnTheKeysSoftLock ? new string[0] : modulesBefore);
        onKeyTurn.Invoke(BombComponent.GetComponent(_componentType), null);
        if (GetValue(keyTurned))
        {
            //Check to see if any forbidden modules for this key were solved.
            if (TwitchPlaySettings.data.DisableTurnTheKeysSoftLock && bombInfo.GetSolvedModuleNames().Any(modulesBefore.Contains))
                bombModule.HandleStrike();  //If so, Award a strike for it.

            if (GetValue(otherKeyTurned))
            {
                int modules = bombInfo.GetSolvedModuleNames().Count(x => RightAfterA.Contains(x) || LeftAfterA.Contains(x));
                TwitchPlaySettings.AddRewardBonus(2 * modules);
                IRCConnection.SendMessage("Reward increased by {0} for defusing module !{1} ({2}).", modules * 2, Code, bombModule.ModuleDisplayName);
            }
        }
        return false;
    }

    private bool GetValue(FieldInfo field)
    {
        return (bool) field.GetValue(BombComponent.GetComponent(_componentType));
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        var commands = inputCommand.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (commands.Length != 2 || commands[0] != "turn")
            yield break;

        MonoBehaviour Key;
        switch (commands[1])
        {
            case "l": case "left":
                Key = _leftKey;
                break;
            case "r": case "right":
                Key = _rightKey;
                break;
            default:
                yield break;
        }
        yield return "Turning the key";
        yield return DoInteractionClick(Key);
    }

    static TurnTheKeyAdvancedComponentSolver()
    {
        _componentType = ReflectionHelper.FindType("TurnKeyAdvancedModule");
        _leftKeyField = _componentType.GetField("LeftKey", BindingFlags.Public | BindingFlags.Instance);
        _rightKeyField = _componentType.GetField("RightKey", BindingFlags.Public | BindingFlags.Instance);
        _activatedField = _componentType.GetField("bActivated", BindingFlags.NonPublic | BindingFlags.Instance);
        _beforeLeftKeyField = _componentType.GetField("LeftBeforeA", BindingFlags.NonPublic | BindingFlags.Static);
        _beforeRightKeyField = _componentType.GetField("RightBeforeA", BindingFlags.NonPublic | BindingFlags.Static);
        _leftKeyTurnedField = _componentType.GetField("bLeftKeyTurned", BindingFlags.NonPublic | BindingFlags.Instance);
        _rightKeyTurnedField = _componentType.GetField("bRightKeyTurned", BindingFlags.NonPublic | BindingFlags.Instance);
        _onLeftKeyTurnMethod = _componentType.GetMethod("OnLeftKeyTurn", BindingFlags.NonPublic | BindingFlags.Instance);
        _onRightKeyTurnMethod = _componentType.GetMethod("OnRightKeyTurn", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private static Type _componentType = null;
    private static FieldInfo _leftKeyField = null;
    private static FieldInfo _rightKeyField = null;
    private static FieldInfo _activatedField = null;
    private static FieldInfo _beforeLeftKeyField = null;
    private static FieldInfo _beforeRightKeyField = null;
    private static FieldInfo _leftKeyTurnedField = null;
    private static FieldInfo _rightKeyTurnedField = null;
    private static MethodInfo _onLeftKeyTurnMethod = null;
    private static MethodInfo _onRightKeyTurnMethod = null;

    private MonoBehaviour _leftKey = null;
    private MonoBehaviour _rightKey = null;

    private static string[] LeftAfterA = new string[]
    {
        "Password",
        "Crazy Talk",
        "Who's On First",
        "Keypads",
        "Listening",
        "Orientation"
    };

    private static string[] LeftBeforeA = new string[]
    {
        "Maze",
        "Memory",
        "Complicated Wires",
        "Wire Sequence",
        "Cryptography"
    };

    private static string[] RightAfterA = new string[]
    {
        "Morse Code",
        "Wires",
        "Two Bits",
        "The Button",
        "Colour Flash",
        "Round Keypad"
    };

    private static string[] RightBeforeA = new string[]
    {
        "Semaphore",
        "Combination Lock",
        "Simon Says",
        "Astrology",
        "Switches",
        "Plumbing"
    };
}
