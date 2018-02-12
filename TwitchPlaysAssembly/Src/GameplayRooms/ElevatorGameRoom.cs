using System;
using System.Collections;
using System.Collections.Generic;
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
		TwitchBombHandle bombHandle = BombMessageResponder.Instance.BombHandles[0];
		TimerComponent timerComponent = bombHandle.bombCommander.timerComponent;
		yield return new WaitUntil(() => timerComponent.IsActive);
		BombMessageResponder.Instance.OnLightsChange(true);


		_elevatorRoom.PacingActions.RemoveAll(action => action.EventType == PaceEvent.OneMinuteLeft);
		while (!bombHandle.bombCommander.Bomb.HasDetonated)
		{
			if (Input.GetKey(KeyCode.Escape))
			{
				IEnumerator bombDrop = bombHandle.OnMessageReceived(bombHandle.nameText.text, "red", "bomb drop");
				while (bombDrop.MoveNext())
					yield return bombDrop.Current;
			}

			if (bombHandle.bombCommander.Bomb.IsSolved())
				yield break;
			ToggleEmergencyLights(SceneManager.Instance.GameplayState.Mission.PacingEventsEnabled && 
				bombHandle.bombCommander.CurrentTimer < 60f && !bombHandle.bombCommander.Bomb.IsSolved());
			yield return null;
		}
	}

	public bool EmergencyLightsState = false;
	public void ToggleEmergencyLights(bool on)
	{
		if (EmergencyLightsState == on) return;
		EmergencyLightsState = on;
		if (EmergencyLightsState)
			_elevatorRoom.EmergencyLight.Activate();
		else
			_elevatorRoom.EmergencyLight.Deactivate();
	}

	private ElevatorRoom _elevatorRoom;
}
