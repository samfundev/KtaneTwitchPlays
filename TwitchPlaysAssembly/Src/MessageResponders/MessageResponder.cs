using UnityEngine;

public abstract class MessageResponder : MonoBehaviour
{
    protected CoroutineQueue _coroutineQueue = null;

    private void OnDestroy()
    {
	    IRCConnection.Instance?.OnMessageReceived.RemoveListener(OnInternalMessageReceived);
    }

    public void SetupResponder(IRCConnection ircConnection, CoroutineQueue coroutineQueue)
    {
        _coroutineQueue = coroutineQueue;

	    IRCConnection.Instance?.OnMessageReceived.AddListener(OnInternalMessageReceived);
    }

    public bool IsAuthorizedDefuser(string userNickName, bool silent=false)
    {
        bool result = (TwitchPlaySettings.data.EnableTwitchPlaysMode || UserAccess.HasAccess(userNickName, AccessLevel.Defuser, true));
        if (!result && !silent)
	        IRCConnection.Instance?.SendMessage(TwitchPlaySettings.data.TwitchPlaysDisabled, userNickName);

        return result;
    }

    protected abstract void OnMessageReceived(string userNickName, string userColorCode, string text);

    private void OnInternalMessageReceived(string userNickName, string userColorCode, string text)
    {
        if (gameObject.activeInHierarchy && isActiveAndEnabled)
        {
            OnMessageReceived(userNickName, userColorCode, text);
        }
    }
}
