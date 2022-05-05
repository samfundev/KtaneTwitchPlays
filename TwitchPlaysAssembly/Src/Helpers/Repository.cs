using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public static class Repository
{
	public static string RawJSON;
	public static List<KtaneModule> Modules;

	public static IEnumerator LoadData()
	{
		if (RawJSON != null)
			yield break;

		var download = new DownloadText("https://ktane.timwi.de/json/raw");
		yield return download;

		RawJSON = download.Text;
		Modules = JsonConvert.DeserializeObject<WebsiteJSON>(RawJSON).KtaneModules;
	}

	public static bool IsBossMod(this string moduleID) => Modules.Any(module => module.ModuleID == moduleID && module.Ignore != null);

	public static string GetManual(string moduleID)
	{
		var match = Modules.FirstOrDefault(module => module.ModuleID == moduleID);
		return match?.FileName ?? match?.Name;
	}

#pragma warning disable CS0649
	public class WebsiteJSON
	{
		public List<KtaneModule> KtaneModules;
	}

	public class KtaneModule
	{
		public string SteamID;
		public string Name;
		public string ModuleID;
		public string Type;
		public string Compatibility;
		public string FileName;
		public Dictionary<string, object> TwitchPlays;

		public List<string> Ignore;
	}
#pragma warning restore CS0649
}