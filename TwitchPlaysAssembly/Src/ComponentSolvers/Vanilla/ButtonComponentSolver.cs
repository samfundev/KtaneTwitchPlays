using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonComponentSolver : ComponentSolver
{
    public ButtonComponentSolver(BombCommander bombCommander, ButtonComponent bombComponent) :
        base(bombCommander, bombComponent)
	{
        ModuleInformation buttonInfo = ComponentSolverFactory.GetModuleInfo("ButtonComponentSolver");
        ModuleInformation squarebuttonInfo = ComponentSolverFactory.GetModuleInfo("ButtonV2");

        bombComponent.GetComponent<Selectable>().OnCancel += bombComponent.OnButtonCancel;
        _button = bombComponent.button;
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
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        bool isModdedSeed = VanillaRuleModifier.IsSeedModded();
        inputCommand = inputCommand.ToLowerInvariant();
        if (!_held && inputCommand.EqualsAny("tap", "click"))
        {
            yield return "tap";
            yield return DoInteractionClick(_button);

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
                    if (!int.TryParse(commandParts[1], out int second))
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
        TimerComponent timerComponent = BombCommander.Bomb.GetTimer();
        string secondString = second.ToString();
        float timeRemaining = float.PositiveInfinity;
        while (timeRemaining > 0.0f && _held)
        {
            if (CoroutineCanceller.ShouldCancel)
            {
	            CoroutineCanceller.ResetCancel();
                yield return string.Format("sendtochat The button was not {0} due to a request to cancel.", _held ? "released" : "tapped");
                yield break;
            }

            timeRemaining = timerComponent.TimeRemaining;

            if (BombCommander.CurrentTimerFormatted.Contains(secondString))
            {
                DoInteractionEnd(_button);
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
            if (!int.TryParse(value, out int time))
            {
                int pos = value.LastIndexOf(':');
                if (pos == -1) continue;
                int hour = 0;
                if (!int.TryParse(value.Substring(0, pos), out int min))
                {
                    int pos2 = value.IndexOf(":");
                    if ((pos2 == -1) || (pos == pos2)) continue;
                    if (!int.TryParse(value.Substring(0, pos2), out hour)) continue;
                    if (!int.TryParse(value.Substring(pos2 + 1, pos - pos2 - 1), out min)) continue;
                }
                if (!int.TryParse(value.Substring(pos + 1), out int sec)) continue;
                time = (hour * 3600) + (min * 60) + sec;
            }
            sortedTimes.Add(time);
        }
        sortedTimes.Sort();
        sortedTimes.Reverse();
        if (sortedTimes.Count == 0) yield break;

        yield return "release";

        TimerComponent timerComponent = BombCommander.Bomb.GetTimer();

        int timeTarget = sortedTimes[0];
        sortedTimes.RemoveAt(0);

        int waitTime = (int)(timerComponent.TimeRemaining + 0.25f);
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
            if (CoroutineCanceller.ShouldCancel)
            {
	            CoroutineCanceller.ResetCancel();
                if (timeTarget < 10)
                    yield return string.Format("sendtochat The button was not {0} due to a request to cancel. Remember that the rule set that applies is seed #{1}", _held ? "released" : "tapped", VanillaRuleModifier.GetRuleSeed());
                else
                    yield return string.Format("sendtochat The button was not {0} due to a request to cancel.", _held ? "released" : "tapped");
                yield break;
            }

            timeRemaining = (int)(timerComponent.TimeRemaining + 0.25f);

            if (timeRemaining < timeTarget)
            {
                if (sortedTimes.Count == 0)
                {
                    yield return string.Format("sendtochaterror The button was not {0} because all of your specfied times are greater than the time remaining.", _held ? "released" : "tapped");
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
            if (Math.Abs(timeRemaining - timeTarget) < 0.1f)
            {
                if (!_held)
                {
                    DoInteractionStart(_button);
                    yield return new WaitForSeconds(0.1f);
                }
                DoInteractionEnd(_button);
                _held = false;
                yield break;
            }

            yield return null;
        }
    }

    private PressableButton _button = null;
    private bool _held = false;
}
