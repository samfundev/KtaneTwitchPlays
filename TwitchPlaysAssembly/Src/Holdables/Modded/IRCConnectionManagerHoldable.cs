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

	private static bool _imageLoadingDisabled = false;
	private Transform _imageTransform;
	private Transform _originalTransform;

	[HideInInspector]
	public static List<string> IRCTextToDisplay = new List<string>();

	[HideInInspector]
	public static bool TwitchPlaysDataRefreshed = false;

	private void Start()
	{
		_originalTransform = transform.Find("TPQuad");
		_imageTransform = Instantiate(_originalTransform.gameObject, transform).transform;

		ConnectButton.OnInteract += ConnectDisconnect;
		ConnectButton.OnInteractEnded += () => MasterAudio.PlaySound3DAtTransformAndForget("press-release", transform, 1f, null, 0f, null);
		StartCoroutine(RefreshIRCBackground());
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

	private IEnumerator RefreshIRCBackground()
	{
		yield return null;

		if (_imageLoadingDisabled) yield break;
		yield return null;
		IRCText.color = TwitchPlaySettings.data.IRCManagerTextColor;
		try
		{
			//Image loading at runtime found at https://forum.unity.com/threads/solved-apply-image-to-plane-primitive.320489/
			if (!File.Exists(TwitchPlaySettings.data.IRCManagerBackgroundImage))
			{
				_originalTransform.gameObject.SetActive(true);
				_imageTransform?.gameObject.SetActive(false);
				yield break;
			}

			_originalTransform.gameObject.SetActive(false);
			_imageTransform.gameObject.SetActive(true);

			MeshRenderer background = _imageTransform.GetComponent<MeshRenderer>();
			var bytes = File.ReadAllBytes(TwitchPlaySettings.data.IRCManagerBackgroundImage);
			var tex = new Texture2D(1, 1);
			tex.LoadImage(bytes);
			background.material.mainTexture = tex;
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "Could NOT set a custom background image due to an exception. Feature is being disabled for the rest of this session:");
			_imageLoadingDisabled = true;
			TwitchPlaySettings.WriteDataToFile();
			_originalTransform.gameObject.SetActive(true);
			_imageTransform?.gameObject.SetActive(false);
		}
	}

	private void Update()
	{
		ConnectButtonText.text = IRCConnection.Instance.State.ToString();
		if (IRCTextToDisplay.Count > 28)
			IRCTextToDisplay = IRCTextToDisplay.TakeLast(28).ToList();
		IRCText.text = string.Join("\n", IRCTextToDisplay.ToArray());

		if (!TwitchPlaysDataRefreshed) return;
		StartCoroutine(RefreshIRCBackground());
		TwitchPlaysDataRefreshed = false;
	}
}
