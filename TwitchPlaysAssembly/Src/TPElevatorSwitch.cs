using System;
using System.Collections;
using UnityEngine;

public class TPElevatorSwitch : MonoBehaviour
{
	public KMSelectable ElevatorSwitch;
	public GameObject ElevatorLEDOn;
	public GameObject ElevatorLEDOff;

	[HideInInspector]
	public GameObject ElevatorRoomGameObject;

	public static TPElevatorSwitch Instance;

	private void Awake() => Instance = this;

	private void Start()
	{
		Room room = SceneManager.Instance.CurrentRoom;
		switch (room)
		{
			case SetupRoom setupRoom:
				StartCoroutine(SetupRoomElevatorSwitch());
				break;
			default:
				gameObject.SetActive(false);
				break;
		}
	}

	private IEnumerator SetupRoomElevatorSwitch()
	{
		SetupRoom setupRoom = SceneManager.Instance.CurrentRoom as SetupRoom;
		if (setupRoom == null) yield break;
		try
		{
			ElevatorRoomGameObject = Resources.Load<GameObject>("PC/Prefabs/ElevatorRoom/ElevatorBombRoom");
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "Can't load the Elevator room prefab.");
			gameObject.SetActive(false);
			yield break;
		}
		if (setupRoom.ElevatorSwitch == null)
		{
			ElevatorSwitch.OnInteract += OnInteract;
			ElevatorLEDOn.SetActive(IsON);
			ElevatorLEDOff.SetActive(!IsON);
			ElevatorSwitch.transform.localEulerAngles = new Vector3(IsON ? 55 : -55, 0, 0);
			yield break;
		}

		ElevatorSwitch elevatorSwitch = setupRoom.ElevatorSwitch;
		DebugHelper.Log("Found an Elevator switch, Activating it now");
		try
		{
			elevatorSwitch.GetComponentInChildren<Selectable>(true).SelectableArea.ActivateSelectableArea();
			elevatorSwitch.Switch.SetInitialState(GameplayState.GameplayRoomPrefabOverride != null);
			elevatorSwitch.LEDOn.SetActive(GameplayState.GameplayRoomPrefabOverride != null);
			elevatorSwitch.LEDOff.SetActive(GameplayState.GameplayRoomPrefabOverride == null);
			elevatorSwitch.Switch.OnToggle += toggleState =>
			{
				DebugHelper.Log($"Toggle State = {toggleState}");
				if (elevatorSwitch.On() && IRCConnection.Instance.State != IRCConnectionState.Connected)
				{
					elevatorSwitch.Switch.Toggle();
					Audio.PlaySound(KMSoundOverride.SoundEffect.Strike, elevatorSwitch.Switch.transform);
					return;
				}

				GameplayState.GameplayRoomPrefabOverride = toggleState ? ElevatorRoomGameObject : null;
				IRCConnection.SendMessage("Elevator is {0}", GameplayState.GameplayRoomPrefabOverride == null ? (ElevatorRoomGameObject == null ? "not loaded" : "off") : "on");
				elevatorSwitch.LEDOn.SetActive(GameplayState.GameplayRoomPrefabOverride != null);
				elevatorSwitch.LEDOff.SetActive(GameplayState.GameplayRoomPrefabOverride == null);
			};
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "Could not activate elevator switch due to an exception:");
			yield break;
		}
		elevatorSwitch.gameObject.SetActive(true);
		yield return null;
		yield return null;
		elevatorSwitch.gameObject.SetActive(true);
		gameObject.SetActive(false);
	}

	public void ReportState() => IRCConnection.SendMessage("Elevator is {0}", GameplayState.GameplayRoomPrefabOverride == null ? (ElevatorRoomGameObject == null ? "not loaded" : "off") : "on");

	private IEnumerator FlipSwitch(bool on)
	{
		if (on == IsON) yield break;
		GameplayState.GameplayRoomPrefabOverride = on ? ElevatorRoomGameObject : null;
		ReportState();

		Audio.PlaySound(KMSoundOverride.SoundEffect.ButtonPress, transform);
		float initialTime = Time.time;
		const float duration = 0.25f;
		while ((Time.time - initialTime) < duration)
		{
			float lerp = (Time.time - initialTime) / duration;
			ElevatorSwitch.transform.localEulerAngles = new Vector3(Mathf.SmoothStep(on ? -55 : 55, on ? 55 : -55, lerp), 0, 0);
			ElevatorLEDOn.SetActive(Mathf.SmoothStep(on ? -55 : 55, on ? 55 : -55, lerp) >= 0);
			ElevatorLEDOff.SetActive(Mathf.SmoothStep(on ? -55 : 55, on ? 55 : -55, lerp) <= 0);
			yield return null;
		}
		ElevatorSwitch.transform.localEulerAngles = new Vector3(on ? 55 : -55, 0, 0);
		ElevatorLEDOn.SetActive(on);
		ElevatorLEDOff.SetActive(!on);
	}

	private bool OnInteract()
	{
		if (IsON)
			StartCoroutine(FlipSwitch(false));
		else
		{
			if (IRCConnection.Instance.State == IRCConnectionState.Connected)
				StartCoroutine(FlipSwitch(true));
			else
				Audio.PlaySound(KMSoundOverride.SoundEffect.Strike, transform);
		}

		return false;
	}

	public IEnumerator ToggleSetupRoomElevatorSwitch(bool elevatorState)
	{
		var setupRoom = SceneManager.Instance.CurrentRoom as SetupRoom;
		if (setupRoom == null || setupRoom.ElevatorSwitch == null)
			yield break;
		var elevatorSwitch = setupRoom.ElevatorSwitch;

		DebugHelper.Log($"Setting Elevator state to {elevatorState}");

		IEnumerator dropHoldables = TwitchPlaysService.Instance.DropAllHoldables();
		while (dropHoldables.MoveNext())
			yield return dropHoldables.Current;
		yield return new WaitForSeconds(0.25f);

		const float duration = 2f;
		GameRoom.ToggleCamera(false);
		yield return null;
		float initialTime = Time.time;
		Vector3 currentWallPosition = new Vector3(0, 0, 0);
		Vector3 currentWallRotation = new Vector3(26.39f, 0, 0);
		Vector3 newWallPosition = new Vector3(-0.6f, -1f, 0.3f);
		Vector3 newWallRotation = new Vector3(0, 40, 0);
		Transform camera = GameRoom.SecondaryCamera.transform;
		while ((Time.time - initialTime) < duration)
		{
			float lerp = (Time.time - initialTime) / duration;
			camera.localPosition = new Vector3(Mathf.SmoothStep(currentWallPosition.x, newWallPosition.x, lerp),
				Mathf.SmoothStep(currentWallPosition.y, newWallPosition.y, lerp),
				Mathf.SmoothStep(currentWallPosition.z, newWallPosition.z, lerp));
			camera.localEulerAngles = new Vector3(Mathf.SmoothStep(currentWallRotation.x, newWallRotation.x, lerp),
				Mathf.SmoothStep(currentWallRotation.y, newWallRotation.y, lerp),
				Mathf.SmoothStep(currentWallRotation.z, newWallRotation.z, lerp));
			yield return null;
		}
		camera.localPosition = newWallPosition;
		camera.localEulerAngles = newWallRotation;
		yield return new WaitForSeconds(0.5f);
		DebugHelper.Log("Elevator Switch Toggled");
		if (elevatorState != elevatorSwitch.On())
			elevatorSwitch.Switch.Toggle();
		else
			ReportState();

		yield return new WaitForSeconds(0.5f);

		initialTime = Time.time;
		while ((Time.time - initialTime) < duration)
		{
			float lerp = (Time.time - initialTime) / duration;
			camera.localPosition = new Vector3(Mathf.SmoothStep(newWallPosition.x, currentWallPosition.x, lerp),
				Mathf.SmoothStep(newWallPosition.y, currentWallPosition.y, lerp),
				Mathf.SmoothStep(newWallPosition.z, currentWallPosition.z, lerp));
			camera.localEulerAngles = new Vector3(Mathf.SmoothStep(newWallRotation.x, currentWallRotation.x, lerp),
				Mathf.SmoothStep(newWallRotation.y, currentWallRotation.y, lerp),
				Mathf.SmoothStep(newWallRotation.z, currentWallRotation.z, lerp));
			yield return null;
		}
		camera.localPosition = currentWallPosition;
		camera.localEulerAngles = currentWallRotation;
		yield return null;
		DebugHelper.Log("Finished");
		GameRoom.ToggleCamera(true);
	}

	public IEnumerator ProcessElevatorCommand(string[] split)
	{
		if (ElevatorRoomGameObject == null) yield break;
		IEnumerator toggleSwitch = null;
		if (split[0] == "elevator")
		{
			switch (split.Length)
			{
				case 2:
					// ReSharper disable once SwitchStatementMissingSomeCases
					switch (split[1])
					{
						case "on" when !IsON:
						case "off" when IsON:
						case "toggle":
						case "switch":
						case "press":
						case "push":
						case "flip":
							toggleSwitch = ToggleSetupRoomElevatorSwitch(!IsON);
							break;
						case "on":
						case "off":
							toggleSwitch = ToggleSetupRoomElevatorSwitch(IsON);
							break;
					}

					break;
				case 1:
					toggleSwitch = ToggleSetupRoomElevatorSwitch(IsON);
					break;
			}
		}
		while (toggleSwitch != null && toggleSwitch.MoveNext())
			yield return toggleSwitch.Current;
	}

	public static bool IsON => GameplayState.GameplayRoomPrefabOverride != null;
}
