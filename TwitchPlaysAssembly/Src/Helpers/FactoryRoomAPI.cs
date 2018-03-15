using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TwitchPlaysAssembly.Helpers
{
	public class FactoryRoomAPI
	{
		private static GameObject _gameObject;

		private static IDictionary<string, object> Properties
		{
			get
			{
				return _gameObject == null
					? null
					: _gameObject.GetComponent<IDictionary<string, object>>();
			}
		}

		//Call this in KMGameState.Setup
		public static IEnumerator Refresh()
		{
			for (var i = 0; i < 4 && _gameObject == null; i++)
			{
				_gameObject = GameObject.Find("Factory_Info");
				yield return null;
			}
		}

		public static bool Installed()
		{
			return _gameObject != null;
		}

		public static List<string> GetFactoryModes()
		{
			return Properties == null || !Properties.ContainsKey("SupportedModes") || Properties["SupportedModes"] == null
				? null
				: ((string[]) Properties["SupportedModes"]).ToList();
		}
	}
}