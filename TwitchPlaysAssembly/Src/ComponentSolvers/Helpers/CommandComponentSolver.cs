using System.Collections;

public abstract class CommandComponentSolver : ReflectionComponentSolver
{
	protected CommandComponentSolver(TwitchModule module, string componentString, string helpMessage) :
		base(module, componentString, helpMessage)
	{
	}

	protected CommandComponentSolver(TwitchModule module, string componentString, string assemblyName, string helpMessage) :
		base(module, componentString, assemblyName, helpMessage)
	{
	}

	public override IEnumerator Respond(string[] split, string command)
	{
		return CommandParser.Invoke(new IRCMessage("", "", command), command, this, GetType());
	}
}