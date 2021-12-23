public sealed class CommandQueueItem
{
	public IRCMessage Message { get; }
	public string Name { get; }
	public CommandQueueItem(IRCMessage msg, string name = null)
	{
		Message = msg;
		Name = name;
	}
}