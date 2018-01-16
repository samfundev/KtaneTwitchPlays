using System;

public class DefaultGameRoom : GameRoom
{
    public static Type RoomType()
    {
        return typeof(GameplayRoom);
    }

    public static bool TryCreateRoom(UnityEngine.Object[] roomObjects, out GameRoom room)
    {
        if (roomObjects == null || roomObjects.Length == 0)
        {
            room = null;
            return false;
        }
        else
        {
            room = new DefaultGameRoom(roomObjects[0]);
            return true;
        }
            
    }

    private DefaultGameRoom(UnityEngine.Object roomObjects)
    {
        DebugHelper.Log("Found gameplay room of type Gameplay Room");
    }
    
}
