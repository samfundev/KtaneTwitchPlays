public sealed class CommandQueueItem
{
	public IRCMessage Message { get; private set; }
	public string Name { get; private set; }
	public CommandQueueItem(IRCMessage msg, string name = null)
	{
		Message = msg;
		Name = name;
	}
}