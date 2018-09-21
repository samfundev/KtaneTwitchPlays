using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class SimpleModComponentSolver : ComponentSolver
{
	public SimpleModComponentSolver(BombCommander bombCommander, BombComponent bombComponent, MethodInfo processMethod, MethodInfo forcedSolveMethod, Component commandComponent, FieldInfo zenModeField, FieldInfo timeModeField, FieldInfo abandonModuleField, FieldInfo twitchPlaysField) :
		base(bombCommander, bombComponent)
	{
		ProcessMethod = processMethod;
		ForcedSolveMethod = forcedSolveMethod;
		CommandComponent = commandComponent;
		ZenModeField = zenModeField;
		TimeModeField = timeModeField;
		AbandonModuleField = abandonModuleField;
		TwitchPlaysField = twitchPlaysField;
		TwitchPlays = true;
		ZenMode = OtherModes.ZenModeOn;
		TimeMode = OtherModes.TimeModeOn;
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (ProcessMethod == null)
		{
			DebugHelper.LogError("A declared TwitchPlays SimpleModComponentSolver process method is <null>, yet a component solver has been created; command invocation will not continue.");
			yield break;
		}

		IList<KMSelectable> selectableList = null;

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

			IEnumerable<KMSelectable> selectableSequence = (IEnumerable<KMSelectable>) ProcessMethod.Invoke(CommandComponent, new object[] { inputCommand });
			if (selectableSequence == null)
				yield break;
			selectableList = selectableSequence as IList<KMSelectable> ?? selectableSequence.ToArray();
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex,
				$"An exception occurred while trying to invoke {ProcessMethod?.DeclaringType?.FullName}.{ProcessMethod.Name}; the command invocation will not continue.");
			loop:
			switch (ex)
			{
				case FormatException fex:
					exception = fex.Message;
					break;
				default:
					if (ex.InnerException == null) throw;
					ex = ex.InnerException;
					goto loop;
			}
		}

		if (exception != null)
		{
			yield return $"sendtochaterror {exception}";
			yield break;
		}

		if (selectableList?.Count == 0)
			yield return null;
		else
		{
			yield return "modsequence";
			yield return "trycancelsequence";
			yield return selectableList;
		}
	}
}
