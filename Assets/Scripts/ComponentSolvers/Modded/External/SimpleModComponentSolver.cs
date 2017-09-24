using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class SimpleModComponentSolver : ComponentSolver
{
    public SimpleModComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller, MethodInfo processMethod, Component commandComponent, string manual, string help, bool statusLeft, bool statusBottom, float rotation) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        ProcessMethod = processMethod;
        CommandComponent = commandComponent;
        helpMessage = help;
        manualCode = manual;
        statusLightLeft = statusLeft;
        statusLightBottom = statusBottom;
        IDRotation = rotation;
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        if (ProcessMethod == null)
        {
            Debug.LogError("A declared TwitchPlays SimpleModComponentSolver process method is <null>, yet a component solver has been created; command invokation will not continue.");
            yield break;
        }

        KMSelectable[] selectableSequence = null;

        try
        {
            selectableSequence = (KMSelectable[])ProcessMethod.Invoke(CommandComponent, new object[] { inputCommand });
            if (selectableSequence == null || selectableSequence.Length == 0)
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
        
        yield return "modsequence";

        for(int selectableIndex = 0; selectableIndex < selectableSequence.Length; ++selectableIndex)
        {
            if (Canceller.ShouldCancel)
            {
                Canceller.ResetCancel();
                yield break;
            }

            KMSelectable selectable = selectableSequence[selectableIndex];
            if (selectable == null)
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            DoInteractionClick(selectable);
			yield return new WaitForSeconds(0.1f);
        }
    }

    private readonly MethodInfo ProcessMethod = null;
    private readonly Component CommandComponent = null;
}
