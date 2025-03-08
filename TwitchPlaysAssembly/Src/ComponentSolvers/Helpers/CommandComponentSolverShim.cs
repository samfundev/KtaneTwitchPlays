using System.Collections;

public abstract class CommandComponentSolverShim : ReflectionComponentSolverShim
{
	protected CommandComponentSolverShim(TwitchModule module, string componentString) :
		base(module, componentString)
	{
	}

	protected CommandComponentSolverShim(TwitchModule module, string componentString, string assemblyName) :
		base(module, componentString, assemblyName)
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		return CommandParser.Invoke(new IRCMessage(null, null, command), command, GetType());
	}
}
