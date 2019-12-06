using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Formatting = Newtonsoft.Json.Formatting;

[Flags]
public enum AccessLevel
{
	Streamer = 0x10000,
	SuperUser = 0x8000,
	Admin = 0x4000,
	Mod = 0x2000,

	Defuser = 0x0004,
	Banned = 0x0002,
	NoPoints = 0x0001,
	User = 0x0000
}

public class BanData
{
	public string BannedBy;
	public string BannedReason;
	public double BanExpiry;
}

public static class UserAccess
{
	private class UserAccessData
	{
		public bool StickyBans = false;
		public AccessLevel MinimumAccessLevelForBanCommand = AccessLevel.Mod;
		public AccessLevel MinimumAccessLevelForTimeoutCommand = AccessLevel.Mod;
		public AccessLevel MinimumAccessLevelForUnbanCommand = AccessLevel.Mod;
		public Dictionary<string, AccessLevel> UserAccessLevel = new Dictionary<string, AccessLevel>();

		public Dictionary<string, BanData> Bans = new Dictionary<string, BanData>();

		public static UserAccessData Instance
		{
			get => _instance ?? (_instance = new UserAccessData());
			set => _instance = value;
		}
		private static UserAccessData _instance;
	}

	static UserAccess()
	{
		/*
		 * Enter here the list of special user roles, giving them bitwise enum flags to determine the level of access each user has.
		 * 
		 * The access level enum can be extended further per your requirements.
		 * 
		 * Use the helper method below to determine if the user has access for a particular access level or not.
		 */

		//Twitch Usernames can't actually begin with an underscore, so these are safe to include as examples
		UserAccessData.Instance.UserAccessLevel["_UserNickName1".ToLowerInvariant()] = AccessLevel.SuperUser | AccessLevel.Admin | AccessLevel.Mod;
		UserAccessData.Instance.UserAccessLevel["_UserNickName2".ToLowerInvariant()] = AccessLevel.Mod;

		LoadAccessList();
	}

	public static void WriteAccessList()
	{
		string path = Path.Combine(Application.persistentDataPath, UsersSavePath);
		try
		{
			DebugHelper.Log($"UserAccess: Writing User Access information data to file: {path}");
			File.WriteAllText(path, JsonConvert.SerializeObject(UserAccessData.Instance, Formatting.Indented, new StringEnumConverter()));
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex);
		}
	}

	public static void LoadAccessList()
	{
		string path = Path.Combine(Application.persistentDataPath, UsersSavePath);
		//Try to read old format first.
		try
		{
			DebugHelper.Log($"UserAccess: Loading User Access information data from file: {path}");
			UserAccessData.Instance.UserAccessLevel = JsonConvert.DeserializeObject<Dictionary<string, AccessLevel>>(File.ReadAllText(path), new StringEnumConverter());
			UserAccessData.Instance.UserAccessLevel = UserAccessData.Instance.UserAccessLevel.ToDictionary(pair => pair.Key.ToLowerInvariant(), pair => pair.Value);
			WriteAccessList();
			return;
		}
		catch (FileNotFoundException)
		{
			DebugHelper.LogWarning($"UserAccess: File {path} was not found.");
			WriteAccessList();
			return;
		}
		catch (Exception ex)
		{
			try
			{
				UserAccessData.Instance = JsonConvert.DeserializeObject<UserAccessData>(File.ReadAllText(path), new StringEnumConverter());
			}
			catch (FileNotFoundException)
			{
				DebugHelper.LogWarning($"UserAccess: File {path} was not found.");
				WriteAccessList();
			}
			catch (Exception ex2)
			{
				DebugHelper.Log("Failed to load AccessLevels.Json in both the Old AND new format, Here are the stack traces.");
				DebugHelper.LogException(ex, "Old AccessLevels.Json format exception:");
				DebugHelper.LogException(ex2, "New AccessLevels.Json format exception:");
			}
		}

		UserAccessData.Instance.UserAccessLevel = UserAccessData.Instance.UserAccessLevel.ToDictionary(pair => pair.Key.ToLowerInvariant(), pair => pair.Value);
		UserAccessData.Instance.Bans = UserAccessData.Instance.Bans.ToDictionary(pair => pair.Key.ToLowerInvariant(), pair => pair.Value);

		foreach (string username in UserAccessData.Instance.UserAccessLevel.Keys
			.Where(x => HasAccess(x, AccessLevel.Banned)).ToArray())
			IsBanned(username);
	}
	public static string UsersSavePath = "AccessLevels.json";

	public static bool ModeratorsEnabled = true;

	public static bool HasAccess(string userNickName, AccessLevel accessLevel, bool orHigher = false)
	{
		if (userNickName == TwitchPlaySettings.data.TwitchPlaysDebugUsername || userNickName == "Bomb Factory")
			return true;
		if (!UserAccessData.Instance.UserAccessLevel.TryGetValue(userNickName.ToLowerInvariant(),
			out AccessLevel userAccessLevel))
			return accessLevel == AccessLevel.User;
		if (userAccessLevel == accessLevel)
			return true;

		do
		{
			if ((accessLevel & userAccessLevel) == accessLevel &&
				(ModeratorsEnabled || accessLevel < (AccessLevel) 0x2000 || accessLevel == AccessLevel.Streamer))
				return true;
			userAccessLevel = (AccessLevel) ((int) userAccessLevel >> 1);
		} while (userAccessLevel != AccessLevel.User && orHigher);

		return TwitchPlaySettings.data.AnarchyMode && userAccessLevel == AccessLevel.Defuser;
	}

	public static AccessLevel HighestAccessLevel(string userNickName)
	{
		if (userNickName == TwitchPlaySettings.data.TwitchPlaysDebugUsername) return AccessLevel.Streamer;

		if (userNickName.EqualsAny("Bomb Factory") || TwitchGame.Instance.Bombs.Any(x => x.BombName == userNickName)) return AccessLevel.Streamer;

		if (!UserAccessData.Instance.UserAccessLevel.TryGetValue(userNickName.ToLowerInvariant(),
			out AccessLevel userAccessLevel))
			return AccessLevel.User;
		if (IsBanned(userNickName) != null)
			return AccessLevel.Banned;
		for (AccessLevel level = (AccessLevel) 0x40000000; level > 0; level = (AccessLevel) ((int) level >> 1))
			if ((userAccessLevel & level) == level)
				return level;
		return TwitchPlaySettings.data.AnarchyMode ? AccessLevel.Defuser : AccessLevel.User;
	}

	public static void TimeoutUser(string userNickName, string moderator, string reason, int timeout, bool isWhisper)
	{
		if (!HasAccess(moderator, UserAccessData.Instance.MinimumAccessLevelForTimeoutCommand, true))
		{
			IRCConnection.SendMessage($"Sorry @{moderator}, you do not have sufficient privileges for this command.", moderator, !isWhisper);
			return;
		}
		if (timeout <= 0)
		{
			IRCConnection.SendMessage("Usage: !timeout <user nick name> <time in seconds> [reason].  Timeout must be for at least one second.", moderator, !isWhisper);
			return;
		}
		if (HasAccess(userNickName, AccessLevel.Streamer))
		{
			IRCConnection.SendMessage($"Sorry @{moderator}, you cannot timeout the streamer.", moderator, !isWhisper);
			return;
		}
		if (userNickName.EqualsIgnoreCase(moderator.ToLowerInvariant()))
		{
			IRCConnection.SendMessage($"Sorry @{moderator}, you cannot timeout yourself.", moderator, !isWhisper);
			return;
		}
		AddUser(userNickName, AccessLevel.Banned);
		if (!UserAccessData.Instance.Bans.TryGetValue(userNickName.ToLowerInvariant(), out BanData ban))
			ban = new BanData();
		ban.BannedBy = moderator;
		ban.BannedReason = reason;
		ban.BanExpiry = DateTime.Now.TotalSeconds() + timeout;
		UserAccessData.Instance.Bans[userNickName.ToLowerInvariant()] = ban;

		WriteAccessList();
		IRCConnection.SendMessage($"User {userNickName} was timed out from Twitch Plays for {timeout} seconds by {moderator}{(reason == null ? "." : $", for the following reason: {reason}")}");
		if (TwitchPlaySettings.data.EnableWhispers)
			IRCConnection.SendMessage($"You were timed out from Twitch Plays for {timeout} seconds by {moderator}{(reason == null ? "." : $", for the following reason: {reason}")}", userNickName, false);
	}

	public static void BanUser(string userNickName, string moderator, string reason, bool isWhisper)
	{
		if (!HasAccess(moderator, UserAccessData.Instance.MinimumAccessLevelForBanCommand, true))
		{
			IRCConnection.SendMessage($"Sorry @{moderator}, you do not have sufficient privileges for this command.", moderator, !isWhisper);
			return;
		}
		if (HasAccess(userNickName, AccessLevel.Streamer))
		{
			IRCConnection.SendMessage($"Sorry @{moderator}, you cannot ban the streamer.", moderator, !isWhisper);
			return;
		}
		if (userNickName.EqualsIgnoreCase(moderator))
		{
			IRCConnection.SendMessage($"Sorry @{moderator}, you cannot ban yourself.", moderator, !isWhisper);
			return;
		}
		AddUser(userNickName, AccessLevel.Banned);
		if (!UserAccessData.Instance.Bans.TryGetValue(userNickName.ToLowerInvariant(), out BanData ban))
			ban = new BanData();
		ban.BannedBy = moderator;
		ban.BannedReason = reason;
		ban.BanExpiry = double.PositiveInfinity;
		UserAccessData.Instance.Bans[userNickName.ToLowerInvariant()] = ban;
		WriteAccessList();
		IRCConnection.SendMessage($"User {userNickName} was banned permanently from Twitch Plays by {moderator}{(reason == null ? "." : $", for the following reason: {reason}")}");
		if (TwitchPlaySettings.data.EnableWhispers)
			IRCConnection.SendMessage($"You were banned permanently from Twitch Plays by {moderator}{(reason == null ? "." : $", for the following reason: {reason}")}", userNickName, false);
	}

	private static void UnbanUser(string userNickName, bool rewrite = true)
	{
		RemoveUser(userNickName, AccessLevel.Banned);
		if (UserAccessData.Instance.Bans.ContainsKey(userNickName.ToLowerInvariant()))
			UserAccessData.Instance.Bans.Remove(userNickName.ToLowerInvariant());
		if (rewrite)
			WriteAccessList();
	}

	public static void UnbanUser(string userNickName, string moderator, bool isWhisper)
	{
		if (!HasAccess(moderator, UserAccessData.Instance.MinimumAccessLevelForUnbanCommand, true))
		{
			IRCConnection.SendMessage($"Sorry @{moderator}, you do not have sufficient privileges for this command.", moderator, !isWhisper);
			return;
		}
		UnbanUser(userNickName);
		IRCConnection.SendMessage($"User {userNickName} was unbanned from Twitch plays by {moderator}.");
		if (TwitchPlaySettings.data.EnableWhispers)
			IRCConnection.SendMessage($"You were unbanned from Twitch Plays by {moderator}.", userNickName, false);
	}

	public static BanData IsBanned(string userNickName)
	{
		if (!UserAccessData.Instance.Bans.TryGetValue(userNickName.ToLowerInvariant(), out BanData ban) || !HasAccess(userNickName, AccessLevel.Banned))
		{
			bool rewrite = ban != null;
			rewrite |= HasAccess(userNickName, AccessLevel.Banned);
			UnbanUser(userNickName, rewrite);
			return null;
		}

		bool unban = ban.BanExpiry < DateTime.Now.TotalSeconds();
		if (!string.IsNullOrEmpty(ban.BannedBy) && !UserAccessData.Instance.StickyBans)
		{
			if (double.IsInfinity(ban.BanExpiry) && !HasAccess(ban.BannedBy, UserAccessData.Instance.MinimumAccessLevelForBanCommand, true))
			{
				unban = true;
				IRCConnection.SendMessage($"User {userNickName} is no longer banned from twitch plays because {ban.BannedBy} no longer has the power to issue permanent bans.");
				if (TwitchPlaySettings.data.EnableWhispers)
					IRCConnection.SendMessage($"You are no longer banned from twitch plays because {ban.BannedBy} no longer has the power to issue permanent bans.", userNickName, false);
			}
			if (!double.IsInfinity(ban.BanExpiry) && !HasAccess(ban.BannedBy, UserAccessData.Instance.MinimumAccessLevelForTimeoutCommand, true))
			{
				unban = true;
				IRCConnection.SendMessage($"User {userNickName} is no longer timed out from twitch plays because {ban.BannedBy} no longer has the power to issue time outs.");
				if (TwitchPlaySettings.data.EnableWhispers)
					IRCConnection.SendMessage($"You are no longer timed out from twitch plays because {ban.BannedBy} no longer has the power to issue time outs.", userNickName, false);
			}
		}
		else if (!UserAccessData.Instance.StickyBans && !unban)
		{
			IRCConnection.SendMessage($"User {userNickName} is no longer banned from twitch plays, as there is no one to hold accountable for the ban.");
			if (TwitchPlaySettings.data.EnableWhispers)
				IRCConnection.SendMessage("You are no longer banned from twitch plays, as there is no one to hold accountable for the ban.", userNickName, false);
			unban = true;
		}
		else
		{
			ban.BannedBy = IRCConnection.Instance.ChannelName;
		}

		unban |= HasAccess(userNickName, UserAccessData.Instance.MinimumAccessLevelForUnbanCommand)
					  || userNickName.EqualsIgnoreCase(ban.BannedBy);

		if (unban)
			UnbanUser(userNickName);

		return TwitchPlaySettings.data.AnarchyMode || unban ? null : ban;
	}

	public static void AddUser(string userNickName, AccessLevel level)
	{
		UserAccessData.Instance.UserAccessLevel.TryGetValue(userNickName.ToLowerInvariant(), out AccessLevel userAccessLevel);
		userAccessLevel |= level;
		UserAccessData.Instance.UserAccessLevel[userNickName.ToLowerInvariant()] = userAccessLevel;
	}

	public static void RemoveUser(string userNickName, AccessLevel level)
	{
		UserAccessData.Instance.UserAccessLevel.TryGetValue(userNickName.ToLowerInvariant(), out AccessLevel userAccessLevel);
		userAccessLevel &= ~level;
		UserAccessData.Instance.UserAccessLevel[userNickName.ToLowerInvariant()] = userAccessLevel;
	}

	public static Dictionary<string, AccessLevel> GetUsers() => UserAccessData.Instance.UserAccessLevel;

	public static Dictionary<string, BanData> GetBans() => UserAccessData.Instance.Bans;

	public static string LevelToString(AccessLevel level)
	{
		return level switch
		{
			AccessLevel.Banned => "Banned",
			AccessLevel.User => "User",
			AccessLevel.NoPoints => "No Points",
			AccessLevel.Defuser => "Defuser",
			AccessLevel.Mod => "Moderator",
			AccessLevel.Admin => "Admin",
			AccessLevel.SuperUser => "Super User",
			AccessLevel.Streamer => "Streamer",
			_ => null,
		};
	}
}

public static class AuditLog
{
	public static void SetupLog()
	{
		string path = Path.Combine(Application.persistentDataPath, SavePath);
		if (!File.Exists(path))
			File.Create(path);
	}

	public static void Log(string username, AccessLevel level, string command)
	{
		File.AppendAllText(Path.Combine(Application.persistentDataPath, SavePath), $"[{DateTime.Now}] {username} ({UserAccess.LevelToString(level)}): {command}{Environment.NewLine}");
	}

	public static string SavePath = "AuditLog.txt";
}
