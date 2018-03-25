using System;
using System.Collections;
using TwitchPlaysAssembly.ComponentSolvers.Modded.Shims;
using UnityEngine;

public class AdventureGameShim : ComponentSolverShim
{
	public AdventureGameShim(BombCommander bombCommander, BombComponent bombComponent) : base(bombCommander, bombComponent)
	{
	}

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		var commands = inputCommand.ToLowerInvariant().Split(new[] {' '}, 2);
		IEnumerator command;
		switch (commands.Length)
		{
			case 1 when commands[0].Equals("cycle"):
				yield return null;
				command = base.RespondToCommandInternal("cycle stats");
				while (command.MoveNext())
				{
					if(!CoroutineCanceller.ShouldCancel)
						yield return command.Current;
				}
				yield return "trywaitcancel 1.0";
				command = base.RespondToCommandInternal("cycle items");
				while (command.MoveNext())
				{
					yield return command.Current;
					yield return "trycancel";
				}
				break;

			case 2 when commands[0].Equals("use"):
				var items = commands[1].Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
				yield return null;
				foreach (var item in items)
				{
					yield return "trycancel";
					command = base.RespondToCommandInternal($"use {item.Trim()}");
					while (command.MoveNext())
					{
						yield return command.Current;
						yield return "trycancel";
					}
				}
				break;

			default:
				command = base.RespondToCommandInternal(inputCommand);
				while (command.MoveNext())
				{
					yield return command.Current;
					yield return "trycancel";
				}
				break;
		}
	}
}
