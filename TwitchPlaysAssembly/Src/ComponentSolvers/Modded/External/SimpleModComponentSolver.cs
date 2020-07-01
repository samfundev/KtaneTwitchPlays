using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class SimpleModComponentSolver : ComponentSolver
{
	public SimpleModComponentSolver(TwitchModule module, ComponentSolverFields componentSolverFields) :
		base(module, componentSolverFields.HookUpEvents)
	{
		ProcessMethod = componentSolverFields.Method;
		ForcedSolveMethod = componentSolverFields.ForcedSolveMethod;
		CommandComponent = componentSolverFields.CommandComponent;
		ZenModeField = componentSolverFields.ZenModeField;
		TimeModeField = componentSolverFields.TimeModeField;
		AbandonModuleField = componentSolverFields.AbandonModuleField;
		TwitchPlaysField = componentSolverFields.TwitchPlaysField;
		HelpMessageField = componentSolverFields.HelpMessageField;
		ManualCodeField = componentSolverFields.ManualCodeField;
		TwitchPlays = true;
		ZenMode = OtherModes.Unexplodable;
		TimeMode = OtherModes.TimeModeOn;
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (ProcessMethod == null)
		{
			DebugHelper.LogError("A declared TwitchPlays SimpleModComponentSolver process method is <null>, yet a component solver has been created; command invocation will not continue.");
			yield break;
		}

		IEnumerable<KMSelectable> selectableSequence = null;

		string exception = null;
		try
		{
			bool regexValid = ModInfo.validCommands == null;
			if (!regexValid)
			{
				foreach (string regex in ModInfo.validCommands)
				{
					regexValid = Regex.IsMatch(inputCommand, regex, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
					if (regexValid)
						break;
				}
			}
			if (!regexValid)
				yield break;

			selectableSequence = (IEnumerable<KMSelectable>) ProcessMethod.Invoke(CommandComponent, new object[] { inputCommand });
			if (selectableSequence == null)
				yield break;
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

		if (selectableSequence?.Any() == false)
		{
			yield return null;
			yield return null;
		}
		else
		{
			yield return "modsequence";
			yield return "trycancelsequence";
			yield return selectableSequence;
		}
	}
}
