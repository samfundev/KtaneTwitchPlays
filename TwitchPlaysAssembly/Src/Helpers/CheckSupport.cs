using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class CheckSupport
{
	static Text alertText;
	static Transform alertProgressBar;
	static readonly List<GameObject> gameObjects = new List<GameObject>();

	public static IEnumerator FindSupportedModules()
	{
		yield return null;

		UnityWebRequest request = UnityWebRequest.Get("https://ktane.timwi.de/json/raw");

		yield return request.SendWebRequest();

		if (!request.isNetworkError && !request.isHttpError)
		{
			var alert = Object.Instantiate(IRCConnection.Instance.ConnectionAlert, IRCConnection.Instance.ConnectionAlert.transform.parent);
			gameObjects.Add(alert);
			alert.SetActive(true);
			alertText = alert.transform.Find("Text").GetComponent<Text>();
			alertProgressBar = alert.transform.Find("ProgressBar");

			var json = JsonConvert.DeserializeObject<WebsiteJSON>(request.downloadHandler.text);
			yield return TestComponents(GetUntestedComponents(json), GetNameMap(json));

			Object.Destroy(alert);
		}
	}

	public static void Cleanup()
	{
		foreach (GameObject gameObject in gameObjects)
			Object.Destroy(gameObject);

		gameObjects.Clear();
	}

	static IEnumerator TestComponents(IEnumerable<BombComponent> untestedComponents, Dictionary<string, string> nameMap)
	{
		GameObject fakeModule = new GameObject();
		gameObjects.Add(fakeModule);
		TwitchModule module = fakeModule.AddComponent<TwitchModule>();
		module.enabled = false;

		HashSet<string> unsupportedModules = new HashSet<string>();
		Dictionary<string, bool> supportStatus = new Dictionary<string, bool>();
		ComponentSolverFactory.SilentMode = true;

		// Try to create a ComponentSolver for each module so we can see what modules are supported.
		foreach (BombComponent bombComponent in untestedComponents)
		{
			ComponentSolver solver = null;
			try
			{
				module.BombComponent = bombComponent.GetComponent<BombComponent>();

				solver = ComponentSolverFactory.CreateSolver(module);

				module.StopAllCoroutines(); // Stop any coroutines to prevent any exceptions or from affecting the next module.
			}
			catch (Exception e)
			{
				DebugHelper.LogException(e, $"Couldn't create a component solver for \"{bombComponent.GetModuleDisplayName()}\" during startup for the following reason:");
			}

			ModuleData.DataHasChanged |= solver != null;

			DebugHelper.Log(solver != null
				? $"Found a solver of type \"{solver.GetType().FullName}\" for component \"{bombComponent.GetModuleDisplayName()}\". This module is {(solver.UnsupportedModule ? "not supported" : "supported")} by Twitch Plays."
				: $"No solver found for component \"{bombComponent.GetModuleDisplayName()}\". This module is not supported by Twitch Plays.");

			string moduleID = bombComponent.GetComponent<KMBombModule>()?.ModuleType ?? bombComponent.GetComponent<KMNeedyModule>()?.ModuleType;
			if (solver?.UnsupportedModule != false && moduleID != null)
			{
				unsupportedModules.Add(moduleID);
			}

			supportStatus[bombComponent.GetModuleDisplayName()] = !(solver?.UnsupportedModule != false && moduleID != null);

			yield return null;
		}

		ComponentSolverFactory.SilentMode = false;
		ModuleData.WriteDataToFile();
		Object.Destroy(fakeModule);

		// Always disable the modules from the spreadsheet
		var disabledSheet = new GoogleSheet("https://spreadsheets.google.com/feeds/list/1G6hZW0RibjW7n72AkXZgDTHZ-LKj0usRkbAwxSPhcqA/3/public/values?alt=json", "modulename");
		yield return disabledSheet;

		if (disabledSheet.Success && TwitchPlaySettings.data.AllowSheetDisabledModules)
		{
			foreach (var row in disabledSheet.GetRows())
			{
				if (!nameMap.TryGetValue(row["modulename"], out string moduleID))
				{
					DebugHelper.Log($"Couldn't map \"{row["modulename"]}\" to a module ID when disabling modules from the spreadsheet.");
					continue;
				}

				unsupportedModules.Add(moduleID);
			}
		}

		// Using the list of unsupported module IDs stored in unsupportedModules, make a Mod Selector profile.
		string profilesPath = Path.Combine(Application.persistentDataPath, "ModProfiles");
		if (Directory.Exists(profilesPath))
		{
			Dictionary<string, object> profileData = new Dictionary<string, object>()
			{
				{ "DisabledList", unsupportedModules },
				{ "Operation", 1 }
			};

			File.WriteAllText(Path.Combine(profilesPath, "TP_Supported.json"), SettingsConverter.Serialize(profileData));
		}

		alertProgressBar.localScale = Vector3.one;

		// Send a message to chat if any modules aren't marked as having support
		if (supportStatus.Values.Count(status => status) > 0)
		{
			var supportedList = supportStatus.Where(pair => pair.Value).Select(pair => pair.Key).Join(", ");
			IRCConnection.SendMessage($"Let the Scoring Team know that the following modules have TP support: {supportedList}");
			alertText.text = $"These modules have TP support: {supportedList}";
			yield return new WaitForSeconds(4);
		}
		else
		{
			alertText.text = "Support checks passed succesfully!";
			yield return new WaitForSeconds(2);
		}

		// Log out the full results of the testing
		DebugHelper.Log($"Support testing results:\n{supportStatus.OrderByDescending(pair => pair.Value).Select(pair => $"{pair.Key} - {(pair.Value ? "" : "Not ")}Supported").Join("\n")}");
	}

	static IEnumerable<BombComponent> GetUntestedComponents(WebsiteJSON json)
	{
		int progress = 0;

		// Get local mods that need to be tested
		if (typeof(ModManager).GetField("loadedMods", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(ModManager.Instance) is not Dictionary<string, Mod> loadedMods)
			yield break;

		var validLocalMods = loadedMods.Values
			.SelectMany(mod => mod.GetModObjects<BombComponent>())
			.Where(bombComponent =>
			{
				string moduleID = bombComponent.GetComponent<KMBombModule>()?.ModuleType ?? bombComponent.GetComponent<KMNeedyModule>()?.ModuleType;
				return json.KtaneModules.Any(module => module.ModuleID == moduleID && module.TwitchPlays == null) || !json.KtaneModules.Any(module => module.ModuleID == moduleID);
			})
			.ToArray();

		// Get mods that need to be loaded into the game to be tested
		var disabledParent = new GameObject();
		gameObjects.Add(disabledParent);
		disabledParent.SetActive(false);

		var modWorkshopPath = Path.GetFullPath(new[] { SteamDirectory, "steamapps", "workshop", "content", "341800" }.Aggregate(Path.Combine));
		var validModules = json.KtaneModules.Where(module =>
		{
			if (module.TwitchPlays != null || module.SteamID == null)
				return false;

			var modPath = Path.Combine(modWorkshopPath, module.SteamID);
			return Directory.Exists(modPath);
		}).ToArray();

		// Test local mods
		int total = validLocalMods.Length + validModules.Length;

		foreach (var component in validLocalMods)
		{
			alertText.text = $"Testing compatibility of:\n\"{component.GetModuleDisplayName()}\"";
			alertProgressBar.localScale = new Vector3((float) progress / total, 1, 1);

			yield return component;

			progress++;
		}

		// Test loaded mods
		foreach (var module in validModules)
		{
			DebugHelper.Log($"Loading module \"{module.Name}\" to test compatibility...");
			alertText.text = $"Testing compatibility of:\n\"{module.Name}\"";
			alertProgressBar.localScale = new Vector3((float) progress / total, 1, 1);

			var modPath = Path.Combine(modWorkshopPath, module.SteamID);
			Mod mod = Mod.LoadMod(modPath, Assets.Scripts.Mods.ModInfo.ModSourceEnum.Local);
			Object[] loadedObjects = new Object[] { };
			foreach (string fileName in mod.GetAssetBundlePaths())
			{
				AssetBundle mainBundle = AssetBundle.LoadFromFile(fileName);
				if (mainBundle != null)
				{
					try
					{
						mod.LoadBundle(mainBundle);
					}
					catch (Exception ex)
					{
						DebugHelper.LogException(ex, $"Load of mod \"{mod.ModID}\" failed:");
					}

					loadedObjects = mainBundle.LoadAllAssets<Object>();

					mainBundle.Unload(false);
				}
			}

			mod.CallMethod("RemoveMissions");
			mod.CallMethod("RemoveSoundOverrides");

			if (mod != null)
			{
				string ModuleID = module.ModuleID;
				GameObject realModule = null;
				foreach (KMBombModule kmbombModule in mod.GetModObjects<KMBombModule>())
				{
					string moduleType = kmbombModule.ModuleType;
					if (moduleType == ModuleID)
					{
						realModule = Object.Instantiate(kmbombModule.gameObject, disabledParent.transform);
						realModule.GetComponent<ModBombComponent>().OnLoadFromBundle();
					}
				}
				foreach (KMNeedyModule kmneedyModule in mod.GetModObjects<KMNeedyModule>())
				{
					string moduleType2 = kmneedyModule.ModuleType;
					if (moduleType2 == ModuleID)
					{
						realModule = Object.Instantiate(kmneedyModule.gameObject, disabledParent.transform);
						realModule.GetComponent<ModNeedyComponent>().OnLoadFromBundle();
					}
				}

				if (realModule != null)
				{
					yield return realModule.GetComponent<BombComponent>();
				}

				mod.RemoveServiceObjects();
				mod.Unload();

				foreach (var loadedObject in loadedObjects)
				{
					// GameObjects can't be unloaded, only destroyed.
					if (loadedObject as GameObject)
					{
						Object.Destroy(loadedObject);
						continue;
					}

					Resources.UnloadAsset(loadedObject);
				}
			}

			progress++;
		}

		Object.Destroy(disabledParent);
	}

#pragma warning disable CS0649
	class WebsiteJSON
	{
		public List<KtaneModule> KtaneModules;
	}

	class KtaneModule
	{
		public string SteamID;
		public string Name;
		public string ModuleID;
		public Dictionary<string, object> TwitchPlays;
	}
#pragma warning restore CS0649

	static string SteamDirectory
	{
		get
		{
			// Mod folders
			var folders = Assets.Scripts.Services.AbstractServices.Instance.GetModFolders();
			if (folders.Count != 0)
			{
				return folders[0] + "/../../../../..";
			}

			// Relative to the game
			var relativePath = Path.GetFullPath("./../../..");
			if (new DirectoryInfo(relativePath).Name == "Steam")
			{
				return relativePath;
			}

			// Registry key
			using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
			{
				if (key?.GetValueNames().Contains("SteamPath") == true)
				{
					return key.GetValue("SteamPath")?.ToString().Replace('/', '\\') ?? string.Empty;
				}
			}

			// Guess common paths
			foreach (var path in new[]
			{
				@"Program Files (x86)\Steam",
				@"Program Files\Steam",
			})
			{
				foreach (var drive in Directory.GetLogicalDrives())
				{
					if (Directory.Exists(drive + path))
					{
						return drive + path;
					}
				}
			}

			foreach (var path in new[]
			{
				"~/Library/Application Support/Steam",
				"~/.steam/steam",
			})
			{
				if (Directory.Exists(path))
				{
					return path;
				}
			}

			return null;
		}
	}

	static Dictionary<string, string> GetNameMap(WebsiteJSON json)
	{
		if (typeof(ModManager).GetField("loadedMods", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(ModManager.Instance) is not Dictionary<string, Mod> loadedMods)
			return null;

		var bombComponents = loadedMods.Values.SelectMany(mod => mod.GetModObjects<BombComponent>());

		var nameMap = new Dictionary<string, string>();
		foreach (var module in json.KtaneModules)
			nameMap[module.Name] = module.ModuleID;

		foreach (var bombComponent in bombComponents)
			nameMap[bombComponent.GetModuleDisplayName()] = bombComponent.GetModuleID();

		return nameMap;
	}
}