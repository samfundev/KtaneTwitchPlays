using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class SimpleModComponentSolver : ComponentSolver
{
	public SimpleModComponentSolver(BombCommander bombCommander, BombComponent bombComponent, MethodInfo processMethod, MethodInfo forcedSolveMethod, Component commandComponent, FieldInfo zenmodefield, FieldInfo abandonModuleField) :
		base(bombCommander, bombComponent)
	{
		ProcessMethod = processMethod;
		ForcedSolveMethod = forcedSolveMethod;
		CommandComponent = commandComponent;
		ZenModeField = zenmodefield;
		AbandonModuleField = abandonModuleField;
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

			var selectableSequence = (IEnumerable<KMSelectable>) ProcessMethod.Invoke(CommandComponent, new object[] { inputCommand });
			if (selectableSequence == null)
				yield break;
			selectableList = (selectableSequence as IList<KMSelectable>) ?? selectableSequence.ToArray();
			if (selectableList.Count == 0)
				yield break;
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, string.Format("An exception occurred while trying to invoke {0}.{1}; the command invocation will not continue.", ProcessMethod?.DeclaringType?.FullName, ProcessMethod.Name));
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

		yield return "modsequence";
		yield return "trycancelsequence";
		yield return selectableList;
	}
}
