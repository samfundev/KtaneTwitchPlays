using System;
using System.Collections;
using System.Text.RegularExpressions;

public class CoroutineModComponentSolver : ComponentSolver
{
	public CoroutineModComponentSolver(TwitchModule module, ComponentSolverFields componentSolverFields) :
		base(module, componentSolverFields.HookUpEvents)
	{
		ProcessMethod = componentSolverFields.Method;
		ForcedSolveMethod = componentSolverFields.ForcedSolveMethod;
		CommandComponent = componentSolverFields.CommandComponent;
		TryCancelField = componentSolverFields.CancelField;
		ZenModeField = componentSolverFields.ZenModeField;
		TimeModeField = componentSolverFields.TimeModeField;
		AbandonModuleField = componentSolverFields.AbandonModuleField;
		TwitchPlaysField = componentSolverFields.TwitchPlaysField;
		SkipTimeField = componentSolverFields.TwitchPlaysSkipTimeField;
		HelpMessageField = componentSolverFields.HelpMessageField;
		TwitchPlays = true;
		ZenMode = OtherModes.Unexplodable;
		TimeMode = OtherModes.TimeModeOn;
		ModInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
	}

	protected internal override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (ProcessMethod == null)
		{
			DebugHelper.LogError("A declared TwitchPlays CoroutineModComponentSolver process method is <null>, yet a component solver has been created; command invokation will not continue.");
			yield break;
		}

		IEnumerator responseCoroutine;

		bool regexValid = ModInfo.validCommands == null;
		if (!regexValid)
		{
			foreach (string regex in ModInfo.validCommands)
			{
				regexValid = Regex.IsMatch(inputCommand, regex, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
				if (regexValid)
				{
					break;
				}
			}
		}
		if (!regexValid)
			yield break;

		try
		{
			responseCoroutine = (IEnumerator) ProcessMethod.Invoke(CommandComponent, new object[] { inputCommand });
			if (responseCoroutine == null)
			{
				yield break;
			}
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex,
				$"An exception occurred while trying to invoke {ProcessMethod?.DeclaringType?.FullName}.{ProcessMethod.Name}; the command invocation will not continue.");

			yield break;
		}

		//Previous change lists mentioned that people using the TPAPI were not following strict rules about how coroutine implementations should be done, w.r.t. required yield returning first before doing things.
		//From the TPAPI side of things, this was *never* an explicit requirement. This yield return is here to explicitly follow the internal design for how component solvers are structured, so that external
		//code would never be executed until absolutely necessary.
		//There is the side-effect though that invalid commands sent to the module will appear as if they were 'correctly' processed, by executing the focus.
		//I'd rather have interactions that are not broken by timing mismatches, even if the tradeoff is that it looks like it accepted invalid commands.
		if (ModInfo.CompatibilityMode)
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
			catch (Exception ex)
			{
				result = false;
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
			if (result)
				yield return responseCoroutine.Current;
			else if (exception != null)
				yield return $"sendtochaterror!f {exception}";
		}
	}
}
