using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Pacing;
using UnityEngine;
using Random = System.Random;

public class ElevatorGameRoom : GameRoom
{
	public static Type RoomType()
	{
		return typeof(ElevatorRoom);
	}

	public static bool TryCreateElevatorRoom(UnityEngine.Object[] roomObjects, out GameRoom room)
	{
		if (roomObjects == null || roomObjects.Length == 0)
		{
			room = null;
			return false;
		}
		else
		{
			room = new ElevatorGameRoom(roomObjects[0]);
			return true;
		}

	}

	private ElevatorGameRoom(UnityEngine.Object roomObjects)
	{
		DebugHelper.Log("Found gameplay room of type Gameplay Room");
		_elevatorRoom = (ElevatorRoom)roomObjects;
	}

	public override void InitializeBombNames()
	{
		List<TwitchBombHandle> bombHandles = BombMessageResponder.Instance.BombHandles;

		Random rand = new Random();
		const float specialNameProbability = 0.25f;
		string[] singleNames =
		{
			"The Elevator of Doom",
			"The Elevator to Hell",
			"The Elevator to Heaven",
			"Bomblebee",
			"Big Bomb",
			"Big Bomb Man",
			"Explodicus",
			"Little Boy",
			"Fat Man",
			"Bombadillo",
			"The Dud",
			"Molotov",
			"Sergeant Cluster",
			"La Bomba",
			"Bombchu",
			"Bomboleo"
		};
		if (!(rand.NextDouble() < specialNameProbability)) return;
		foreach (TwitchBombHandle handle in bombHandles)
		{
			handle.nameText.text = singleNames[rand.Next(0, singleNames.Length - 1)];
		}
	}
	
	public override IEnumerator ReportBombStatus()
	{
		Camera camera = Camera.main;
		List<TwitchBombHandle> bombHandles = BombMessageResponder.Instance.BombHandles;
		if (!SceneManager.Instance.GameplayState.Mission.PacingEventsEnabled)
			yield break;

		_elevatorRoom.PacingActions.RemoveAll(action => action.EventType == PaceEvent.OneMinuteLeft);
		while (bombHandles.TrueForAll(handle => !handle.bombCommander.Bomb.HasDetonated))
		{
			if (Input.GetKey(KeyCode.Escape))
			{
				camera.transform.localEulerAngles = Vector3.zero;
				camera.transform.localPosition = Vector3.zero;
			}

			if (bombHandles.TrueForAll(handle => handle.bombCommander.Bomb.IsSolved()))
				yield break;
			ToggleEmergencyLights(bombHandles.Any(handle => handle.bombCommander.CurrentTimer < 60f && !handle.bombCommander.Bomb.IsSolved()));
			yield return null;
		}
	}

	public bool EmergencyLightsState = false;
	public void ToggleEmergencyLights(bool on)
	{
		if (EmergencyLightsState == on) return;
		EmergencyLightsState = on;
		MethodInfo method = on ? _turnOnEmergencyLightsMethod : _turnOffEmergencyLightsMethod;
		method.Invoke(_elevatorRoom, null);
	}

	static ElevatorGameRoom()
	{
		_turnOffEmergencyLightsMethod = typeof(ElevatorRoom).GetMethod("TurnOffEmergencyLights", BindingFlags.NonPublic | BindingFlags.Instance);
		_turnOnEmergencyLightsMethod = typeof(ElevatorRoom).GetMethod("TurnOnEmergencyLights", BindingFlags.NonPublic | BindingFlags.Instance);
	}

	private ElevatorRoom _elevatorRoom;
	private static readonly MethodInfo _turnOffEmergencyLightsMethod = null;
	private static readonly MethodInfo _turnOnEmergencyLightsMethod = null;
}
