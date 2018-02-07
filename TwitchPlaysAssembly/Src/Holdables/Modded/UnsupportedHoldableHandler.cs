using System.Collections;


public class UnsupportedHoldableHandler : HoldableHandler
{
	public UnsupportedHoldableHandler(KMHoldableCommander commander, FloatingHoldable holdable, IRCConnection connection)
		: base(commander, holdable, connection)
	{
		HelpMessage = "!{0} is not supported by Twitch Plays yet.";
	}

	protected override IEnumerator RespondToCommandInternal(string command)
	{
		ircConnection.SendMessage($"Sorry @{UserNickName}, This holdable is not supported in TwitchPlays yet.");
		yield break;
	}
}
