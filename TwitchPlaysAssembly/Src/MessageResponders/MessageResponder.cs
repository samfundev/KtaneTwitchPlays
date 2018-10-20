using System;
using System.Linq;
using UnityEngine;

public abstract class MessageResponder : MonoBehaviour
{
	protected CoroutineQueue CoroutineQueue;

	private void OnDestroy()
	{
		if (IRCConnection.Instance == null) return;
		IRCConnection.Instance.OnMessageReceived.RemoveListener(OnInternalMessageReceived);
	}

	public void SetupResponder(CoroutineQueue coroutineQueue)
	{
		CoroutineQueue = coroutineQueue;
		if (IRCConnection.Instance == null) return;
		IRCConnection.Instance.OnMessageReceived.AddListener(OnInternalMessageReceived);
	}

	public static bool IsAuthorizedDefuser(string userNickName, bool isWhisper, bool silent = false)
	{
		if (userNickName.EqualsAny("Bomb Factory", TwitchPlaySettings.data.TwitchPlaysDebugUsername) || BombMessageResponder.Instance.BombHandles.Any(x => x.BombName == userNickName))
			return true;
		BanData ban = UserAccess.IsBanned(userNickName);
		if (ban != null)
		{
			if (silent) return false;

			if (double.IsPositiveInfinity(ban.BanExpiry))
			{
				IRCConnection.SendMessage($"Sorry @{userNickName}, You were banned permanently from Twitch Plays by {ban.BannedBy}{(string.IsNullOrEmpty(ban.BannedReason) ? "." : $", for the following reason: {ban.BannedReason}")}", userNickName, !isWhisper);
			}
			else
			{
				int secondsRemaining = (int) (ban.BanExpiry - DateTime.Now.TotalSeconds());

				int daysRemaining = secondsRemaining / 86400; secondsRemaining %= 86400;
				int hoursRemaining = secondsRemaining / 3600; secondsRemaining %= 3600;
				int minutesRemaining = secondsRemaining / 60; secondsRemaining %= 60;
				string timeRemaining = $"{secondsRemaining} seconds.";
				if (daysRemaining > 0) timeRemaining = $"{daysRemaining} days, {hoursRemaining} hours, {minutesRemaining} minutes, {secondsRemaining} seconds.";
				else if (hoursRemaining > 0) timeRemaining = $"{hoursRemaining} hours, {minutesRemaining} minutes, {secondsRemaining} seconds.";
				else if (minutesRemaining > 0) timeRemaining = $"{minutesRemaining} minutes, {secondsRemaining} seconds.";

				IRCConnection.SendMessage($"Sorry @{userNickName}, You were timed out from Twitch Plays by {ban.BannedBy}{(string.IsNullOrEmpty(ban.BannedReason) ? "." : $", For the following reason: {ban.BannedReason}")} You can participate again in {timeRemaining}", userNickName, !isWhisper);
			}
			return false;
		}

		bool result = (TwitchPlaySettings.data.EnableTwitchPlaysMode || UserAccess.HasAccess(userNickName, AccessLevel.Defuser, true));
		if (!result && !silent)
			IRCConnection.SendMessage(string.Format(TwitchPlaySettings.data.TwitchPlaysDisabled, userNickName), userNickName, !isWhisper);

		return result;
	}

	protected abstract void OnMessageReceived(Message message);

	private void OnInternalMessageReceived(Message message)
	{
		if (gameObject.activeInHierarchy && isActiveAndEnabled)
		{
			OnMessageReceived(message);
		}
	}
}
