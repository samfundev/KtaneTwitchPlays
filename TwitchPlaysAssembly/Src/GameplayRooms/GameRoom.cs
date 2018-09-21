using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Object = UnityEngine.Object;
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
		BombMessageResponder.Instance.InitializeModuleCodes();
	}

	protected void InitializeBomb(Bomb bomb)
	{
		if (ReuseBombCommander)
		{
			//Destroy existing component handles, and instantiate a new set.
			BombMessageResponder.Instance.BombHandles[0].bombCommander.ReuseBombCommander(bomb);
			BombMessageResponder.Instance.DestroyComponentHandles();
			BombMessageResponder.Instance.CreateComponentHandlesForBomb(bomb);
			BombMessageResponder.Instance.InitializeModuleCodes();
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
		const float specialNameProbability = 1.25f;
		string[] singleNames =
		{
			"Bomblebee ",
			"Big Bomb",
			"Big Bomb Man",
			"Explodicus ",
			"Little Boy",
			"Fat Man",
			"Bombadillo ",
			"The Dud",
			"Molotov ",
			"Sergeant Cluster",
			"La Bomba",
			"Bombchu ",
			"Bomboleo "
		};
		string[,] doubleNames =
		{
			{null, "The Bomb 2: Bomb Harder"},
			{null, "The Bomb 2: The Second Bombing"},
			{"Bomb ", "Bomber "},
			{null, "The Bomb Reloaded"},
			{"Bombic ", "& Knuckles"},
			{null, "The River Kwai"},
			{"Bomboleo ", "Bombolea "}
		};

		switch (bombHandles.Count)
		{
			case 1 when rand.NextDouble() >= specialNameProbability:
				break;
			case 2 when rand.NextDouble() < specialNameProbability:
				int nameIndex = rand.Next(0, doubleNames.GetLength(0));
				for (int i = 0; i < 2; i++)
				{
					string nameText = doubleNames[nameIndex, i];
					if (nameText != null)
					{
						bombHandles[i].bombName = nameText;
					}
				}
				break;
			case 2:
				bombHandles[1].bombName = "The Other Bomb";
				break;
			default:
				foreach (TwitchBombHandle handle in bombHandles)
				{
					handle.bombName = singleNames[rand.Next(0, singleNames.Length)];
				}
				break;
		}
	}

	public virtual void OnDisable()
	{

	}

	private static Camera _mainCamera;
	public static Camera SecondaryCamera;
	public static bool IsMainCamera = true;
	public static int MainCameraCullingMask;
	public static int SecondryCameraCullingMask;
	public static void InitializeSecondaryCamera()
	{
		
		if (SecondaryCamera != null) return;
		
		GameObject customMover = new GameObject("CustomCameraMover");
		customMover.transform.SetParent(Camera.main.transform.parent.parent);
		_mainCamera = Camera.main;
		_mainCamera.cullingMask |= 0x00002000;
		SecondaryCamera = Object.Instantiate(_mainCamera, Vector3.zero, Quaternion.identity, customMover.transform);
		for (int i = 0; i < SecondaryCamera.transform.childCount; i++)
		{
			Object.DestroyImmediate(SecondaryCamera.transform.GetChild(i).gameObject);
		}
		SecondaryCamera.transform.localEulerAngles = new Vector3(26.39f, 0, 0);
		SecondaryCamera.gameObject.SetActive(false);
		DebugHelper.Log($"Main Camera Culling mask = {_mainCamera.cullingMask:X8}\nSecondary Camera Culling mask = {SecondaryCamera.cullingMask:X8}");

		MainCameraCullingMask = _mainCamera.cullingMask;
		SecondryCameraCullingMask = SecondaryCamera.cullingMask;
	}

	public static void ToggleCamera(bool main)
	{
		IsMainCamera = main;
		SecondaryCamera.gameObject.SetActive(!main);
		_mainCamera.gameObject.SetActive(main);
	}

	public static void HideCamera()
	{
		_mainCamera.cullingMask = 0;
		if(SecondaryCamera != null)
			SecondaryCamera.cullingMask = 0;
	}

	public static void ShowCamera()
	{
		_mainCamera.cullingMask = MainCameraCullingMask;
		if(SecondaryCamera != null)
			SecondaryCamera.cullingMask = SecondryCameraCullingMask;
	}

	public static void ResetCamera()
	{
		SecondaryCamera.transform.localPosition = Vector3.zero;
		SecondaryCamera.transform.localEulerAngles = new Vector3(26.39f, 0, 0);
	}

	public static void SetCameraPostion(Vector3 movement)
	{
		if (IsMainCamera) return;
		SecondaryCamera.transform.localPosition = movement;
	}

	public static void SetCameraRotation(Vector3 rotation)
	{
		if (IsMainCamera) return;
		SecondaryCamera.transform.localEulerAngles = rotation;
	}

	public static void MoveCamera(Vector3 movement)
	{
		if (IsMainCamera) return;
		Vector3 m = CurrentCameraPosition;
		SecondaryCamera.transform.localPosition = new Vector3(movement.x + m.x, movement.y + m.y, movement.z + m.z);
	}

	public static void RotateCamera(Vector3 rotation)
	{
		if (IsMainCamera) return;
		Vector3 r = CurrentCameraEulerAngles;
		SecondaryCamera.transform.localEulerAngles = new Vector3(r.x + rotation.x, r.y + rotation.y, r.z + rotation.z);
	}

	public static Vector3 CurrentCameraPosition => SecondaryCamera.transform.localPosition;

	public static Vector3 CurrentCameraEulerAngles => SecondaryCamera.transform.localEulerAngles;

	public bool InitializeOnLightsOn = true;
	public void InitializeGameModes(bool lightsOn)
	{
		if (!lightsOn) return;
		if (OtherModes.TimeModeOn)
		{
			OtherModes.SetMultiplier(TwitchPlaySettings.data.TimeModeStartingMultiplier);
		}

		List<TwitchBombHandle> bombHandles = BombMessageResponder.Instance.BombHandles;
		foreach (TwitchBombHandle handle in bombHandles)
		{
			if (OtherModes.TimeModeOn)
			{
				handle.bombCommander.timerComponent.TimeRemaining = TwitchPlaySettings.data.TimeModeStartingTime * 60;
			}
			else if (OtherModes.ZenModeOn)
			{
				handle.bombCommander.timerComponent.TimeRemaining = 1;
			}
		}
	}

	public virtual IEnumerator ReportBombStatus()
	{
		yield break;
	}

	public string[] ValidEdgeworkRegex =
	{
		"^edgework((?: 45|-45)|(?: top right| right top| right bottom| bottom right| bottom left| left bottom| left top| top left| left| top| right| bottom| tr| rt| tl| lt| br| rb| bl| lb| t| r| b| l))?$"
	};

	public virtual IEnumerator BombCommanderHoldBomb(Bomb bomb, bool frontFace = true)
	{
		yield return true;
	}

	public virtual IEnumerator BombCommanderDropBomb(Bomb bomb)
	{
		yield return true;
	}

	public virtual IEnumerator BombCommanderTurnBomb(Bomb bomb)
	{
		yield return true;
	}

	public virtual IEnumerator BombCommanderBombEdgework(Bomb bomb, Match edgeworkMatch)
	{
		yield return true;
	}

	public virtual IEnumerator BombCommanderFocus(Bomb bomb, Selectable selectable, float focusDistance, bool frontFace)
	{
		yield return true;
	}

	public virtual IEnumerator BombCommanderDefocus(Bomb bomb, Selectable selectable, bool frontFace)
	{
		yield return true;
	}

	public virtual bool BombCommanderRotateByLocalQuaternion(Bomb bomb, Quaternion localQuaternion)
	{
		return true;
	}
}
