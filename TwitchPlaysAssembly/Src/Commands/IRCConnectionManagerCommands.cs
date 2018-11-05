using System;
using System.Collections;

/// <summary>Commands for the IRC Connection Holdable.</summary>
public static class IRCConnectionManagerCommands
{
	[Command(@"help")]
	public static IEnumerator Help()
	{
		yield return "sendtochat Disconnect the IRC from Twitch Plays with “!{0} disconnect”. For obvious reasons, only the streamer may do this.";
	}

	[Command(@"disconnect")]
	public static IEnumerator Disconnect(IRCConnectionManagerHoldable holdable)
	{
		bool allowed = false;
		yield return null;
		yield return new object[]
		{
			"streamer",
			new Action(() =>
			{
				allowed = true;
				holdable.ConnectButton.OnInteract();
				holdable.ConnectButton.OnInteractEnded();
			}),
			new Action(() => Audio.PlaySound(KMSoundOverride.SoundEffect.Strike, holdable.transform))
		};
		if (!allowed)
			yield return "sendtochaterror only the streamer may use the IRC disconnect button.";
	}
}
