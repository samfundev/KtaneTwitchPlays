using System;
using UnityEngine;

public abstract class MessageResponder : MonoBehaviour
{
    protected CoroutineQueue _coroutineQueue = null;

    private void OnDestroy()
    {
	    IRCConnection.Instance?.OnMessageReceived.RemoveListener(OnInternalMessageReceived);
    }

    public void SetupResponder(CoroutineQueue coroutineQueue)
	{
        _coroutineQueue = coroutineQueue;

	    IRCConnection.Instance?.OnMessageReceived.AddListener(OnInternalMessageReceived);
    }

    public static bool IsAuthorizedDefuser(string userNickName, bool silent=false)
    {
	    bool result = UserAccess.IsBanned(userNickName, out string moderator, out string reason, out double expiry);
	    if (result)
	    {
		    if (silent) return false;

		    if (double.IsPositiveInfinity(expiry))
		    {
			    IRCConnection.Instance?.SendMessage("Sorry @{0}, You were banned permanently from Twitch Plays by {1}{2}", userNickName, moderator, string.IsNullOrEmpty(reason) ? "." : $", for the following reason: {reason}");
		    }
		    else
		    {
			    int secondsRemaining = (int)(expiry - DateTime.Now.TotalSeconds());
				int daysRemaining = secondsRemaining / 86400; secondsRemaining %= 86400;
			    int hoursRemaining = secondsRemaining / 3600; secondsRemaining %= 3600;
			    int minutesRemaining = secondsRemaining / 60; secondsRemaining %= 60;
			    string timeRemaining = $"{secondsRemaining} seconds.";
			    if (daysRemaining > 0) timeRemaining = $"{daysRemaining} days, {hoursRemaining} hours, {minutesRemaining} minutes, {secondsRemaining} seconds.";
				else if (hoursRemaining > 0) timeRemaining = $"{hoursRemaining} hours, {minutesRemaining} minutes, {secondsRemaining} seconds.";
				else if (minutesRemaining > 0) timeRemaining = $"{minutesRemaining} minutes, {secondsRemaining} seconds.";

				IRCConnection.Instance?.SendMessage("Sorry @{0}, You were timed out from Twitch Plays by {1}{2} You can participate again in {3}", userNickName, moderator, string.IsNullOrEmpty(reason) ? "." : $", For the following reason: {reason}", timeRemaining);
		    }
		    return false;
	    }

        result = (TwitchPlaySettings.data.EnableTwitchPlaysMode || UserAccess.HasAccess(userNickName, AccessLevel.Defuser, true));
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
