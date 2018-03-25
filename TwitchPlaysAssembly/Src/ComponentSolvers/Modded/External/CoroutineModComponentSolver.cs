using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class CoroutineModComponentSolver : ComponentSolver
{
	public CoroutineModComponentSolver(BombCommander bombCommander, BombComponent bombComponent, MethodInfo processMethod, MethodInfo forcedSolveMethod, Component commandComponent, FieldInfo cancelfield, FieldInfo zenmodefield) :
		base(bombCommander, bombComponent)
	{
		ProcessMethod = processMethod;
		ForcedSolveMethod = forcedSolveMethod;
		CommandComponent = commandComponent;
		TryCancelField = cancelfield;
		ZenModeField = zenmodefield;
		ZenMode = OtherModes.ZenModeOn;
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (ProcessMethod == null)
		{
			DebugHelper.LogError("A declared TwitchPlays CoroutineModComponentSolver process method is <null>, yet a component solver has been created; command invokation will not continue.");
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
			DebugHelper.LogException(ex, string.Format("An exception occurred while trying to invoke {0}.{1}; the command invokation will not continue.", ProcessMethod.DeclaringType.FullName, ProcessMethod.Name));

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
		bool result = true;
		string exception = null;
		while (result)
		{
			try
			{
				result = responseCoroutine.MoveNext();
			}
			catch (FormatException ex)
			{
				DebugHelper.LogException(ex, string.Format("An exception occurred while trying to invoke {0}.{1}; the command invocation will not continue.", ProcessMethod?.DeclaringType?.FullName, ProcessMethod.Name));
				result = false;
				exception = ex.Message;
			}
			catch (Exception ex)
			{
				DebugHelper.LogException(ex, string.Format("An exception occurred while trying to invoke {0}.{1}; the command invocation will not continue.", ProcessMethod?.DeclaringType?.FullName, ProcessMethod.Name));
				throw;
			}
			if(result)
				yield return responseCoroutine.Current;
			else if (exception != null)
				yield return $"sendtochaterror {exception}";
		} 
	}

	
}
