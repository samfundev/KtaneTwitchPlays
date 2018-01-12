public enum CommandResponse
{
    Start,
    EndNotComplete,
    EndComplete,
    EndError,
    EndErrorSubtractScore,
    NoResponse
}

public interface ICommandResponseNotifier
{
    void ProcessResponse(CommandResponse response, int value = 0);
}
