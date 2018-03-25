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
			DebugHelper.LogError("A declared TwitchPlays SimpleModComponentSolver process method is <null>, yet a component solver has been created; command invocation will not continue.");
			yield break;
		}

		KMSelectable[] selectableSequence = null;

		string exception = null;
		try
		{
			bool regexValid = modInfo.validCommands == null;
			if (!regexValid)
			{
				foreach (string regex in modInfo.validCommands)
				{
					regexValid = Regex.IsMatch(inputCommand, regex, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
					if (regexValid)
						break;
				}
			}
			if (!regexValid)
				yield break;

			selectableSequence = (KMSelectable[]) ProcessMethod.Invoke(CommandComponent, new object[] {inputCommand});
			if (selectableSequence == null || selectableSequence.Length == 0)
				yield break;
		}
		catch (FormatException ex)
		{
			DebugHelper.LogException(ex, string.Format("An exception occurred while trying to invoke {0}.{1}; the command invocation will not continue.", ProcessMethod.DeclaringType.FullName, ProcessMethod.Name));
			exception = ex.Message;
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, string.Format("An exception occurred while trying to invoke {0}.{1}; the command invocation will not continue.", ProcessMethod.DeclaringType.FullName, ProcessMethod.Name));
			throw;
		}

		if (exception != null)
		{
			yield return $"sendtochaterror {exception}";
			yield break;
		}

		yield return "modsequence";
		yield return "trycancelsequence";
		yield return selectableSequence;
	}
}
