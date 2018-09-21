using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
		ToggleCamera(false);
		ResetCamera();
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
		if (!(rand.NextDouble() < specialNameProbability)) return;
		foreach (TwitchBombHandle handle in bombHandles)
		{
			handle.bombName = singleNames[rand.Next(0, singleNames.Length - 1)];
		}
	}

	public override IEnumerator ReportBombStatus()
	{
		IEnumerator baseIEnumerator = base.ReportBombStatus();
		while (baseIEnumerator.MoveNext()) yield return baseIEnumerator.Current;
		ValidEdgeworkRegex = new[] { $"^edgework((?: right| left| back| r| l| b)?)$" };
		TwitchBombHandle bombHandle = BombMessageResponder.Instance.BombHandles[0];
		TimerComponent timerComponent = bombHandle.bombCommander.timerComponent;
		yield return new WaitUntil(() => timerComponent.IsActive);
		BombMessageResponder.Instance.OnLightsChange(true);

		_elevatorRoom.PacingActions.RemoveAll(action => action.EventType == PaceEvent.OneMinuteLeft);
		while (!bombHandle.bombCommander.Bomb.HasDetonated)
		{
			if (Input.GetKey(KeyCode.Escape))
			{
				IEnumerator bombDrop = bombHandle.OnMessageReceived(new Message(bombHandle.bombName, "red", "bomb drop"));
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

	#region Bomb Commander Overrides
	public override IEnumerator BombCommanderHoldBomb(Bomb bomb, bool frontFace = true)
	{
		yield return false;
		if (_currentWall != CurrentElevatorWall.Dropped) yield break;

		IEnumerator dropAllHoldables = MiscellaneousMessageResponder.DropAllHoldables();
		while (dropAllHoldables.MoveNext())
			yield return dropAllHoldables.Current;

		IEnumerator holdBomb = DoElevatorCameraRotate(CurrentElevatorWall.Dropped, CurrentElevatorWall.Back, 1, false, false);
		while (holdBomb.MoveNext())
			yield return holdBomb.Current;
		_currentWall = CurrentElevatorWall.Back;
	}

	public override IEnumerator BombCommanderDropBomb(Bomb bomb)
	{
		yield return false;
		if (_currentWall == CurrentElevatorWall.Dropped) yield break;
		IEnumerator bombDrop = DoElevatorCameraRotate(_currentWall, CurrentElevatorWall.Dropped, 1, false, false);
		while (bombDrop.MoveNext())
			yield return bombDrop.Current;
		_currentWall = CurrentElevatorWall.Dropped;
	}

	public override IEnumerator BombCommanderTurnBomb(Bomb bomb)
	{
		yield return false;
		IEnumerator dropAllHoldables = MiscellaneousMessageResponder.DropAllHoldables();
		while (dropAllHoldables.MoveNext())
		{
			yield return dropAllHoldables.Current;
		}
		IEnumerator rotateCamera;
		switch (_currentWall)
		{
			case CurrentElevatorWall.Right:
				rotateCamera = DoElevatorCameraRotate(_currentWall, CurrentElevatorWall.Back, 1, false, false);
				_currentWall = CurrentElevatorWall.Back;
				break;
			case CurrentElevatorWall.Dropped:
			case CurrentElevatorWall.Back:
				rotateCamera = DoElevatorCameraRotate(_currentWall, CurrentElevatorWall.Left, 1, false, false);
				_currentWall = CurrentElevatorWall.Left;
				break;
			case CurrentElevatorWall.Left:
				rotateCamera = DoElevatorCameraRotate(_currentWall, CurrentElevatorWall.Right, 1, false, false);
				_currentWall = CurrentElevatorWall.Right;
				break;
			default: yield break;
		}
		while (rotateCamera.MoveNext())
		{
			yield return rotateCamera.Current;
		}
	}

	public override IEnumerator BombCommanderBombEdgework(Bomb bomb, Match edgeworkMatch)
	{
		yield return false;
		if (edgeworkMatch == null || !edgeworkMatch.Success) yield break;

		for(int i = 0; i < edgeworkMatch.Groups.Count; i++)
			DebugHelper.Log($"edgeworkMatch.Groups[{i}].Value = {edgeworkMatch.Groups[i].Value}");

		string edge = edgeworkMatch.Groups[1].Value.ToLowerInvariant().Trim();
		if (string.IsNullOrEmpty(edge))
			edge = "all edges";

		IEnumerator showEdgework = MiscellaneousMessageResponder.DropAllHoldables();
		while (showEdgework.MoveNext())
			yield return showEdgework.Current;

		CurrentElevatorWall currentWall = _currentWall == CurrentElevatorWall.Dropped ? CurrentElevatorWall.Back : _currentWall;
		if (edge.EqualsAny("all edges", "l", "left"))
		{
			showEdgework = DoElevatorCameraRotate(_currentWall, CurrentElevatorWall.Left, 1, false, true);
			_currentWall = CurrentElevatorWall.Left;
			while (showEdgework.MoveNext())
				yield return showEdgework.Current;
			yield return new WaitForSeconds(3);
		}
		if (edge.EqualsAny("all edges", "b", "back"))
		{
			showEdgework = DoElevatorCameraRotate(_currentWall, CurrentElevatorWall.Back, 1, edge == "all edges", true);
			_currentWall = CurrentElevatorWall.Back;
			while (showEdgework.MoveNext())
				yield return showEdgework.Current;
			yield return new WaitForSeconds(3);
		}
		if (edge.EqualsAny("all edges", "r", "right"))
		{
			showEdgework = DoElevatorCameraRotate(_currentWall, CurrentElevatorWall.Right, 1, edge == "all edges", true);
			_currentWall = CurrentElevatorWall.Right;
			while (showEdgework.MoveNext())
				yield return showEdgework.Current;
			yield return new WaitForSeconds(3);
		}
		showEdgework = DoElevatorCameraRotate(_currentWall, currentWall, 1, true, false);
		_currentWall = currentWall;
		while (showEdgework.MoveNext())
			yield return showEdgework.Current;
	}

	public override IEnumerator BombCommanderFocus(Bomb bomb, Selectable selectable, float focusDistance, bool frontFace)
	{
		yield return false;
		IEnumerator turnBomb = null;
		int rotation = (int)Math.Round(selectable.transform.localEulerAngles.y, 0);
		DebugHelper.Log($"selectable.name = {selectable.transform.name}");
		DebugHelper.Log($"selectable position = {Math.Round(selectable.transform.localPosition.x, 3)},{Math.Round(selectable.transform.localPosition.y, 3)},{Math.Round(selectable.transform.localPosition.z, 3)}");
		DebugHelper.Log($"selectable rotation = {Math.Round(selectable.transform.localEulerAngles.y, 3)}");

		switch (rotation)
		{
			case 90 when _currentWall != CurrentElevatorWall.Left:
				turnBomb = DoElevatorCameraRotate(_currentWall, CurrentElevatorWall.Left, 1, false, false);
				_currentWall = CurrentElevatorWall.Left;
				break;
			case 180 when _currentWall != CurrentElevatorWall.Back:
				turnBomb = DoElevatorCameraRotate(_currentWall, CurrentElevatorWall.Back, 1, false, false);
				_currentWall = CurrentElevatorWall.Back;
				break;
			case 270 when _currentWall != CurrentElevatorWall.Right:
				turnBomb = DoElevatorCameraRotate(_currentWall, CurrentElevatorWall.Right, 1, false, false);
				_currentWall = CurrentElevatorWall.Right;
				break;
		}
		while (turnBomb != null && turnBomb.MoveNext())
			yield return turnBomb.Current;

		selectable.HandleSelect(false);
		selectable.HandleInteract();
	}

	public override IEnumerator BombCommanderDefocus(Bomb bomb, Selectable selectable, bool frontFace)
	{
		yield return false;
		selectable.HandleCancel();
		selectable.HandleDeselect();
	}

	public override bool BombCommanderRotateByLocalQuaternion(Bomb bomb, Quaternion localQuaternion)
	{
		return false;
	}

	private IEnumerator DoElevatorCameraRotate(CurrentElevatorWall currentWall, CurrentElevatorWall newWall, float duration, bool fromEdgework, bool toEdgework)
	{
		if (currentWall == CurrentElevatorWall.Dropped && newWall != CurrentElevatorWall.Dropped)
			ToggleCamera(false);
		float initialTime = Time.time;
		Vector3 currentWallPosition = fromEdgework ? ElevatorEdgeworkCameraPositions[(int)currentWall] : ElevatorCameraPositions[(int)currentWall];
		Vector3 currentWallRotation = fromEdgework ? ElevatorEdgeworkCameraRotations[(int)currentWall] : ElevatorCameraRotations[(int)currentWall];
		Vector3 newWallPosition = toEdgework ? ElevatorEdgeworkCameraPositions[(int)newWall] : ElevatorCameraPositions[(int)newWall];
		Vector3 newWallRotation = toEdgework ? ElevatorEdgeworkCameraRotations[(int)newWall] : ElevatorCameraRotations[(int)newWall];
		Transform camera = SecondaryCamera.transform;
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
		if (newWall == CurrentElevatorWall.Dropped)
			ToggleCamera(true);
	}

	private enum CurrentElevatorWall
	{
		Left,
		Back,
		Right,
		Dropped
	}

	private readonly Vector3[] ElevatorCameraRotations =
	{
		new Vector3(0, -90, 0),
		Vector3.zero,
		new Vector3(0, 90, 0), 

		new Vector3(26.39f, 0, 0)
	};

	private readonly Vector3[] ElevatorEdgeworkCameraRotations =
	{
		new Vector3(20, -90, 0),
		new Vector3(20, 0, 0),
		new Vector3(20, 90, 0),
		new Vector3(26.39f, 0, 0)
	};

	private readonly Vector3[] ElevatorCameraPositions =
	{
		new Vector3(0.625f, 0.125f, 1.425f),
		new Vector3(-0.125f, 0.125f, 0.8f),
		new Vector3(-0.875f, 0.125f, 1.425f),
		Vector3.zero
	};

	private readonly Vector3[] ElevatorEdgeworkCameraPositions =
	{
		new Vector3(0.625f, 0.125f, 1.425f),
		new Vector3(-0.125f, 0.125f, 0.8f),
		new Vector3(-0.875f, 0.125f, 1.425f),
		Vector3.zero
	};

	private CurrentElevatorWall _currentWall = CurrentElevatorWall.Dropped;
	#endregion

	private ElevatorRoom _elevatorRoom;
}
