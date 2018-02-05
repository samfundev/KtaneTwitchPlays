using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarkTonic.MasterAudio;
using UnityEngine;

public class IRCConnectionManagerHandler : HoldableHandler
{
	public IRCConnectionManagerHandler(KMHoldableCommander commander, FloatingHoldable holdable, IRCConnection connection, CoroutineCanceller canceller) : base(commander, holdable, connection, canceller)
	{
		_connectButton = holdable.GetComponent<IRCConnectionManagerHoldable>().ConnectButton;
		HelpMessage = "Disconnect the IRC from twitch plays with !{0} disconnect.  For obvious reasons, only the streamer may do this.";
		if (commander != null)
			commander.ID = "ircmanager";
	}

	protected override IEnumerator RespondToCommandInternal(string command)
	{
		if (!command.ToLowerInvariant().Equals("disconnect")) yield break;
		bool disallowed = false;
		bool allowed = false;
		yield return null;
		yield return new object[]
		{
			"streamer",
			new Action(() =>
			{
				allowed = true;
				_connectButton.OnInteract();
				_connectButton.OnInteractEnded();
			}),
			new Action(() => disallowed = true)
		};
		while (!allowed && !disallowed) yield return null;
		if (allowed) yield break;
		MasterAudio.PlaySound3DAtTransformAndForget("strike", Holdable.transform, 1f, null, 0f, null);
		yield return "sendtochaterror only the streamer may use the IRC disconnect button.";
	}



	private readonly KMSelectable _connectButton;
}

public class IRCConnectionManagerHoldable : MonoBehaviour
{
	public KMSelectable ConnectButton;
	public TextMesh ConnectButtonText;
	public TextMesh IRCText;

	private string backgroundImage = "";
	private Color textColor = new Color(1.00f, 0.44f, 0.00f);

	[HideInInspector]
	public static List<string> ircTextToDisplay = new List<string>();

	private void Start()
	{
		ConnectButton.OnInteract += ConnectDisconnect;
		ConnectButton.OnInteractEnded += () => MasterAudio.PlaySound3DAtTransformAndForget("press-release", transform, 1f, null, 0f, null);

	}

	private bool ConnectDisconnect()
	{
		MasterAudio.PlaySound3DAtTransformAndForget("press-in", transform, 1f, null, 0f, null);
		if (IRCConnection.Instance == null) return false;

		if (IRCConnection.Instance.State == IRCConnectionState.Disconnected)
		{
			IRCConnection.Instance.Connect();
		}
		else
		{
			IRCConnection.Instance.Disconnect();
		}

		return false;
	}

	private void Update()
	{
		bool forceRefresh = ircTextToDisplay.Contains("TWITCHPLAYS DATA RELOADED");
		ircTextToDisplay.Remove("TWITCHPLAYS DATA RELOADED");

		ConnectButtonText.text = IRCConnection.Instance.State.ToString();
		if (ircTextToDisplay.Count > 28)
			ircTextToDisplay = ircTextToDisplay.TakeLast(28).ToList();
		IRCText.text = string.Join("\n", ircTextToDisplay.ToArray());

		if (textColor != TwitchPlaySettings.data.IRCManagerTextColor || forceRefresh)
		{
			textColor = TwitchPlaySettings.data.IRCManagerTextColor;
			IRCText.color = textColor;
		}

		if (backgroundImage != TwitchPlaySettings.data.IRCManagerBackgroundImage || forceRefresh)
		{
			backgroundImage = TwitchPlaySettings.data.IRCManagerBackgroundImage;
			//Image loading at runtime found at https://forum.unity.com/threads/solved-apply-image-to-plane-primitive.320489/
			if (File.Exists(backgroundImage))
			{
				MeshRenderer background = transform.Find("TPQuad").GetComponent<MeshRenderer>();
				var bytes = File.ReadAllBytes(backgroundImage);
				var tex = new Texture2D(1, 1);
				tex.LoadImage(bytes);
				background.material.mainTexture = tex;
			}
		}

	}
}
