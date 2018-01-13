using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ButtonComponentSolver : ComponentSolver
{
    public ButtonComponentSolver(BombCommander bombCommander, BombComponent bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        ModuleInformation buttonInfo = ComponentSolverFactory.GetModuleInfo("ButtonComponentSolver");
        ModuleInformation squarebuttonInfo = ComponentSolverFactory.GetModuleInfo("ButtonV2");

        _button = (MonoBehaviour)_buttonField.GetValue(bombComponent);
        modInfo = new ModuleInformation
        {
            builtIntoTwitchPlays = buttonInfo.builtIntoTwitchPlays,
            CameraPinningAlwaysAllowed = buttonInfo.CameraPinningAlwaysAllowed,
            helpText = VanillaRuleModifier.IsSeedVanilla()
                ? buttonInfo.helpText
                : squarebuttonInfo.helpText,
            manualCode = buttonInfo.manualCode,
            moduleDisplayName = buttonInfo.moduleDisplayName,
            moduleID = buttonInfo.moduleID,
            moduleScore = VanillaRuleModifier.IsSeedVanilla()
                ? buttonInfo.moduleScore
                : squarebuttonInfo.moduleScore
        };

        _bombComponent = bombComponent;
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        bool isModdedSeed = VanillaRuleModifier.IsSeedModded();
        inputCommand = inputCommand.ToLowerInvariant();
        if (!_held && inputCommand.EqualsAny("tap", "click"))
        {
            yield return "tap";
            DoInteractionClick(_button);
            _buttonCloseMethod.Invoke(_bombComponent, null);
            yield return new WaitForSeconds(0.1f);

        }
        if (!_held && (inputCommand.StartsWith("tap ") ||
                       inputCommand.StartsWith("click ")))
        {
            if (!isModdedSeed)
                yield break;
            yield return "tap2";

            IEnumerator releaseCoroutine = ReleaseCoroutineModded(inputCommand.Substring(inputCommand.IndexOf(' ')));
            while (releaseCoroutine.MoveNext())
            {
                yield return releaseCoroutine.Current;
            }
        }
        else if (!_held && inputCommand.Equals("hold"))
        {
            yield return "hold";

            _held = true;
            DoInteractionStart(_button);
            yield return new WaitForSeconds(2.0f);
        }
        else if (_held)
        {
            string[] commandParts = inputCommand.Split(' ');
            if (commandParts.Length == 2 && commandParts[0].Equals("release"))
            {
                IEnumerator releaseCoroutine;

                if (!isModdedSeed)
                {
                    int second = 0;
                    if (!int.TryParse(commandParts[1], out second))
                    {
                        yield break;
                    }
                    if (second >= 0 && second <= 9)
                        releaseCoroutine = ReleaseCoroutineVanilla(second);
                    else
                        yield break;
                }
                else
                {
                    releaseCoroutine = ReleaseCoroutineModded(inputCommand.Substring(inputCommand.IndexOf(' ')));
                }

                while (releaseCoroutine.MoveNext())
                {
                    yield return releaseCoroutine.Current;
                }
            }
        }
    }

    private IEnumerator ReleaseCoroutineVanilla(int second)
    {
        yield return "release";
        MonoBehaviour timerComponent = (MonoBehaviour)CommonReflectedTypeInfo.GetTimerMethod.Invoke(BombCommander.Bomb, null);
        string secondString = second.ToString();
        float timeRemaining = float.PositiveInfinity;
        while (timeRemaining > 0.0f && _held)
        {
            if (Canceller.ShouldCancel)
            {
                Canceller.ResetCancel();
                yield return string.Format("sendtochat The button was not {0} due to a request to cancel.", _held ? "released" : "tapped");
                yield break;
            }

            timeRemaining = (float)CommonReflectedTypeInfo.TimeRemainingField.GetValue(timerComponent);

            if (BombCommander.CurrentTimerFormatted.Contains(secondString))
            {
                DoInteractionEnd(_button);
                _buttonCloseMethod.Invoke(_bombComponent, null);
                _held = false;
            }

            yield return null;
        }
    }

    private IEnumerator ReleaseCoroutineModded(string second)
    {
        bool longwait = false;
        string[] list = second.Split(' ');
        List<int> sortedTimes = new List<int>();
        foreach (string value in list)
        {
            int time = -1;
            if (!int.TryParse(value, out time))
            {
                int pos = value.LastIndexOf(':');
                if (pos == -1) continue;
                int hour = 0;
                int min, sec;
                if (!int.TryParse(value.Substring(0, pos), out min))
                {
                    int pos2 = value.IndexOf(":");
                    if ((pos2 == -1) || (pos == pos2)) continue;
                    if (!int.TryParse(value.Substring(0, pos2), out hour)) continue;
                    if (!int.TryParse(value.Substring(pos2 + 1, pos - pos2 - 1), out min)) continue;
                }
                if (!int.TryParse(value.Substring(pos + 1), out sec)) continue;
                time = (hour * 3600) + (min * 60) + sec;
            }
            sortedTimes.Add(time);
        }
        sortedTimes.Sort();
        sortedTimes.Reverse();
        if (sortedTimes.Count == 0) yield break;

        yield return "release";

        MonoBehaviour timerComponent = (MonoBehaviour)CommonReflectedTypeInfo.GetTimerMethod.Invoke(BombCommander.Bomb, null);

        int timeTarget = sortedTimes[0];
        sortedTimes.RemoveAt(0);

        int waitTime = (int)((float)CommonReflectedTypeInfo.TimeRemainingField.GetValue(timerComponent) + 0.25f);
        waitTime -= timeTarget;
        if (waitTime >= 30)
        {
            yield return "elevator music";
            if (waitTime >= 120)
            {
                yield return string.Format("sendtochat !!!WARNING!!! - you might want to do a !cancel right about now, as you will be waiting for {0} minutes and {1} seconds for button release. Seed #{2} applies to this button.", waitTime / 60, waitTime % 60, VanillaRuleModifier.GetRuleSeed());
                yield return string.Format("sendtochat Click the button with !{0} tap. Click the button at time with !{0} tap 8:55 8:44 8:33. Hold the button with !{0} hold. Release the button with !{0} release 9:58 9:49 9:30.", Code);
                longwait = true;
            }
        }

        float timeRemaining = float.PositiveInfinity;
        
        while (timeRemaining > 0.0f)
        {
            if (Canceller.ShouldCancel)
            {
                Canceller.ResetCancel();
                if (timeTarget < 10)
                    yield return string.Format("sendtochat The button was not {0} due to a request to cancel. Remember that the rule set that applies is seed #{1}", _held ? "released" : "tapped", VanillaRuleModifier.GetRuleSeed());
                else
                    yield return string.Format("sendtochat The button was not {0} due to a request to cancel.", _held ? "released" : "tapped");
                _buttonCloseMethod.Invoke(_bombComponent, null);
                yield break;
            }

            timeRemaining = (int)((float)CommonReflectedTypeInfo.TimeRemainingField.GetValue(timerComponent) + 0.25f);

            if (timeRemaining < timeTarget)
            {
                if (sortedTimes.Count == 0)
                {
                    yield return string.Format("sendtochaterror The button was not {0} because all of your specfied times are greater than the time remaining.", _held ? "released" : "tapped");
                    _buttonCloseMethod.Invoke(_bombComponent, null);
                    yield break;
                }
                timeTarget = sortedTimes[0];
                sortedTimes.RemoveAt(0);

                waitTime = (int)timeRemaining;
                waitTime -= timeTarget;
                if (waitTime >= 30)
                {
                    yield return "elevator music";
                    
                    if (waitTime >= 120 && !longwait)
                    {
                        yield return string.Format("sendtochat !!!WARNING!!! - you might want to do a !cancel right about now, as you will be waiting for {0} minutes and {1} seconds for button release. Seed #{2} applies to this button.", waitTime / 60, waitTime % 60, VanillaRuleModifier.GetRuleSeed());
                        yield return string.Format("sendtochat Click the button with !{0} tap. Click the button at time with !{0} tap 8:55 8:44 8:33. Hold the button with !{0} hold. Release the button with !{0} release 9:58 9:49 9:30.", Code);
                    }
                    longwait = true;
                }

                continue;
            }
            if (timeRemaining == timeTarget)
            {
                if (!_held)
                {
                    DoInteractionStart(_button);
                    yield return new WaitForSeconds(0.1f);
                }
                DoInteractionEnd(_button);
                _buttonCloseMethod.Invoke(_bombComponent, null);
                _held = false;
                yield break;
            }

            yield return null;
        }
    }

    static ButtonComponentSolver()
    {
        _buttonComponentType = ReflectionHelper.FindType("ButtonComponent");
        _buttonField = _buttonComponentType.GetField("button", BindingFlags.Public | BindingFlags.Instance);
        _buttonCloseMethod = _buttonComponentType.GetMethod("CloseLid", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private static Type _buttonComponentType = null;
    private static FieldInfo _buttonField = null;
    private static MethodInfo _buttonCloseMethod = null;

    private MonoBehaviour _button = null;
    private MonoBehaviour _bombComponent;
    private bool _held = false;

}
