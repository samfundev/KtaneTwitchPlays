using System.Collections;

public interface ICommandResponder
{
	IEnumerator RespondToCommand(Message message, ICommandResponseNotifier responseNotifier);
}
