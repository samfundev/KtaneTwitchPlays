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

        ((KMSelectable) _leftKey).OnInteract = HandleLeftKey;
        ((KMSelectable) _rightKey).OnInteract = HandleRightKey;
    }

    private bool HandleRightKey()
    {
        if (!GetValue(_activatedField) || GetValue(_rightKeyTurnedField)) return false;
        KMBombInfo bombInfo = BombComponent.GetComponent<KMBombInfo>();
        KMBombModule bombModule = BombComponent.GetComponent<KMBombModule>();

        _beforeRightKeyField.SetValue(null, TwitchPlaySettings.data.DisableTurnTheKeysSoftLock ? new string[0] : RightBeforeA);
        _onRightKeyTurnMethod.Invoke(BombComponent.GetComponent(_componentType), null);
        if (GetValue(_rightKeyTurnedField) && TwitchPlaySettings.data.DisableTurnTheKeysSoftLock)
        {
            //Check to see if any forbidden modules for this key were solved.
            if (bombInfo.GetSolvedModuleNames().Any(x => RightBeforeA.Contains(x)))
                bombModule.HandleStrike();  //If so, Award a strike for it.
        }
        return false;
    }

    private bool HandleLeftKey()
    {
        if (!GetValue(_activatedField) || GetValue(_leftKeyTurnedField)) return false;
        KMBombInfo bombInfo = BombComponent.GetComponent<KMBombInfo>();
        KMBombModule bombModule = BombComponent.GetComponent<KMBombModule>();

        _beforeLeftKeyField.SetValue(null, TwitchPlaySettings.data.DisableTurnTheKeysSoftLock ? new string[0] : LeftBeforeA);
        _onLeftKeyTurnMethod.Invoke(BombComponent.GetComponent(_componentType), null);
        if (GetValue(_leftKeyTurnedField) && TwitchPlaySettings.data.DisableTurnTheKeysSoftLock)
        {
            //Check to see if any forbidden modules for this key were solved.
            if (bombInfo.GetSolvedModuleNames().Any(x => LeftBeforeA.Contains(x)))
                bombModule.HandleStrike();  //If so, Award a strike for it.
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

    private static string[] LeftBeforeA = new string[]
    {
        "Maze",
        "Memory",
        "Complicated Wires",
        "Wire Sequence",
        "Cryptography"
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
