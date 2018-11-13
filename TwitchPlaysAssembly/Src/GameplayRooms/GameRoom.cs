using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

public abstract class GameRoom
{
	public static GameRoom Instance;

	public delegate Type GameRoomType();
	public delegate bool CreateRoom(Object[] roomObjects, out GameRoom room);

	protected int BombCount;

	public static readonly GameRoomType[] GameRoomTypes =
	{
		//Supported Mod gameplay room types
		Factory.FactoryType,
		PortalRoom.PortalRoomType,

		//Supported vanilla gameplay room types.  (Also catches all unknown Mod gameplay rooms, as ModGameplayRoom inherits FacilityRoom)
		Facility.RoomType,
		ElevatorGameRoom.RoomType,

		//Catch all for unknown room types in the vanilla game.
		DefaultGameRoom.RoomType
	};

	public static readonly CreateRoom[] CreateRooms =
	{
		Factory.TrySetupFactory,
		PortalRoom.TryCreatePortalRoom,
		Facility.TryCreateFacility,
		ElevatorGameRoom.TryCreateElevatorRoom,
		DefaultGameRoom.TryCreateRoom
	};

	public bool HoldBomb = true;

	public int BombID { get; protected set; }

	public virtual void RefreshBombID(ref int bombID) => BombID = bombID;

	public virtual bool IsCurrentBomb(int bombIndex) => true;

	public virtual void InitializeBombs(List<Bomb> bombs)
	{
		int currentBomb = bombs.Count == 1 ? -1 : 0;
		for (int i = 0; i < bombs.Count; i++)
		{
			TwitchGame.Instance.SetBomb(bombs[i], currentBomb == -1 ? -1 : i);
		}
		BombCount = currentBomb == -1 ? -1 : bombs.Count;
		TwitchGame.Instance.InitializeModuleCodes();
	}

	protected void InitializeBomb(Bomb bomb, bool reuseTwitchBomb=false)
	{
		if (!reuseTwitchBomb)
		{
			TwitchGame.Instance.SetBomb(bomb, BombCount++);
			return;
		}

		BombCount++;
		TwitchBomb tb = TwitchGame.Instance.Bombs[0];
		tb.Bomb = bomb;
		tb.BombTimeStamp = DateTime.Now;
		tb.BombStartingTimer = bomb.GetTimer().TimeRemaining;
		TwitchGame.Instance.CreateComponentHandlesForBomb(tb);
		TwitchGame.Instance.InitializeModuleCodes();
	}

	public virtual void InitializeBombNames()
	{
		List<TwitchBomb> bombHandles = TwitchGame.Instance.Bombs;

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
						bombHandles[i].BombName = nameText;
				}
				break;
			case 2:
				bombHandles[1].BombName = "The Other Bomb";
				break;
			default:
				foreach (TwitchBomb handle in bombHandles)
					handle.BombName = singleNames[rand.Next(0, singleNames.Length)];
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
	public static int SecondaryCameraCullingMask;
	public static void InitializeSecondaryCamera()
	{
		if (SecondaryCamera != null) return;

		GameObject customMover = new GameObject("CustomCameraMover");
		if (Camera.main == null)
		{
			DebugHelper.Log("Could not Initialize the secondary camera, because the main camera is not available");
			return;
		}

		customMover.transform.SetParent(Camera.main.transform.parent.parent);
		_mainCamera = Camera.main;

		// This enables the layer used by KTANE’s “MouseCam”
		_mainCamera.cullingMask |= 0x00002000;

		// This enables the layers used by the camera slots for modules (9–12 and 17–30)
		_mainCamera.cullingMask |= 0x7FFE1E00;

		SecondaryCamera = Object.Instantiate(_mainCamera, Vector3.zero, Quaternion.identity, customMover.transform);

		for (int i = 0; i < SecondaryCamera.transform.childCount; i++)
			Object.DestroyImmediate(SecondaryCamera.transform.GetChild(i).gameObject);
		SecondaryCamera.transform.localEulerAngles = new Vector3(26.39f, 0, 0);
		SecondaryCamera.gameObject.SetActive(false);

		MainCameraCullingMask = _mainCamera.cullingMask;
		SecondaryCameraCullingMask = SecondaryCamera.cullingMask;
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
		if (SecondaryCamera != null)
			SecondaryCamera.cullingMask = 0;
	}

	public static void ShowCamera()
	{
		if (_mainCamera != null)
			_mainCamera.cullingMask = MainCameraCullingMask;
		if (SecondaryCamera != null)
			SecondaryCamera.cullingMask = SecondaryCameraCullingMask;
	}

	public static void ResetCamera()
	{
		SecondaryCamera.transform.localPosition = Vector3.zero;
		SecondaryCamera.transform.localEulerAngles = new Vector3(26.39f, 0, 0);
	}

	// ReSharper disable once UnusedMember.Global
	public static void SetCameraPosition(Vector3 movement)
	{
		if (IsMainCamera) return;
		SecondaryCamera.transform.localPosition = movement;
	}

	// ReSharper disable once UnusedMember.Global
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
	public static void InitializeGameModes(bool lightsOn)
	{
		if (!lightsOn) return;
		if (OtherModes.TimeModeOn)
			OtherModes.SetMultiplier(TwitchPlaySettings.data.TimeModeStartingMultiplier);

		List<TwitchBomb> bombHandles = TwitchGame.Instance.Bombs;
		foreach (TwitchBomb bomb in bombHandles)
			if (OtherModes.TimeModeOn)
				bomb.CurrentTimer = TwitchPlaySettings.data.TimeModeStartingTime * 60;
			else if (OtherModes.ZenModeOn)
				bomb.CurrentTimer = 1;
	}

	public virtual IEnumerator ReportBombStatus()
	{
		yield break;
	}

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

	public virtual IEnumerator BombCommanderBombEdgework(Bomb bomb, string edge)
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

	public virtual bool BombCommanderRotateByLocalQuaternion(Bomb bomb, Quaternion localQuaternion) => true;
}
