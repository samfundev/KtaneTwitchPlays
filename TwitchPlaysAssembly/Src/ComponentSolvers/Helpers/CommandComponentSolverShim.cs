using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using static CommandParser;

public abstract class CommandComponentSolverShim : ReflectionComponentSolverShim
{
	protected CommandComponentSolverShim(TwitchModule module, string componentString) :
		base(module, componentString)
	{
	}

	protected CommandComponentSolverShim(TwitchModule module, string assemblyName, string componentString) :
		base(module, assemblyName, componentString)
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		var methods = GetType()
			.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
			.Where(method =>
				method.ReturnType == typeof(IEnumerator) &&
				method.GetParameters().Length == 1 &&
				method.GetParameters()[0].ParameterType == typeof(CommandParser));
		foreach (var method in methods)
		{
			var enumerator = ((IEnumerator) method.Invoke(this, new[] { new CommandParser(command) })).Flatten();
			var parsedCommand = false;

			while (true)
			{
				try
				{
					if (!enumerator.MoveNext()) break;
					parsedCommand = true;
				}
				catch (ParsingFailedException)
				{
					break;
				}
				catch (Exception)
				{
					throw;
				}

				yield return enumerator.Current;
			}

			if (!parsedCommand) continue;

			break;
		}
	}
}
