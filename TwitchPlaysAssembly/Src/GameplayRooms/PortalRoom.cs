using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Pacing;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class PortalRoom : GameRoom
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

	public static bool TryCreatePortalRoom(Object[] roomObjects, out GameRoom room)
	{
		if (roomObjects == null || roomObjects.Length == 0 || PortalRoomType() == null)
		{
			room = null;
			return false;
		}

		room = new PortalRoom((MonoBehaviour) roomObjects[0]);
		return true;
	}

	private PortalRoom(Object room)
	{
		_room = room;
	}

	public override IEnumerator ReportBombStatus()
	{
		IEnumerator baseIEnumerator = base.ReportBombStatus();
		while (baseIEnumerator.MoveNext()) yield return baseIEnumerator.Current;

		List<TwitchBomb> bombHandles = TwitchGame.Instance.Bombs;
		yield return new WaitUntil(() => SceneManager.Instance.GameplayState.RoundStarted);
		yield return new WaitForSeconds(0.1f);
		_roomLight = (GameObject) _roomLightField.GetValue(_room);

		PaceMaker paceMaker = SceneManager.Instance.GameplayState.GetPaceMaker();
		List<PacingAction> actions = (List<PacingAction>) typeof(PaceMaker).GetField("actions", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(paceMaker);
		actions?.RemoveAll(action => action.EventType == PaceEvent.OneMinuteLeft);

		while (bombHandles.TrueForAll(handle => !handle.Bomb.HasDetonated))
		{
			if (bombHandles.TrueForAll(handle => handle.Bomb.IsSolved()))
				yield break;
			ToggleEmergencyLights(!OtherModes.Unexplodable && bombHandles.Any(handle => handle.CurrentTimer < 60f && !handle.Bomb.IsSolved()), bombHandles[0]);
			yield return null;
		}
	}

	public override IEnumerator InterruptLights()
	{
		//already done
		yield break;
	}

	private bool _emergencyLightsState;
	private IEnumerator _emergencyLightsRoutine;
	private void ToggleEmergencyLights(bool on, TwitchBomb handle)
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

	private static Type _portalRoomType;
	private static MethodInfo _redLightsMethod;
	private static FieldInfo _roomLightField;
	private readonly Object _room;
	private GameObject _roomLight;
}
