using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

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

	[HideInInspector]
	public TPElevatorSwitch ElevatorSwitch;

	private void Start()
	{
		ElevatorSwitch = GetComponentInChildren<TPElevatorSwitch>(true);
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
			if (ElevatorSwitch.gameObject.activeSelf)
				ElevatorSwitch.ElevatorSwitch.OnInteract();
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
