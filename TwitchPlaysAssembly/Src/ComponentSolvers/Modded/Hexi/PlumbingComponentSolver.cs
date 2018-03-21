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
	    string[] sequence = inputCommand.ToUpperInvariant().Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();
	    List<string> pipes = new List<string>();
	    bool elevator = false;

		inputCommand = inputCommand.ToLowerInvariant();

        if (inputCommand.EqualsAny("submit", "check"))
        {
	        inputCommand = "submit";
        }
		else if (inputCommand.Equals("spinme"))
        {
	        elevator = true;
        }
		else if (inputCommand.StartsWith("rotate "))
        {
			foreach (string buttonString in sequence)
			{
				var letters = "ABCDEF";
				var numbers = "123456";
				if (buttonString.Length != 2 || letters.IndexOf(buttonString[0]) < 0 ||
				    numbers.IndexOf(buttonString[1]) < 0)
				{
					yield return $"sendtochaterror Bad pipe position: '{buttonString}'";
					yield break;
				}

				var row = numbers.IndexOf(buttonString[1]);
				var col = letters.IndexOf(buttonString[0]);
				var button = letters[row].ToString() + numbers[col];

				pipes.Add(button);
				elevator |= pipes.FindAll(x => x.Equals(button)).Count >= 4;
			}
	        inputCommand = $"rotate {pipes.Join()}";
        }

	    if (elevator)
	    {
		    yield return null;
		    yield return "elevator music";
	    }

	    IEnumerator process = base.RespondToCommandInternal(inputCommand);
	    while (process.MoveNext()) yield return process.Current;
    }
}