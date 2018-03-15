public enum CommandResponse
{
	Start,
	EndNotComplete,
	NoResponse
}

public interface ICommandResponseNotifier
{
	void ProcessResponse(CommandResponse response, int value = 0);
}
