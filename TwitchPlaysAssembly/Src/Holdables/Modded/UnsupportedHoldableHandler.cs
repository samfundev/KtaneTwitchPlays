using System.Collections;

public class UnsupportedHoldableHandler : HoldableHandler
{
	public UnsupportedHoldableHandler(KMHoldableCommander commander, FloatingHoldable holdable)
		: base(commander, holdable)
	{
		HelpMessage = "!{0} is not supported by Twitch Plays yet.";
	}

	protected override IEnumerator RespondToCommandInternal(string command, bool isWhisper)
	{
		IRCConnection.Instance.SendMessage($"Sorry @{UserNickName}, This holdable is not supported in TwitchPlays yet.", UserNickName, !isWhisper);
		yield break;
	}
}
