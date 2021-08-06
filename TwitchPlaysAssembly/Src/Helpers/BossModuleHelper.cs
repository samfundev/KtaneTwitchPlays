using System.Collections.Generic;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

public static class BossModuleHelper
{
	private static List<string> _bossmods = new List<string>();
	
	public static bool IsBossMod(this string mod) => _bossmods.Contains(mod);
	
	public static IEnumerator GetBossMods()
	{
		using (var http = UnityWebRequest.Get("https://ktane.timwi.de/json/raw"))
		{
			yield return http.SendWebRequest();

			if (http.isNetworkError || http.responseCode != 200)
			{
				DebugHelper.LogError("Failed to load boss modules. Network error.");
			}

			var mods = JObject.Parse(http.downloadHandler.text)["KtaneModules"] as JArray;

			if (mods == null)
			{
				DebugHelper.LogError("Failed to load boss modules. Mods is null.");
			}

			var bossMods = new List<string>();

			foreach (JObject mod in mods)
			{
				var ignoreList = mod["IgnoreProcessed"] as JArray ?? mod["Ignore"] as JArray;
				var name = mod["Name"] as JValue;
				var id = mod["ModuleID"] as JValue;

				if (ignoreList != null)
				{
					if(id.Value is string)
						bossMods.Add((name.Value.ToString()));
					else
						DebugHelper.Log($"Failed to load name for mod {name.Value}.");
				}
			}
			DebugHelper.Log("List of boss modules loaded.");
			_bossmods = bossMods;
		}
	}
}
