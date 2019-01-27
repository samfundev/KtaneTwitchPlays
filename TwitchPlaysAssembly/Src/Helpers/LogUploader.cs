using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class LogUploader : MonoBehaviour
{
	public static LogUploader Instance;

	public string Log { get; private set; }

	[HideInInspector]
	public string previousUrl = null;

	[HideInInspector]
	public bool postOnComplete = false;

	[HideInInspector]
	public string LOGPREFIX;

	readonly private string[] _blacklistedLogLines =
	{
		"[ServicesSteam]",
		"[BombGenerator] Instantiated EmptyComponent",
		"[BombGenerator] Filling remaining spaces with empty components.",
		"[BombGenerator] BombTypeEnum: Default",
		"[StatsManager]",
		"[FileUtilityHelper]",
		"[MenuPage]",
		"[PlayerSettingsManager]",
		"[LeaderboardBulkSubmissionWorker]",
		"[MissionManager]",
		"[AlarmClock]",
		"[AlarmClockExtender]",
		"[Alarm Clock Extender]",
		"[BombGenerator] Instantiated TimerComponent",
		"[BombGenerator] Instantiating RequiresTimerVisibility components on",
		"[BombGenerator] Instantiating remaining components on any valid face.",
		"[PrefabOverride]",
		"Tick delay:",
		"Calculated FPS: ",
		"[ModuleCameras]",
		"[TwitchPlays]",
		"(Filename:  Line: 21)"
	};

	private readonly OrderedDictionary domainNames = new OrderedDictionary
	{
		// In order of preference (favorite first)
		// The integer value is the data size limit in bytes
		{ "ktane.timwi.de", 25000000 },
		{ "hastebin.com", 400000 }
	};

	public void Awake() => Instance = this;

	public void OnEnable()
	{
		LOGPREFIX = "[" + GetType().Name + "] ";
		Application.logMessageReceived += HandleLog;
	}

	public void OnDisable() => Application.logMessageReceived -= HandleLog;

	public void Clear() => Log = "";

	public void GetAnalyzerUrl(Action<string> callback) => StartCoroutine(GetAnalyzerUrl(Log, callback));

	public IEnumerator GetAnalyzerUrl(string data, Action<string> callback)
	{
		// This first line is necessary as the Log Analyzer uses it as an identifier
		data = "Initialize engine version: Twitch Plays\n" + data;

		byte[] encodedData = Encoding.UTF8.GetBytes(data);
		int dataLength = encodedData.Length;

		bool tooLong = false;

		foreach (DictionaryEntry domain in domainNames)
		{
			string domainName = (string) domain.Key;
			int maxLength = (int) domain.Value;

			tooLong = false;
			if (dataLength >= maxLength)
			{
				Debug.LogFormat(LOGPREFIX + "Data ({0}B) is too long for {1} ({2}B)", dataLength, domainName, maxLength);
				tooLong = true;
				continue;
			}

			Debug.Log(LOGPREFIX + "Posting new log to " + domainName);

			if (domainName == "ktane.timwi.de")
			{
				UnityWebRequest www = UnityWebRequest.Post(TwitchPlaySettings.data.RepositoryUrl + "/upload-log", new List<IMultipartFormSection> {
					new MultipartFormFileSection("log", encodedData, null, "output_log.txt"),
					new MultipartFormDataSection("noredirect", "1", Encoding.UTF8, "text/plain"),
					new MultipartFormDataSection("extrapadding", "1") // Ugly Hack: Unity doesn't seem to send the request properly and leaves out the last boundry. This adds an extra field to counter that.
				});

				yield return www.Send();

				if (!www.isNetworkError && !www.isHttpError)
				{
					string analysisUrl = www.downloadHandler.text;
					Debug.Log(LOGPREFIX + "Logfile now available at " + analysisUrl);

					callback(analysisUrl);

					break;
				}

				Debug.Log(LOGPREFIX + "Error: " + www.error);
			}
			else
			{
				string url = "https://" + domainName + "/documents";

				WWW www = new WWW(url, encodedData);

				yield return www;

				if (www.error == null)
				{
					// example result
					// {"key":"oxekofidik"}

					string key = www.text;
					key = key.Substring(0, key.Length - 2);
					key = key.Substring(key.LastIndexOf("\"", StringComparison.InvariantCulture) + 1);
					string rawUrl = "https://" + domainName + "/raw/" + key;

					Debug.Log(LOGPREFIX + "Paste now available at " + rawUrl);

					callback(UrlHelper.Instance.LogAnalyserFor(rawUrl));

					break;
				}

				Debug.Log(LOGPREFIX + "Error: " + www.error);
			}
		}

		if (tooLong)
		{
			IRCConnection.SendMessage(TwitchPlaySettings.data.LogTooBig);
		}

		try
		{
			string filepath = Path.Combine(TwitchPlaySettings.data.TPSharedFolder, "Bomb Output Logs");
			Directory.CreateDirectory(filepath);
			File.WriteAllText(Path.Combine(filepath, $"Bomb {DateTime.Now:yyyy-MM-dd THH-mm-ss}.txt"), data);
			File.WriteAllText(Path.Combine(TwitchPlaySettings.data.TPSharedFolder, "Previous Bomb.txt"), data);
		}
		catch
		{
			//
		}
	}

	public void PostToChat(string analysisUrl, string format = "Analysis for this bomb: {0}", string emote = "copyThis") =>
		IRCConnection.SendMessageFormat($"{emote} {format}", analysisUrl);

	public void GetBombUrl()
	{
		previousUrl = null;
		postOnComplete = false;
		GetAnalyzerUrl(url =>
		{
			previousUrl = url;
			if (postOnComplete) PostToChat(url);
		});
	}

	internal void HandleLog(string message, string stackTrace, LogType type)
	{
		if (_blacklistedLogLines.Any(message.StartsWith)) return;
		if (message.StartsWith("Function ") && message.Contains(" may only be called from main thread!")) return;
		Log += message + "\n";
		if (type == LogType.Exception && !string.IsNullOrEmpty(stackTrace))
			Log += stackTrace + "\n";
	}
}
