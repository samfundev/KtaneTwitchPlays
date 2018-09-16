using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

public class LogUploader : MonoBehaviour
{
	public static LogUploader Instance;

	public string Log { get; private set; }

	[HideInInspector]
	public string analysisUrl = null;

	[HideInInspector]
	public bool postOnComplete = false;

	[HideInInspector]
	public string LOGPREFIX;

	//private string output;

	private string[] _blacklistedLogLines =
	{
		"[PaceMaker]",
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
		// In order of preference (favourite first)
		// The integer value is the data size limit in bytes
		{ "ktane.timwi.de", 25000000 },
		{ "hastebin.com", 400000 }
	};

	public void Awake()
	{
		Instance = this;
	}

	public void OnEnable()
	{
		LOGPREFIX = "[" + GetType().Name + "] ";
		Application.logMessageReceived += HandleLog;
	}

	public void OnDisable()
	{
		Application.logMessageReceived -= HandleLog;
	}

	public void Clear()
	{
		Log = "";
	}

	public string Flush()
	{
		string result = Log;
		Log = "";
		return result;
	}

	public void Post(bool postToChat = true)
	{
		analysisUrl = null;
		postOnComplete = false;
		StartCoroutine( DoPost(Log, postToChat) );
	}

	private IEnumerator DoPost(string data, bool postToChat)
	{
		// This first line is necessary as the Log Analyser uses it as an identifier
		data = "Initialize engine version: Twitch Plays\n" + data;

		byte[] encodedData = System.Text.Encoding.UTF8.GetBytes(data);
		int dataLength = encodedData.Length;

		bool tooLong = false;

		foreach (DictionaryEntry domain in domainNames)
		{
			string domainName = (string)domain.Key;
			int maxLength = (int)domain.Value;

			tooLong = false;
			if (dataLength >= maxLength)
			{
				Debug.LogFormat(LOGPREFIX + "Data ({0}B) is too long for {1} ({2}B)", dataLength, domainName, maxLength);
				tooLong = true;
				continue;
			}

			Debug.Log(LOGPREFIX + "Posting new log to " + domainName);
			string url;
			WWW www;
			if (domainName == "ktane.timwi.de")
			{
				url = "https://" + domainName + "/upload-log";
				WWWForm wwwform = new WWWForm();
				wwwform.AddField("name", "log");
				wwwform.AddBinaryData("upload", encodedData, $"Bomb { DateTime.Now:yyyy-MM-dd THH-mm-ss}.txt");
				www = new WWW(url, wwwform);
			}
			else
			{
				url = "https://" + domainName + "/documents";
				www = new WWW(url, encodedData);
			}

			yield return www;

			if (www.error == null)
			{
				string rawUrl;
				if (domainName == "ktane.timwi.de")
				{
					var sha1 = SHA1.Create().ComputeHash(encodedData).ToHex();
					rawUrl = "https://ktane.timwi.de/Logfiles/" + sha1;
					DebugHelper.Log(sha1);
				}
				else
				{
					// example result
					// {"key":"oxekofidik"}
					string key = www.text;
					key = key.Substring(0, key.Length - 2);
					key = key.Substring(key.LastIndexOf("\"") + 1);
					rawUrl = "https://" + domainName + "/raw/" + key;
				}

				Debug.Log(LOGPREFIX + "Paste now available at " + rawUrl);

				analysisUrl = UrlHelper.Instance.LogAnalyserFor(rawUrl);

				if (postOnComplete)
				{
					PostToChat();
				}

				break;
			}
			else
			{
				Debug.Log(LOGPREFIX + "Error: " + www.error + ", " + www.text);
			}
		}

		if (tooLong)
		{
			IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.LogTooBig);
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

		yield break;
	}

	public bool PostToChat(string format = "Analysis for this bomb: {0}", string emote = "copyThis")
	{
		if (string.IsNullOrEmpty(analysisUrl))
		{
			Debug.Log(LOGPREFIX + "No analysis URL available, can't post to chat");
			return false;
		}
		Debug.Log(LOGPREFIX + "Posting analysis URL to chat");
		emote = " " + emote + " ";
		IRCConnection.Instance.SendMessage(emote + format, analysisUrl);
		return true;
	}

	private void HandleLog(string message, string stackTrace, LogType type)
	{
		if (_blacklistedLogLines.Any(message.StartsWith)) return;
		if (message.StartsWith("Function ") && message.Contains(" may only be called from main thread!")) return;
		Log += message + "\n";
	}

}
