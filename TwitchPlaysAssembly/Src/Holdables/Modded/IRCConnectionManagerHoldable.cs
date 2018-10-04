using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class IRCConnectionManagerHandler : HoldableHandler
{
	public IRCConnectionManagerHandler(KMHoldableCommander commander, FloatingHoldable holdable) : base(commander, holdable)
	{
		_connectButton = holdable.GetComponent<IRCConnectionManagerHoldable>().ConnectButton;
		_elevatorSwitch = TPElevatorSwitch.Instance;
		HelpMessage = "Disconnect the IRC from twitch plays with !{0} disconnect. For obvious reasons, only the streamer may do this.";
		if (_elevatorSwitch != null && _elevatorSwitch.gameObject.activeInHierarchy)
		{
			HelpMessage += " Turn the elevator on with !{0} elevator on. Turn the Elevator off with !{0} elevator off. Flip the elevator on/off with !{0} elevator toggle";
		}
		if (commander != null)
			commander.ID = "ircmanager";
		Instance = this;
	}

	protected override IEnumerator RespondToCommandInternal(string command, bool isWhisper)
	{
		DebugHelper.Log($"Received: !ircmanager {command}");

		string[] split = command.ToLowerInvariant().Trim().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
		// ReSharper disable SwitchStatementMissingSomeCases
		switch (split[0])
		{
			case "elevator" when _elevatorSwitch != null && _elevatorSwitch.gameObject.activeInHierarchy:
				switch (split.Length)
				{
					case 1:
						yield return null;
						TPElevatorSwitch.Instance.ReportState();
						yield return null;
						break;
					case 2:
						switch (split[1])
						{
							case "on" when !TPElevatorSwitch.IsON:
							case "off" when TPElevatorSwitch.IsON:
							case "flip":
							case "toggle":
							case "switch":
							case "press":
							case "push":
								yield return null;
								_elevatorSwitch.ElevatorSwitch.OnInteract();
								yield return new WaitForSeconds(0.1f);
								break;
							case "on":
							case "off":
								yield return null;
								TPElevatorSwitch.Instance.ReportState();
								yield return null;
								break;
						}

						break;
				}

				break;
			case "disconnect":
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
					new Action(() => Audio.PlaySound(KMSoundOverride.SoundEffect.Strike, Holdable.transform))
				};
				if (allowed) yield break;
				yield return "sendtochaterror only the streamer may use the IRC disconnect button.";
				break;
		}
		// ReSharper restore SwitchStatementMissingSomeCases
	}

	public static IRCConnectionManagerHandler Instance;
	private readonly KMSelectable _connectButton;
	private readonly TPElevatorSwitch _elevatorSwitch;
}

public class IRCConnectionManagerHoldable : MonoBehaviour
{
	public KMSelectable ConnectButton;
	public TextMesh ConnectButtonText;
	public TextMesh IRCText;

	private static bool _imageLoadingDisabled = false;
	private GameObject _newImageGameObject;
	private GameObject _originalImageGameObject;

	[HideInInspector]
	public static List<string> IRCTextToDisplay = new List<string>();

	[HideInInspector]
	public static bool TwitchPlaysDataRefreshed = false;

	private TPElevatorSwitch _elevatorSwitch;

	private void Start()
	{
		_elevatorSwitch = GetComponentInChildren<TPElevatorSwitch>(true);
		_originalImageGameObject = transform.Find("TPQuad").gameObject;
		_newImageGameObject = Instantiate(_originalImageGameObject, transform);

		ConnectButton.OnInteract += ConnectDisconnect;
		ConnectButton.OnInteractEnded += () => Audio.PlaySound(KMSoundOverride.SoundEffect.ButtonRelease, transform);
		StartCoroutine(RefreshIRCBackground());
	}

	private bool ConnectDisconnect()
	{
		Audio.PlaySound(KMSoundOverride.SoundEffect.ButtonPress, transform);
		if (IRCConnection.Instance == null) return false;

		if (IRCConnection.Instance.State == IRCConnectionState.Disconnected)
			IRCConnection.Connect();
		else
			IRCConnection.Disconnect();

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
#pragma warning disable IDE0031 // Use null propagation
			MeshRenderer background = _newImageGameObject != null ? _newImageGameObject.GetComponent<MeshRenderer>() : null;
#pragma warning restore IDE0031 // Use null propagation
			//Image loading at runtime found at https://forum.unity.com/threads/solved-apply-image-to-plane-primitive.320489/
			if (background == null || !File.Exists(TwitchPlaySettings.data.IRCManagerBackgroundImage))
			{
				_originalImageGameObject.SetActive(true);
				if (_newImageGameObject != null)
					_newImageGameObject.SetActive(false);
				yield break;
			}

			_originalImageGameObject.SetActive(false);
			if (_newImageGameObject != null)
				_newImageGameObject.SetActive(true);

			byte[] bytes = File.ReadAllBytes(TwitchPlaySettings.data.IRCManagerBackgroundImage);
			Texture2D tex = new Texture2D(1, 1);
			tex.LoadImage(bytes);
			background.material.mainTexture = tex;
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "Could NOT set a custom background image due to an exception. Feature is being disabled for the rest of this session:");
			_imageLoadingDisabled = true;
			TwitchPlaySettings.WriteDataToFile();
			_originalImageGameObject.SetActive(true);
			if (_newImageGameObject != null)
				_newImageGameObject.SetActive(false);
		}
	}

	private void Update()
	{
		if (IRCConnection.Instance.State != IRCConnectionState.Connected && TPElevatorSwitch.IsON)
		{
			if (_elevatorSwitch.gameObject.activeSelf)
				_elevatorSwitch.ElevatorSwitch.OnInteract();
			else if (SceneManager.Instance.CurrentRoom is SetupRoom setupRoom && setupRoom.ElevatorSwitch != null)
				setupRoom.ElevatorSwitch.Switch.Toggle();
		}
		ConnectButtonText.text = IRCConnection.Instance.State.ToString();
		if (IRCTextToDisplay.Count > 28)
			IRCTextToDisplay = IRCTextToDisplay.TakeLast(28).ToList();
		IRCText.text = string.Join("\n", IRCTextToDisplay.ToArray());

		if (!TwitchPlaysDataRefreshed) return;
		StartCoroutine(RefreshIRCBackground());
		TwitchPlaysDataRefreshed = false;
	}
}
