using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Pacing;
using Object = UnityEngine.Object;

public sealed class Facility : GameRoom
{
	public static Type RoomType() => typeof(FacilityRoom);

	public static bool TryCreateFacility(Object[] roomObjects, out GameRoom room)
	{
		if (roomObjects == null || roomObjects.Length == 0)
		{
			room = null;
			return false;
		}
		room = new Facility((FacilityRoom) roomObjects[0]);
		return true;
	}

	private Facility(FacilityRoom facilityRoom)
	{
		DebugHelper.Log("Found gameplay room of type Facility Room");
		_facilityRoom = facilityRoom;
	}

	public override IEnumerator ReportBombStatus()
	{
		IEnumerator baseIEnumerator = base.ReportBombStatus();
		while (baseIEnumerator.MoveNext()) yield return baseIEnumerator.Current;

		List<TwitchBomb> bombHandles = TwitchGame.Instance.Bombs;

		if (!SceneManager.Instance.GameplayState.Mission.PacingEventsEnabled)
			yield break;

		_facilityRoom.PacingActions.RemoveAll(action => action.EventType == PaceEvent.OneMinuteLeft);
		while (bombHandles.TrueForAll(bomb => !bomb.Bomb.HasDetonated))
		{
			if (bombHandles.TrueForAll(bomb => bomb.Bomb.IsSolved()))
				yield break;
			ToggleEmergencyLights(bombHandles.Any(bomb => bomb.CurrentTimer < 60f && !bomb.IsSolved && !OtherModes.Unexplodable));
			yield return null;
		}
	}

	public override IEnumerator InterruptLights()
	{
		//Already done
		yield break;
	}

	public bool EmergencyLightsState;
	public void ToggleEmergencyLights(bool on)
	{
		if (EmergencyLightsState == on) return;
		EmergencyLightsState = on;
		MethodInfo method = on ? TurnOnEmergencyLightsMethod : TurnOffEmergencyLightsMethod;
		method.Invoke(_facilityRoom, null);
	}

	static Facility()
	{
		TurnOffEmergencyLightsMethod = typeof(FacilityRoom).GetMethod("TurnOffEmergencyLights", BindingFlags.NonPublic | BindingFlags.Instance);
		TurnOnEmergencyLightsMethod = typeof(FacilityRoom).GetMethod("TurnOnEmergencyLights", BindingFlags.NonPublic | BindingFlags.Instance);
	}

	private static readonly MethodInfo TurnOffEmergencyLightsMethod;
	private static readonly MethodInfo TurnOnEmergencyLightsMethod;

	private readonly FacilityRoom _facilityRoom;
}
