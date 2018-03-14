using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class SimpleModComponentSolver : ComponentSolver
{
    public SimpleModComponentSolver(BombCommander bombCommander, BombComponent bombComponent, MethodInfo processMethod, MethodInfo forcedSolveMethod, Component commandComponent, FieldInfo zenmodefield) :
        base(bombCommander, bombComponent)
	{
        ProcessMethod = processMethod;
		ForcedSolveMethod = forcedSolveMethod;
        CommandComponent = commandComponent;
		ZenModeField = zenmodefield;
		ZenMode = OtherModes.ZenModeOn;
        modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        if (ProcessMethod == null)
        {
            DebugHelper.LogError("A declared TwitchPlays SimpleModComponentSolver process method is <null>, yet a component solver has been created; command invokation will not continue.");
            yield break;
        }

        KMSelectable[] selectableSequence = null;

        try
        {
            bool RegexValid = modInfo.validCommands == null;
            if (!RegexValid)
            {
                foreach (string regex in modInfo.validCommands)
                {
                    RegexValid = Regex.IsMatch(inputCommand, regex, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                    if (RegexValid)
                    {
                        break;
                    }
                }
            }
            if (!RegexValid)
                yield break;

            selectableSequence = (KMSelectable[])ProcessMethod.Invoke(CommandComponent, new object[] { inputCommand });
            if (selectableSequence == null || selectableSequence.Length == 0)
            {
                yield break;
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogException(ex, string.Format("An exception occurred while trying to invoke {0}.{1}; the command invokation will not continue.", ProcessMethod.DeclaringType.FullName, ProcessMethod.Name));
            yield break;
        }

        if (!modInfo.DoesTheRightThing)
        {
            yield return "modsequence";
        }

        foreach (KMSelectable selectable in selectableSequence)
        {
	        yield return "trycancel";
	        yield return new[] {selectable};
        }
    }
}
