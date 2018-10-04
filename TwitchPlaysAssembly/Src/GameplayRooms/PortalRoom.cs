using Assets.Scripts.Pacing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class PortalRoom : GameRoom
{
	public static Type PortalRoomType()
	{
		if (_portalRoomType != null) return _portalRoomType;
		_portalRoomType = ReflectionHelper.FindType("PortalRoom", "HexiBombRoom");

		if (_portalRoomType == null)
			return null;

		_redLightsMethod = _portalRoomType.GetMethod("RedLight", BindingFlags.Public | BindingFlags.Instance);
		_roomLightField = _portalRoomType.GetField("RoomLight", BindingFlags.Public | BindingFlags.Instance);

		return _portalRoomType;
	}

	public static bool TryCreatePortalRoom(UnityEngine.Object[] roomObjects, out GameRoom room)
	{
		if (roomObjects == null || roomObjects.Length == 0 || PortalRoomType() == null)
		{
			room = null;
			return false;
		}

		room = new PortalRoom((MonoBehaviour) roomObjects[0]);
		return true;
	}

	private PortalRoom(UnityEngine.Object room)
	{
		DebugHelper.Log("Portal Room created");
		_room = room;
	}

	public override IEnumerator ReportBombStatus()
	{
		IEnumerator baseIEnumerator = base.ReportBombStatus();
		while (baseIEnumerator.MoveNext()) yield return baseIEnumerator.Current;

		List<TwitchBombHandle> bombHandles = BombMessageResponder.Instance.BombHandles;
		yield return new WaitUntil(() => SceneManager.Instance.GameplayState.RoundStarted);
		yield return new WaitForSeconds(0.1f);
		_roomLight = (GameObject) _roomLightField.GetValue(_room);

		PaceMaker paceMaker = SceneManager.Instance.GameplayState.GetPaceMaker();
		List<PacingAction> actions = (List<PacingAction>) typeof(PaceMaker).GetField("actions", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(paceMaker);
		actions?.RemoveAll(action => action.EventType == PaceEvent.OneMinuteLeft);

		while (bombHandles.TrueForAll(handle => !handle.bombCommander.Bomb.HasDetonated))
		{
			if (bombHandles.TrueForAll(handle => handle.bombCommander.Bomb.IsSolved()))
				yield break;
			ToggleEmergencyLights(bombHandles.Any(handle => handle.bombCommander.CurrentTimer < 60f && !handle.bombCommander.Bomb.IsSolved()), bombHandles[0]);
			yield return null;
		}
	}

	private bool _emergencyLightsState = false;
	private IEnumerator _emergencyLightsRoutine = null;
	private void ToggleEmergencyLights(bool on, TwitchBombHandle handle)
	{
		if (_emergencyLightsState == on) return;
		_emergencyLightsState = on;
		if (!on)
		{
			handle.StopCoroutine(_emergencyLightsRoutine);
			_emergencyLightsRoutine = null;
			_roomLight.GetComponent<Light>().color = new Color(0.5f, 0.5f, 0.5f);
		}
		else
		{
			_emergencyLightsRoutine = (IEnumerator) _redLightsMethod.Invoke(_room, null);
			handle.StartCoroutine(_emergencyLightsRoutine);
		}
	}

	private static Type _portalRoomType = null;
	private static MethodInfo _redLightsMethod = null;
	private static FieldInfo _roomLightField = null;
	private readonly UnityEngine.Object _room = null;
	private GameObject _roomLight = null;
}
