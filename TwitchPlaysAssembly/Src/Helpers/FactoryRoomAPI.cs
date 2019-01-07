using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class FactoryRoomAPI
{
	private static GameObject _gameObject;

	private static IDictionary<string, object> Properties => _gameObject?.GetComponent<IDictionary<string, object>>();

	//Call this in KMGameState.Setup
	public static IEnumerator Refresh()
	{
		for (int i = 0; i < 4 && _gameObject == null; i++)
		{
			_gameObject = GameObject.Find("Factory_Info");
			yield return null;
		}
	}

	public static bool Installed() => _gameObject != null;

	public static List<string> GetFactoryModes() => Properties == null || !Properties.ContainsKey("SupportedModes") || Properties["SupportedModes"] == null
			? null
			: ((string[]) Properties["SupportedModes"]).ToList();
}
