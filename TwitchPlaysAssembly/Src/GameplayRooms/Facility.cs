

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Pacing;
using UnityEngine;

public class Facility : GameRoom
{
    public static Type RoomType()
    {
        return typeof(FacilityRoom);
    }

    public static bool TryCreateFacility(UnityEngine.Object[] roomObjects, out GameRoom room)
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

    public override IEnumerator ReportBombStatus(List<TwitchBombHandle> bombHandles)
    {
        _facilityRoom.PacingActions.RemoveAll(action => action.EventType == PaceEvent.OneMinuteLeft);
        while (bombHandles.TrueForAll(handle => !handle.bombCommander.Bomb.HasDetonated))
        {
            if (bombHandles.TrueForAll(handle => handle.bombCommander.Bomb.IsSolved()))
                yield break;
            ToggleEmergencyLights(bombHandles.Any(handle => handle.bombCommander.CurrentTimer < 60f && !handle.bombCommander.Bomb.IsSolved()));
            yield return null;
        }
    }

    public bool EmergencyLightsState = false;
    public void ToggleEmergencyLights(bool on)
    {
        if (EmergencyLightsState == on) return;
        EmergencyLightsState = on;
        MethodInfo method = on ? _turnOnEmergencyLightsMethod : _turnOffEmergencyLightsMethod;
        method.Invoke(_facilityRoom, null);
    }

    

    static Facility()
    {
        _turnOffEmergencyLightsMethod = typeof(FacilityRoom).GetMethod("TurnOffEmergencyLights", BindingFlags.NonPublic | BindingFlags.Instance);
        _turnOnEmergencyLightsMethod = typeof(FacilityRoom).GetMethod("TurnOnEmergencyLights", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private static MethodInfo _turnOffEmergencyLightsMethod = null;
    private static MethodInfo _turnOnEmergencyLightsMethod = null;

    private FacilityRoom _facilityRoom = null;
}
