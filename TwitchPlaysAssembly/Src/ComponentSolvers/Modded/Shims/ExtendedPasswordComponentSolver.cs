using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TwitchPlaysAssembly.ComponentSolvers.Modded.Shims;

public class ExtendedPasswordComponentSolver : ComponentSolverShim
{
	public ExtendedPasswordComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), "!{0} cycle 6 [cycle through the letters in column 6] | !{0} lambda [try to submit a word]");
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.Trim();
		if (inputCommand.StartsWith("cycle ", StringComparison.InvariantCultureIgnoreCase))
		{
			HashSet<int> alreadyCycled = new HashSet<int>();
			string[] commandParts = inputCommand.Split(' ');
			
			foreach (string cycle in commandParts.Skip(1))
			{
				if (!int.TryParse(cycle, out int spinnerIndex) || !alreadyCycled.Add(spinnerIndex) || spinnerIndex < 1 || spinnerIndex > 6)
					continue;

				IEnumerator spinnerCoroutine = base.RespondToCommandInternal($"cycle {cycle}");
				while (spinnerCoroutine.MoveNext())
				{
					yield return spinnerCoroutine.Current;
					yield return "trycancel";
				}
			}
			yield break;
		}
		else if (inputCommand.Trim().Length == 6)
		{
			IEnumerator command = base.RespondToCommandInternal(inputCommand);
			while (command.MoveNext())
			{
				yield return command.Current;
				yield return "trycancel";
			}
			yield return null;
			yield return "unsubmittablepenalty";
		}
		else
		{
			yield return "sendtochaterror valid commands are 'cycle [columns]' or a 6 letter password to try.";
		}
	}
}
