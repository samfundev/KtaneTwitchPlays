using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class CoroutineModComponentSolver : ComponentSolver
{
    public CoroutineModComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller, MethodInfo processMethod, Component commandComponent, FieldInfo cancelfield, Type canceltype) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        ProcessMethod = processMethod;
        CommandComponent = commandComponent;
        TryCancelField = cancelfield;
        TryCancelComponentSolverType = canceltype;
        modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        if (ProcessMethod == null)
        {
            Debug.LogError("A declared TwitchPlays CoroutineModComponentSolver process method is <null>, yet a component solver has been created; command invokation will not continue.");
            yield break;
        }
		
        IEnumerator responseCoroutine = null;

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

        try
        {
            responseCoroutine = (IEnumerator)ProcessMethod.Invoke(CommandComponent, new object[] { inputCommand });
            if (responseCoroutine == null)
            {
                yield break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogErrorFormat("An exception occurred while trying to invoke {0}.{1}; the command invokation will not continue.", ProcessMethod.DeclaringType.FullName, ProcessMethod.Name);
            Debug.LogException(ex);
            yield break;
        }

        //Previous changelists mentioned that people using the TPAPI were not following strict rules about how coroutine implementations should be done, w.r.t. required yield returning first before doing things.
        //From the TPAPI side of things, this was *never* an explicit requirement. This yield return is here to explicitly follow the internal design for how component solvers are structured, so that external
        //code would never be executed until absolutely necessary.
        //There is the side-effect though that invalid commands sent to the module will appear as if they were 'correctly' processed, by executing the focus.
        //I'd rather have interactions that are not broken by timing mismatches, even if the tradeoff is that it looks like it accepted invalid commands.
        if (!modInfo.DoesTheRightThing)
        {
            yield return "modcoroutine";
        }

        while (true)
        {
            try
            {
                if (!responseCoroutine.MoveNext())
                    yield break;
            }
            catch (Exception ex)
            {
                Debug.LogErrorFormat(
                    "An exception occurred while trying to invoke {0}.{1}; the command invokation will not continue.",
                    ProcessMethod.DeclaringType.FullName, ProcessMethod.Name);
                Debug.LogException(ex);
                yield break;
            }

            object currentObject = responseCoroutine.Current;
            if (currentObject is KMSelectable)
            {
                KMSelectable selectable = (KMSelectable) currentObject;
                if (HeldSelectables.Contains(selectable))
                {
                    DoInteractionEnd(selectable);
                    HeldSelectables.Remove(selectable);
                }
                else
                {
                    DoInteractionStart(selectable);
                    HeldSelectables.Add(selectable);
                }
            }
            if (currentObject is KMSelectable[])
            {
                KMSelectable[] selectables = (KMSelectable[]) currentObject;
                foreach (KMSelectable selectable in selectables)
                {
                    DoInteractionClick(selectable);
					yield return new WaitForSeconds(0.1f);
                }
            }
            if (currentObject is string)
            {
                string str = (string) currentObject;
                if (str.Equals("cancelled", StringComparison.InvariantCultureIgnoreCase))
                {
                    Canceller.ResetCancel();
                    TryCancel = false;
                }
            }
            yield return currentObject;

            if (Canceller.ShouldCancel)
                TryCancel = true;
        } 
    }

    private readonly MethodInfo ProcessMethod = null;
    private readonly Component CommandComponent = null;
    private readonly HashSet<KMSelectable> HeldSelectables = new HashSet<KMSelectable>();
}
