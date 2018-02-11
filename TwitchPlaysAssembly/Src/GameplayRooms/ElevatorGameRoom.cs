using System;
using System.Collections;
using UnityEngine;

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
		_elevatorRoom = roomObjects;
		HoldBomb = false;
	}

	
	public override IEnumerator ReportBombStatus()
	{
		Camera camera = Camera.main;
		while (true)
		{


			yield return null;
		}


		yield break;
	}

	private UnityEngine.Object _elevatorRoom;
}
