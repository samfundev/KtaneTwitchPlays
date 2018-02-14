using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class MissionMessageResponder : MessageResponder
{
    private BombBinderCommander _bombBinderCommander = null;
    private FreeplayCommander _freeplayCommander = null;
	private GameObject _elevatorRoom = null;
	private ElevatorSwitch _elevatorSwitch = null;

    #region Unity Lifecycle
    private void OnEnable()
    {
		// InputInterceptor.DisableInput();

		StartCoroutine(CheckForBombBinderAndFreeplayDevice());
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        _bombBinderCommander = null;
        _freeplayCommander = null;
    }
    #endregion

    #region Protected/Private Methods
    private IEnumerator CheckForBombBinderAndFreeplayDevice()
    {
        yield return null;

        while (true)
        {
            BombBinder[] bombBinders = FindObjectsOfType<BombBinder>();
            if (bombBinders != null && bombBinders.Length > 0)
            {
                _bombBinderCommander = new BombBinderCommander(bombBinders[0]);
                break;
            }

            yield return null;
        }

        while (true)
        {
            FreeplayDevice[] freeplayDevices = FindObjectsOfType<FreeplayDevice>();
            if (freeplayDevices != null && freeplayDevices.Length > 0)
            {
                _freeplayCommander = new FreeplayCommander(freeplayDevices[0]);
                break;
            }

            yield return null;
        }

	    SetupRoom setupRoom = (SetupRoom)SceneManager.Instance.CurrentRoom;
	    DebugHelper.PrintTree(setupRoom.transform, new []{typeof(FloatingHoldable)}, true);
	    ElevatorSwitch elevatorSwitch = setupRoom.ElevatorSwitch;
	    if (elevatorSwitch != null)
	    {
		    DebugHelper.Log("Found an Elevator switch, Activating it now");
		    bool noException = true;
		    try
		    {
			    _elevatorSwitch = elevatorSwitch;
			    elevatorSwitch.GetComponentInChildren<Selectable>(true).SelectableArea.ActivateSelectableArea();
			    elevatorSwitch.Switch.SetInitialState(GameplayState.GameplayRoomPrefabOverride != null);
			    SetLEDElevatorSwitch(GameplayState.GameplayRoomPrefabOverride != null);
			    elevatorSwitch.Switch.OnToggle += OnToggleElevatorSwitch;
			    _elevatorRoom = Resources.Load<GameObject>("PC/Prefabs/ElevatorRoom/ElevatorBombRoom");
			}
		    catch (Exception ex)
		    {
			    _elevatorSwitch = null;
				DebugHelper.LogException(ex,"Could not activate elevator switch due to an exception:");
			    noException = false;
		    }

		    if (noException)
		    {
				elevatorSwitch.gameObject.SetActive(true);
			    yield return null;
			    yield return null;
			    elevatorSwitch.gameObject.SetActive(true);
		    }
	    }
	    else
	    {
		    DebugHelper.Log("No Elevator switch found");
		    try
		    {
			    _elevatorRoom = Resources.Load<GameObject>("PC/Prefabs/ElevatorRoom/ElevatorBombRoom");
			    DebugHelper.Log("Elevator room loaded successfully");
		    }
		    catch (Exception ex)
		    {
			    DebugHelper.LogException(ex, "Failed to load the Elevator room");
			    GameplayState.GameplayRoomPrefabOverride = null;
		    }
		}

	    
    }

	private IEnumerator ToggleElevatorSwitch(bool elevatorState)
	{
		DebugHelper.Log("Setting Elevator state to {0}", elevatorState);
		if (_elevatorSwitch == null)
		{
			OnToggleElevatorSwitch(elevatorState);
			yield break;
		}
		float duration = 2f;
		GameRoom.ToggleCamera(false);
		yield return null;
		float initialTime = Time.time;
		Vector3 currentWallPosition = new Vector3(0,0,0);
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
		if (elevatorState != _elevatorSwitch.On())
		{
			_elevatorSwitch.Switch.Toggle();
		}
		else
		{
			OnToggleElevatorSwitch(elevatorState);
		}
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

	private void SetLEDElevatorSwitch(bool state)
	{
		_elevatorSwitch?.LEDOn.SetActive(state);
		_elevatorSwitch?.LEDOff.SetActive(!state);
	}

	private void OnToggleElevatorSwitch(bool toggleState)
	{
		GameplayState.GameplayRoomPrefabOverride = toggleState ? _elevatorRoom : null;
		IRCConnection.Instance.SendMessage("Elevator is {0}", GameplayState.GameplayRoomPrefabOverride == null ? (_elevatorRoom == null ? "not loaded" : "off") : "on");
		SetLEDElevatorSwitch(toggleState);
	}

	protected override void OnMessageReceived(string userNickName, string userColorCode, string text)
	{
		if (_bombBinderCommander == null)
		{
			return;
		}

		if (!text.StartsWith("!") || text.Equals("!")) return;
		text = text.Substring(1);

		string[] split = text.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		string textAfter = split.Skip(1).Join();
		switch (split[0])
		{
			case "binder":
				if ((TwitchPlaySettings.data.EnableMissionBinder && TwitchPlaySettings.data.EnableTwitchPlaysMode) || UserAccess.HasAccess(userNickName, AccessLevel.Admin, true))
				{
					_coroutineQueue.AddToQueue(_bombBinderCommander.RespondToCommand(userNickName, textAfter, null));
				}
				else
				{
					IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.MissionBinderDisabled, userNickName);
				}
				break;
			case "freeplay":
				if((TwitchPlaySettings.data.EnableFreeplayBriefcase && TwitchPlaySettings.data.EnableTwitchPlaysMode) || UserAccess.HasAccess(userNickName, AccessLevel.Admin, true))
				{
					_coroutineQueue.AddToQueue(_freeplayCommander.RespondToCommand(userNickName, textAfter, null));
				}
				else
				{
					IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.FreePlayDisabled, userNickName);
				}
				break;

			case "elevator":
				if (split.Length == 2)
				{
					switch (split[1])
					{
						case "on":
							DebugHelper.Log("Adding to queue");
							_coroutineQueue.AddToQueue(ToggleElevatorSwitch(true));
							break;
						case "off":
							DebugHelper.Log("Adding to queue");
							_coroutineQueue.AddToQueue(ToggleElevatorSwitch(false));
							break;
						case "toggle":
						case "switch":
						case "press":
						case "flip":
							DebugHelper.Log("Adding to queue");
							_coroutineQueue.AddToQueue(ToggleElevatorSwitch(GameplayState.GameplayRoomPrefabOverride == null));
							break;
					}
				}
				else if (split.Length == 1)
				{
					DebugHelper.Log("Adding to queue");
					_coroutineQueue.AddToQueue(ToggleElevatorSwitch(GameplayState.GameplayRoomPrefabOverride != null));
				}
				break;
			
			default:
				break;
		}
	}
    #endregion
}
