using UnityEngine;
using System.Reflection;
using System;

public static class MissionID
{
	public static string GetMissionID()
	{
		var gameplayState = GameObject.Find("GameplayState(Clone)").GetComponent<GameplayState>();
		var type = gameplayState.GetType();
		var fieldMission = type.GetField("MissionToLoad", BindingFlags.Public | BindingFlags.Static);
		return fieldMission.GetValue(gameplayState).ToString();
	}
}