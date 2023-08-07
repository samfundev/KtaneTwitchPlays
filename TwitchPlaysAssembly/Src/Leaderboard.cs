using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using System.Xml.Serialization;
using Newtonsoft.Json.Converters;
using UnityEngine;

public class Leaderboard
{
	public class LeaderboardEntry
	{
		public string UserName
		{
			get;
			set;
		}

		public Color UserColor
		{
			get;
			set;
		}

		public int SolveCount
		{
			get;
			set;
		}

		public int StrikeCount
		{
			get;
			set;
		}

		public int SolveScore
		{
			get;
			set;
		}

		public DateTime LastAction
		{
			get;
			set;
		}

		public int Rank
		{
			get;
			set;
		}

		public float RecordSoloTime
		{
			get;
			set;
		}

		public float TotalSoloTime
		{
			get;
			set;
		}

		public int TotalSoloClears
		{
			get;
			set;
		}

		public int SoloRank
		{
			get;
			set;
		}

		public bool OptOut
		{
			get;
			set;
		}

		[JsonConverter(typeof(StringEnumConverter))]
		public OtherModes.Team? Team { get; set; } = null;

		[JsonIgnore]
		private bool _active;

		[JsonIgnore]
		private Timer _activityTimer;

		[JsonIgnore]
		public bool Active
		{
			get => _active;
			set
			{
				_active = value;
				_activityTimer?.Dispose();
				_activityTimer = null;
				if (!value) return;
				_activityTimer = new Timer(1.8e+6) { AutoReset = false };
				_activityTimer.Elapsed += delegate { Active = false; };
				_activityTimer.Enabled = true;
			}
		}

		public float TimePerSoloSolve => TotalSoloClears == 0 ? 0 : RecordSoloTime / RequiredSoloSolves;

		public void AddSolve(int num = 1) => SolveCount += num;

		public void AddStrike(int num = 1) => StrikeCount += num;

		public void AddScore(int num) => SolveScore += num;
	}

	private static Color SafeGetColor(string userName) => IRCConnection.GetUserColor(userName);

	private bool GetEntry(string UserName, out LeaderboardEntry entry) => _entryDictionary.TryGetValue(UserName.ToLowerInvariant(), out entry);

	public LeaderboardEntry GetEntry(string userName)
	{
		if (!GetEntry(userName, out LeaderboardEntry entry))
		{
			entry = new LeaderboardEntry();
			_entryDictionary[userName.ToLowerInvariant()] = entry;
			_entryList.Add(entry);
			entry.UserColor = SafeGetColor(userName);
		}
		entry.UserName = userName;
		return entry;
	}

	private LeaderboardEntry GetEntry(string userName, Color userColor)
	{
		LeaderboardEntry entry = GetEntry(userName);
		entry.UserName = userName;
		entry.UserColor = userColor;
		return entry;
	}

	public void SetActivity(string userName, bool active)
	{
		LeaderboardEntry entry = GetEntry(userName);
		entry.Active = active;
	}

	public void OptOut(string userName)
	{
		var entry = GetEntry(userName);
		entry.OptOut = true;
		SaveDataToFile();
	}

	public LeaderboardEntry AddSoloClear(string userName, float newRecord, out float previousRecord)
	{
		LeaderboardEntry entry = _entryDictionary[userName.ToLowerInvariant()];
		previousRecord = entry.RecordSoloTime;
		if ((entry.TotalSoloClears < 1) || (newRecord < previousRecord))
		{
			entry.RecordSoloTime = newRecord;
		}
		entry.TotalSoloClears++;
		entry.TotalSoloTime += newRecord;
		ResetSortFlag();

		if (entry.TotalSoloClears == 1)
		{
			_entryListSolo.Add(entry);
		}

		SoloSolver = entry;
		return entry;
	}

	public void AddSolve(string userName, int numSolve = 1) => AddSolve(userName, SafeGetColor(userName), numSolve);
	public void AddSolve(string userName, Color userColor, int numSolve = 1)
	{
		LeaderboardEntry entry = GetEntry(userName, userColor);

		entry.AddSolve(numSolve);
		entry.LastAction = DateTime.Now;
		ResetSortFlag();

		string name = userName.ToLowerInvariant();
		CurrentSolvers[name] = CurrentSolvers.TryGetValue(name, out int value) ? value + numSolve : numSolve;
	}
	public void AddStrike(string userName, int numStrikes = 1) => AddStrike(userName, SafeGetColor(userName), numStrikes);

	public void AddStrike(string userName, Color userColor, int numStrikes = 1)
	{
		LeaderboardEntry entry = GetEntry(userName, userColor);

		entry.AddStrike(numStrikes);
		entry.LastAction = DateTime.Now;
		ResetSortFlag();
	}

	public void AddScore(string userName, int numScore) => AddScore(userName, SafeGetColor(userName), numScore);

	public void AddScore(string userName, Color userColor, int numScore)
	{
		LeaderboardEntry entry = GetEntry(userName, userColor);
		entry.AddScore(numScore);
		entry.LastAction = DateTime.Now;
		ResetSortFlag();

		string name = userName.ToLowerInvariant();
		if (!CurrentSolvers.ContainsKey(name))
			CurrentSolvers[name] = 0;
	}

	public void MakeEvil(string userName) => MakeEvil(userName, SafeGetColor(userName));

	public void MakeEvil(string userName, Color userColor)
	{
		LeaderboardEntry entry = GetEntry(userName, userColor);
		entry.Team = OtherModes.Team.Evil;
		entry.LastAction = DateTime.Now;
	}

	public void MakeGood(string userName) => MakeGood(userName, SafeGetColor(userName));

	public void MakeGood(string userName, Color userColor)
	{
		LeaderboardEntry entry = GetEntry(userName, userColor);
		entry.Team = OtherModes.Team.Good;
		entry.LastAction = DateTime.Now;
	}

	public bool IsAnyEvil() => _entryList.Any(x => x.Active && x.Team == OtherModes.Team.Evil);

	public bool IsAnyGood() => _entryList.Any(x => x.Active && x.Team == OtherModes.Team.Good);

	public bool IsTeamBalanced(OtherModes.Team team)
	{
		List<LeaderboardEntry> leaderboardEntries = _entryList.Where(x => x.Active).ToList();
		IEnumerable<LeaderboardEntry> givenTeamEntries = leaderboardEntries.Where(x => x.Team == team);

		return (double) givenTeamEntries.Count() / leaderboardEntries.Count < 0.75;
	}

	public OtherModes.Team? GetTeam(string userName) => GetTeam(userName, SafeGetColor(userName));

	public OtherModes.Team? GetTeam(string userName, Color userColor)
	{
		LeaderboardEntry entry = GetEntry(userName, userColor);
		return entry.Team;
	}

	public IEnumerable<LeaderboardEntry> GetSortedEntries(int count)
	{
		CheckAndSort();
		return _entryList.Take(count);
	}

	public IEnumerable<LeaderboardEntry> GetSortedSoloEntries(int count)
	{
		CheckAndSort();
		return _entryListSolo.Take(count);
	}

	public IEnumerable<LeaderboardEntry> GetSortedEntriesIncluding(Dictionary<string, int>.KeyCollection extras, int count)
	{
		var entries = new List<LeaderboardEntry>();

		foreach (string name in extras)
			if (GetEntry(name, out LeaderboardEntry entry))
				entries.Add(entry);

		//always add the top three in the leaderboard
		entries.AddRange(GetSortedEntries(3).Except(entries));

		if (entries.Count < count)
		{
			entries.AddRange(GetSortedEntries(count).Except(entries).Take(count - entries.Count));
		}

		entries.Sort(CompareScores);
		return entries.Take(count);
	}

	public IEnumerable<LeaderboardEntry> GetSortedSoloEntriesIncluding(string userName, int count)
	{
		List<LeaderboardEntry> ranking = GetSortedSoloEntries(count).ToList();

		LeaderboardEntry entry = _entryDictionary[userName.ToLowerInvariant()];
		if (entry.SoloRank > count)
		{
			ranking.RemoveAt(ranking.Count - 1);
			ranking.Add(entry);
		}

		return ranking;
	}

	public int GetRank(string userName, out LeaderboardEntry entry)
	{
		if (userName.StartsWith("@"))
			userName = userName.Substring(1);

		if (!GetEntry(userName, out entry))
		{
			return _entryList.Count + 1;
		}

		CheckAndSort();
		return _entryList.IndexOf(entry) + 1;
	}

	/// <summary>Gets a user's true rank. True rank doesn't rank/sort the user differently if they have opted out.</summary>
	public int GetTrueRank(string userName)
	{
		var trueList = new List<LeaderboardEntry>(_entryList);
		trueList.Sort((lhs, rhs) => CompareScores(lhs, rhs, true));

		var rank = trueList.IndexOf(entry => entry.UserName == userName) + 1;
		return rank == 0 ? _entryList.Count + 1 : rank;
	}

	public List<LeaderboardEntry> GetEntries(int rank)
	{
		CheckAndSort();
		return _entryList.Where(entry => entry.Rank == rank && (entry.SolveCount != 0 || entry.StrikeCount != 0) && !entry.OptOut).ToList();
	}

	public List<LeaderboardEntry> GetSoloEntries(int rank)
	{
		CheckAndSort();
		return _entryListSolo.Where(entry => entry.Rank == rank && (entry.SolveCount != 0 || entry.StrikeCount != 0)).ToList();
	}

	/// <summary>Gets all leaderboard entries that have a non-null team set for VS mode.</summary>
	public List<LeaderboardEntry> GetVSEntries()
	{
		return _entryList.Where(entry => entry.Team != null).ToList();
	}

	public int GetSoloRank(string userName, out LeaderboardEntry entry)
	{
		if (userName.StartsWith("@"))
			userName = userName.Substring(1);

		entry = _entryListSolo.Find(x => string.Equals(x.UserName, userName, StringComparison.InvariantCultureIgnoreCase));
		return entry != null ? _entryListSolo.IndexOf(entry) : 0;
	}

	public void GetTotalSolveStrikeCounts(out int solveCount, out int strikeCount, out int scoreCount)
	{
		solveCount = 0;
		strikeCount = 0;
		scoreCount = 0;

		foreach (LeaderboardEntry entry in _entryList)
		{
			solveCount += entry.SolveCount;
			strikeCount += entry.StrikeCount;
			scoreCount += entry.SolveScore;
		}
	}

	public void AddEntry(LeaderboardEntry user)
	{
		LeaderboardEntry entry = GetEntry(user.UserName, user.UserColor);
		entry.SolveCount = user.SolveCount;
		entry.StrikeCount = user.StrikeCount;
		entry.SolveScore = user.SolveScore;
		entry.LastAction = user.LastAction;
		entry.RecordSoloTime = user.RecordSoloTime;
		entry.Team = user.Team;
		entry.TotalSoloTime = user.TotalSoloTime;
		entry.TotalSoloClears = user.TotalSoloClears;
		entry.OptOut = user.OptOut;

		if (entry.TotalSoloClears > 0)
		{
			_entryListSolo.Add(entry);
		}
	}

	public void AddEntries(List<LeaderboardEntry> entries)
	{
		foreach (LeaderboardEntry entry in entries)
		{
			AddEntry(entry);
		}
	}

	public void DeleteEntry(LeaderboardEntry user)
	{
		_entryDictionary.Remove(user.UserName.ToLowerInvariant());
		_entryList.Remove(user);
	}

	public void DeleteEntry(string userNickName) => DeleteEntry(GetEntry(userNickName));

	public void DeleteSoloEntry(LeaderboardEntry user) => _entryListSolo.Remove(user);

	public void DeleteSoloEntry(string userNickName) => DeleteSoloEntry(_entryListSolo.First(x => string.Equals(x.UserName, userNickName, StringComparison.InvariantCultureIgnoreCase)));

	public void ResetLeaderboard()
	{
		_entryDictionary.Clear();
		_entryList.Clear();
		_entryListSolo.Clear();
		CurrentSolvers.Clear();
		BombsAttempted = 0;
		BombsCleared = 0;
		BombsExploded = 0;
		OldBombsAttempted = 0;
		OldBombsCleared = 0;
		OldBombsExploded = 0;
		OldScore = 0;
		OldSolves = 0;
		OldStrikes = 0;
	}

	private void ResetSortFlag() => _sorted = false;

	private void CheckAndSort()
	{
		if (_sorted)
			return;

		_entryList.Sort(CompareScores);
		_entryListSolo.Sort(CompareSoloTimes);
		_sorted = true;

		int i = 1;
		LeaderboardEntry previous = null;
		foreach (LeaderboardEntry entry in _entryList)
		{
			entry.Rank = previous == null ? 1 : (CompareScores(entry, previous) == 0) ? previous.Rank : i;
			previous = entry;
			i++;
		}

		i = 1;
		foreach (LeaderboardEntry entry in _entryListSolo)
		{
			entry.SoloRank = i++;
		}
	}

	private static int CompareScores(LeaderboardEntry lhs, LeaderboardEntry rhs) => CompareScores(lhs, rhs, false);

	private static int CompareScores(LeaderboardEntry lhs, LeaderboardEntry rhs, bool trueRank)
	{
		if (!trueRank)
		{
			// People who have opted out of scores get sorted separately to prevent score guessing.
			if (lhs.OptOut && !rhs.OptOut) return 1;
			else if (!lhs.OptOut && rhs.OptOut) return -1;
			else if (lhs.OptOut && rhs.OptOut) return rhs.SolveCount.CompareTo(lhs.SolveCount);
		}

		if (lhs.SolveScore != rhs.SolveScore)
		{
			// Intentionally reversed comparison to sort from highest to lowest
			return rhs.SolveScore.CompareTo(lhs.SolveScore);
		}

		// Intentionally reversed comparison to sort from highest to lowest
		return rhs.SolveCount.CompareTo(lhs.SolveCount);
	}

	private static int CompareSoloTimes(LeaderboardEntry lhs, LeaderboardEntry rhs) => lhs.RecordSoloTime.CompareTo(rhs.RecordSoloTime);

	public void ClearSolo()
	{
		SoloSolver = null;
		CurrentSolvers.Clear();
	}

	public void LoadDataFromFile()
	{
		_loaded = false;
		string path = Path.Combine(Application.persistentDataPath, usersSavePath);
		try
		{
			DebugHelper.Log($"Loading leaderboard data from file: {path}");
			XmlSerializer xml = new XmlSerializer(_entryList.GetType());
			using (TextReader reader = new StreamReader(path))
			{
				List<LeaderboardEntry> entries = (List<LeaderboardEntry>) xml.Deserialize(reader);
				AddEntries(entries);
			}
			ResetSortFlag();

			path = Path.Combine(Application.persistentDataPath, statsSavePath);
			DebugHelper.Log($"Loading stats data from file: {path}");
			string jsonInput = File.ReadAllText(path);
			Dictionary<string, int> stats = JsonConvert.DeserializeObject<Dictionary<string, int>>(jsonInput);

			BombsAttempted = stats["BombsAttempted"];
			BombsCleared = stats["BombsCleared"];
			BombsExploded = stats["BombsExploded"];
			OldBombsAttempted = BombsAttempted;
			OldBombsCleared = BombsCleared;
			OldBombsExploded = BombsExploded;

			GetTotalSolveStrikeCounts(out OldSolves, out OldStrikes, out OldScore);
			_loaded = true;
		}
		catch (FileNotFoundException)
		{
			DebugHelper.LogWarning($"File {path} was not found.");
			_loaded = true;
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex);
		}

		if (!_loaded)
			TwitchPlaysService.Instance.CoroutineQueue.AddToQueue(SendLeaderboardWarning());
	}

	public void SaveDataToFile()
	{
		if (!_loaded)
		{
			DebugHelper.LogError("Cannot be saved as it did not load successfully.");
			return;
		}

		string path = Path.Combine(Application.persistentDataPath, usersSavePath);
		try
		{
			if (!_sorted)
			{
				CheckAndSort();
			}

			DebugHelper.Log($"Saving leaderboard data to file: {path}");
			XmlSerializer xml = new XmlSerializer(_entryList.GetType());
			using (TextWriter writer = new StreamWriter(path))
			{
				xml.Serialize(writer, _entryList);
			}

			path = Path.Combine(Application.persistentDataPath, statsSavePath);
			DebugHelper.Log($"Saving stats data to file: {path}");
			Dictionary<string, int> stats = new Dictionary<string, int>
			{
				{ "BombsAttempted", BombsAttempted },
				{ "BombsCleared", BombsCleared },
				{ "BombsExploded", BombsExploded }
			};
			string jsonOutput = JsonConvert.SerializeObject(stats, Formatting.Indented, new JsonSerializerSettings()
			{
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
			});
			File.WriteAllText(path, jsonOutput);
		}
		catch (FileNotFoundException)
		{
			DebugHelper.LogWarning($"File {path} was not found.");
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex);
		}
	}

	private static IEnumerator<WaitUntil> SendLeaderboardWarning()
	{
		yield return new WaitUntil(() => IRCConnection.Instance.State == IRCConnectionState.Connected);

		IRCConnection.SendMessage("Warning: Unable to load the leaderboard successfully so the leaderboard will not be saved. Please check the log.");
	}

	public int Count => _entryList.Count;

	public int SoloCount => _entryListSolo.Count;

	private readonly Dictionary<string, LeaderboardEntry> _entryDictionary = new Dictionary<string, LeaderboardEntry>();
	private readonly List<LeaderboardEntry> _entryList = new List<LeaderboardEntry>();
	private readonly List<LeaderboardEntry> _entryListSolo = new List<LeaderboardEntry>();
	private static Leaderboard _instance;
	private bool _sorted = false;
	private bool _loaded = false;
	public bool Success = false;

	public int BombsAttempted = 0;
	public int BombsCleared = 0;
	public int BombsExploded = 0;
	public int OldBombsAttempted = 0;
	public int OldBombsCleared = 0;
	public int OldBombsExploded = 0;
	public int OldSolves = 0;
	public int OldStrikes = 0;
	public int OldScore = 0;

	public LeaderboardEntry SoloSolver = null;
	public Dictionary<string, int> CurrentSolvers = new Dictionary<string, int>();

	public static int RequiredSoloSolves = 11;
	public static string usersSavePath = "TwitchPlaysUsers.xml";
	public static string statsSavePath = "TwitchPlaysStats.json";

	public static Leaderboard Instance => _instance ?? (_instance = new Leaderboard());
}
