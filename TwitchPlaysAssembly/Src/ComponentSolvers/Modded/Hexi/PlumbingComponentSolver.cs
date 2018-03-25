using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TwitchPlaysAssembly.ComponentSolvers.Modded.Shims;

public class PlumbingComponentSolver : ComponentSolverShim
{
	public PlumbingComponentSolver(BombCommander bombCommander, BombComponent bombComponent) :
		base(bombCommander, bombComponent)
	{
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		inputCommand = inputCommand.ToLowerInvariant();
		var sequence = inputCommand.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();

		if (sequence.Any(x => sequence.Count(y => y.Equals(x)) >= 4) || inputCommand.Equals("spinme"))
		{
			yield return null;
			yield return "elevator music";
		}

		if (inputCommand.Equals("check")) inputCommand = "submit";

		IEnumerator process = base.RespondToCommandInternal(inputCommand);
		while (process.MoveNext()) yield return process.Current;
	}
}