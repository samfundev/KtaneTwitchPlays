using System;
using Object = UnityEngine.Object;

public class DefaultGameRoom : GameRoom
{
	//The one catch-all room that as of now, should never be reached unless the game developers add in a new room type in the future.
	public static Type RoomType() => typeof(GameplayRoom);

	public static bool TryCreateRoom(Object[] roomObjects, out GameRoom room)
	{
		if (roomObjects == null || roomObjects.Length == 0)
		{
			room = null;
			return false;
		}

		room = new DefaultGameRoom(roomObjects[0]);
		return true;
	}

	// ReSharper disable once UnusedParameter.Local
	private DefaultGameRoom(Object roomObjects)
	{
		DebugHelper.Log("Found gameplay room of type Gameplay Room");
	}
}
