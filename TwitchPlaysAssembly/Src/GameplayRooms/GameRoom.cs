using System;
using System.Collections;
using System.Collections.Generic;
using Random = System.Random;

public abstract class GameRoom
{
    public static GameRoom Instance;

    public delegate Type GameRoomType();
    public delegate bool CreateRoom(UnityEngine.Object[] roomObjects, out GameRoom room);

	protected bool ReuseBombCommander = false;
	protected int BombCount;

	public static GameRoomType[] GameRoomTypes =
    {
        //Supported Mod gameplay room types
        Factory.FactoryType,
        PortalRoom.PortalRoomType,

        //Supported vanilla gameplay room types.  (Also catches all unknown Mod gameplay rooms, as ModGameplayRoom inherits FacilityRoom)
        Facility.RoomType,
		ElevatorGameRoom.RoomType,

        //Catch all for unknown room types in the vanilla game. (As of now, the only remaining GameplayRoom type in this catch-all is ElevatorRoom)
        DefaultGameRoom.RoomType
    };

    public static CreateRoom[] CreateRooms =
    {
        Factory.TrySetupFactory,
        PortalRoom.TryCreatePortalRoom,
        Facility.TryCreateFacility,
		ElevatorGameRoom.TryCreateElevatorRoom,
        DefaultGameRoom.TryCreateRoom
    };

	public bool HoldBomb = true;

    public int BombID { get; protected set; }

    public virtual void RefreshBombID(ref int bombID)
    {
        BombID = bombID;
    }


    public virtual bool IsCurrentBomb(int bombIndex)
    {
        return true;
    }

	public virtual void InitializeBombs(List<Bomb> bombs)
	{
		int _currentBomb = bombs.Count == 1 ? -1 : 0;
		for (int i = 0; i < bombs.Count; i++)
		{
			BombMessageResponder.Instance.SetBomb(bombs[i], _currentBomb == -1 ? -1 : i);
		}
		BombCount = (_currentBomb == -1) ? -1 : bombs.Count;
	}

	protected void InitializeBomb(Bomb bomb)
	{
		if (ReuseBombCommander)
		{
			//Destroy existing component handles, and instantiate a new set.
			BombMessageResponder.Instance.BombHandles[0].bombCommander.ReuseBombCommander(bomb);
			BombMessageResponder.Instance.DestroyComponentHandles();
			BombMessageResponder.Instance.CreateComponentHandlesForBomb(bomb);
		}
		else
		{
			//Set another bomb, and add it to the list.
			BombMessageResponder.Instance.SetBomb(bomb, BombCount++);
		}
	}

    public virtual void InitializeBombNames()
    {
	    List<TwitchBombHandle> bombHandles = BombMessageResponder.Instance.BombHandles;

        Random rand = new Random();
        const float specialNameProbability = 0.25f;
        string[] singleNames = 
        {
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
        string[,] doubleNames =
        {
            {null, "The Bomb 2: Bomb Harder"},
            {null, "The Bomb 2: The Second Bombing"},
            {"Bomb", "Bomber"},
            {null, "The Bomb Reloaded"},
            {"Bombic", "& Knuckles"},
            {null, "The River Kwai"},
            {"Bomboleo", "Bombolea"}
        };

        switch (bombHandles.Count)
        {
            case 1 when rand.NextDouble() >= specialNameProbability:
                break;
            case 2 when rand.NextDouble() < specialNameProbability:
                int nameIndex = rand.Next(0, doubleNames.Length - 1);
                for (int i = 0; i < 2; i++)
                {
                    string nameText = doubleNames[nameIndex, i];
                    if (nameText != null)
                    {
                        bombHandles[i].nameText.text = nameText;
                    }
                }
                break;
            case 2:
                bombHandles[1].nameText.text = "The Other Bomb";
                break;
            default:
                foreach(TwitchBombHandle handle in bombHandles)
                {
                    handle.nameText.text = singleNames[rand.Next(0, singleNames.Length - 1)];
                }
                break;
        }
    }

    public virtual IEnumerator ReportBombStatus()
    {
        yield break;
    }
}