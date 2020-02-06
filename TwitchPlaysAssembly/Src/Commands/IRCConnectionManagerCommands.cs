using System;
using System.Collections;

/// <summary>Commands for the IRC Connection Holdable.</summary>
public static class IRCConnectionManagerCommands
{
	/// <name>Disconnect</name>
	/// <syntax>disconnect</syntax>
	/// <summary>Presses the disconnect button.</summary>
	/// <restriction>Streamer</restriction>
	[Command(@"disconnect")]
	public static IEnumerator Disconnect(TwitchHoldable holdable, string user, bool isWhisper) =>
		holdable.RespondToCommand(user, string.Empty, isWhisper, Disconnect(holdable.Holdable.GetComponent<IRCConnectionManagerHoldable>()));

	private static IEnumerator Disconnect(IRCConnectionManagerHoldable holdable)
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
