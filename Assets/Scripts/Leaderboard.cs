using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System;
using System.Xml.Serialization;

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

        public float SolveRate
        {
            get
            {
                if (StrikeCount == 0)
                {
                    return SolveCount;
                }

                return ((float)SolveCount) / StrikeCount;
            }
        }

        public float TimePerSoloSolve
        {
            get
            {
                if (TotalSoloClears == 0)
                {
                    return 0;
                }
                return ((float)RecordSoloTime) / Leaderboard.RequiredSoloSolves;
            }
        }

        public void AddSolve(int num = 1)
        {
            SolveCount += num;
        }

        public void AddStrike(int num = 1)
        {
            StrikeCount += num;
        }

    }

    private bool GetEntry(string UserName, out LeaderboardEntry entry)
    {
        return _entryDictionary.TryGetValue(UserName.ToLowerInvariant(), out entry);
    }

    private LeaderboardEntry GetEntry(string userName, Color userColor)
    {
        LeaderboardEntry entry = null;
        if (!GetEntry(userName, out entry))
        {
            entry = new LeaderboardEntry();
            _entryDictionary[userName.ToLowerInvariant()] = entry;
            _entryList.Add(entry);
        }
        entry.UserName = userName;
        entry.UserColor = userColor;
        return entry;
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

    public void AddSolve(string userName, Color userColor)
    {
        LeaderboardEntry entry = GetEntry(userName, userColor);

        entry.AddSolve();
        entry.LastAction = DateTime.Now;
        ResetSortFlag();

        string name = userName.ToLowerInvariant();
        int value = 0;
        CurrentSolvers[name] = CurrentSolvers.TryGetValue(name, out value) ? value + 1 : 1;
    }

    public void AddStrike(string userName, Color userColor, int numStrikes)
    {
        LeaderboardEntry entry = GetEntry(userName, userColor);

        entry.AddStrike(numStrikes);
        entry.LastAction = DateTime.Now;
        ResetSortFlag();
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

    public IEnumerable<LeaderboardEntry> GetSortedEntriesIncluding(Dictionary<string, int>.KeyCollection entries, int count)
    {
        List<LeaderboardEntry> ranking = GetSortedEntries(count).ToList();
        List<LeaderboardEntry> extras = new List<LeaderboardEntry>();

        LeaderboardEntry entry;
        foreach (string name in entries)
        {
            if (GetRank(name, out entry) > count)
            {
                extras.Add(entry);
            }
        }
        extras.Sort(CompareScores);

        return ranking.Take(count - extras.Count).Concat(extras);
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
        if (!GetEntry(userName, out entry))
        {
            return _entryList.Count + 1;
        }

        CheckAndSort();
        return _entryList.IndexOf(entry) + 1;
    }

    public int GetRank(int rank, out LeaderboardEntry entry)
    {
        CheckAndSort();
        entry = (_entryList.Count >= rank) ? _entryList[rank - 1] : null;
        return (entry != null) ? entry.Rank : 0;
    }

    public int GetSoloRank(int rank, out LeaderboardEntry entry)
    {
        CheckAndSort();
        entry = (_entryListSolo.Count >= rank) ? _entryListSolo[rank - 1] : null;
        return (entry != null) ? entry.SoloRank : 0;
    }

    public void GetTotalSolveStrikeCounts(out int solveCount, out int strikeCount)
    {
        solveCount = 0;
        strikeCount = 0;

        foreach (LeaderboardEntry entry in _entryList)
        {
            solveCount += entry.SolveCount;
            strikeCount += entry.StrikeCount;
        }
    }

    public void AddEntry(LeaderboardEntry user)
    {
        LeaderboardEntry entry = GetEntry(user.UserName, user.UserColor);
        entry.SolveCount = user.SolveCount;
        entry.StrikeCount = user.StrikeCount;
        entry.LastAction = user.LastAction;
        entry.RecordSoloTime = user.RecordSoloTime;
        entry.TotalSoloTime = user.TotalSoloTime;
        entry.TotalSoloClears = user.TotalSoloClears;

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

    private void ResetSortFlag()
    {
        _sorted = false;
    }

    private void CheckAndSort()
    {
        if (!_sorted)
        {
            _entryList.Sort(CompareScores);
            _entryListSolo.Sort(CompareSoloTimes);
            _sorted = true;

            int i = 1;
            LeaderboardEntry previous = null;
            foreach (LeaderboardEntry entry in _entryList)
            {
                if (previous == null)
                {
                    entry.Rank = 1;
                }
                else
                {
                    entry.Rank = (CompareScores(entry, previous) == 0) ? previous.Rank : i;
                }
                previous = entry;
                i++;
            }

            i = 1;
            foreach (LeaderboardEntry entry in _entryListSolo)
            {
                entry.SoloRank = i++;
            }
        }
    }

    private static int CompareScores(LeaderboardEntry lhs, LeaderboardEntry rhs)
    {
        if (lhs.SolveCount != rhs.SolveCount)
        {
            //Intentially reversed comparison to sort from highest to lowest
            return rhs.SolveCount.CompareTo(lhs.SolveCount);
        }

        //Intentially reversed comparison to sort from highest to lowest
        return rhs.SolveRate.CompareTo(lhs.SolveRate);
    }

    private static int CompareSoloTimes(LeaderboardEntry lhs, LeaderboardEntry rhs)
    {
        return lhs.RecordSoloTime.CompareTo(rhs.RecordSoloTime);
    }

    public void ClearSolo()
    {
        SoloSolver = null;
        CurrentSolvers.Clear();
    }

    public void LoadDataFromFile()
    {
        string path = Path.Combine(Application.persistentDataPath, usersSavePath);
        try
        {
            Debug.Log("Leaderboard: Loading leaderboard data from file: " + path);
            XmlSerializer xml = new XmlSerializer(_entryList.GetType());
            TextReader reader = new StreamReader(path);
            List<LeaderboardEntry> entries = (List<LeaderboardEntry>)xml.Deserialize(reader);
            AddEntries(entries);
            ResetSortFlag();

            path = Path.Combine(Application.persistentDataPath, statsSavePath);
            Debug.Log("Leaderboard: Loading stats data from file: " + path);
            string jsonInput = File.ReadAllText(path);
            Dictionary<string, int> stats = JsonConvert.DeserializeObject<Dictionary<string, int>>(jsonInput);

            BombsAttempted = stats["BombsAttempted"];
            BombsCleared = stats["BombsCleared"];
            BombsExploded = stats["BombsExploded"];
            OldBombsAttempted = BombsAttempted;
            OldBombsCleared = BombsCleared;
            OldBombsExploded = BombsExploded;

            GetTotalSolveStrikeCounts(out OldSolves, out OldStrikes);
        }
        catch (FileNotFoundException)
        {
            Debug.LogWarningFormat("Leaderboard: File {0} was not found.", path);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    public void SaveDataToFile()
    {
        string path = Path.Combine(Application.persistentDataPath, usersSavePath);
        try
        {
            if (!_sorted)
            {
                CheckAndSort();
            }

            Debug.Log("Leaderboard: Saving leaderboard data to file: " + path);
            XmlSerializer xml = new XmlSerializer(_entryList.GetType());
            TextWriter writer = new StreamWriter(path);
            xml.Serialize(writer, _entryList);

            path = Path.Combine(Application.persistentDataPath, statsSavePath);
            Debug.Log("Leaderboard: Saving stats data to file: " + path);
            Dictionary<string, int> stats = new Dictionary<string, int>
            {
                { "BombsAttempted", BombsAttempted },
                { "BombsCleared", BombsCleared },
                { "BombsExploded", BombsExploded }
            };
            string jsonOutput = JsonConvert.SerializeObject(stats, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
            });
            File.WriteAllText(path, jsonOutput);
        }
        catch (FileNotFoundException)
        {
            Debug.LogWarningFormat("Leaderboard: File {0} was not found.", path);
        }
        catch (Exception ex)
        {
            string msg = ex.ToString();
            Debug.LogWarning(msg);
        }
    }

    public int Count
    {
        get
        {
            return _entryList.Count;
        }
    }

    public int SoloCount
    {
        get
        {
            return _entryListSolo.Count;
        }
    }


    private Dictionary<string, LeaderboardEntry> _entryDictionary = new Dictionary<string, LeaderboardEntry>();
    private List<LeaderboardEntry> _entryList = new List<LeaderboardEntry>();
    private List<LeaderboardEntry> _entryListSolo = new List<LeaderboardEntry>();
    private bool _sorted = false;
    public bool Success = false;

    public int BombsAttempted = 0;
    public int BombsCleared = 0;
    public int BombsExploded = 0;
    public int OldBombsAttempted = 0;
    public int OldBombsCleared = 0;
    public int OldBombsExploded = 0;
    public int OldSolves = 0;
    public int OldStrikes = 0;

    public LeaderboardEntry SoloSolver = null;
    public Dictionary<string, int> CurrentSolvers = new Dictionary<string, int>();

    public static int RequiredSoloSolves = 11;
    public static string usersSavePath = "TwitchPlaysUsers.xml";
    public static string statsSavePath = "TwitchPlaysStats.json";
}
