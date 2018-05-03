using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using Formatting = Newtonsoft.Json.Formatting;

[Flags()]
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
	public Double BanExpiry;
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
		string path = Path.Combine(Application.persistentDataPath, usersSavePath);
		try
		{
			DebugHelper.Log("UserAccess: Writing User Access information data to file: {0}", path);
			File.WriteAllText(path, JsonConvert.SerializeObject(UserAccessData.Instance, Formatting.Indented, new StringEnumConverter()));
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex);
		}
	}

	public static void LoadAccessList()
	{
		string path = Path.Combine(Application.persistentDataPath, usersSavePath);
		//Try to read old format first.
		try
		{
			DebugHelper.Log("UserAccess: Loading User Access information data from file: {0}", path);
			UserAccessData.Instance.UserAccessLevel = JsonConvert.DeserializeObject<Dictionary<string, AccessLevel>>(File.ReadAllText(path), new StringEnumConverter());
			UserAccessData.Instance.UserAccessLevel = UserAccessData.Instance.UserAccessLevel.ToDictionary(pair => pair.Key.ToLowerInvariant(), pair => pair.Value);
			WriteAccessList();
			return;
		}
		catch (FileNotFoundException)
		{
			DebugHelper.LogWarning("UserAccess: File {0} was not found.", path);
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
				DebugHelper.LogWarning("UserAccess: File {0} was not found.", path);
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

		foreach (string username in UserAccessData.Instance.UserAccessLevel.Keys.Where(x => HasAccess(x, AccessLevel.Banned)).ToArray())
		{
			IsBanned(username);
		}

	}
	public static string usersSavePath = "AccessLevels.json";

	public static bool HasAccess(string userNickName, AccessLevel accessLevel, bool orHigher = false)
	{
		if (userNickName == TwitchPlaySettings.data.TwitchPlaysDebugUsername)
		{
			return true;
		}
		if (!UserAccessData.Instance.UserAccessLevel.TryGetValue(userNickName.ToLowerInvariant(), out AccessLevel userAccessLevel))
		{
			return accessLevel == AccessLevel.User;
		}
		if (userAccessLevel == accessLevel)
		{
			return true;
		}

		do
		{
			if ((accessLevel & userAccessLevel) == accessLevel)
			{
				return true;
			}
			userAccessLevel = (AccessLevel) ((int) userAccessLevel >> 1);
		} while (userAccessLevel != AccessLevel.User && orHigher);

		return false;
	}

	public static AccessLevel HighestAccessLevel(string userNickName)
	{
		if (userNickName == TwitchPlaySettings.data.TwitchPlaysDebugUsername) return AccessLevel.Streamer;

		if (userNickName.EqualsAny("Bomb Factory") || BombMessageResponder.Instance.BombHandles.Select(x => x.nameText.text).Contains(userNickName)) return AccessLevel.Streamer;

		if (!UserAccessData.Instance.UserAccessLevel.TryGetValue(userNickName.ToLowerInvariant(), out AccessLevel userAccessLevel))
		{
			return AccessLevel.User;
		}
		if(IsBanned(userNickName) != null)
			return AccessLevel.Banned;
		for (AccessLevel level = (AccessLevel)0x40000000; level > 0; level = (AccessLevel)((int)level >> 1))
		{
			if ((userAccessLevel & level) == level)
				return level;
		}
		return AccessLevel.User;
	}

	public static void TimeoutUser(string userNickName, string moderator, string reason, int timeout)
	{
		if (!HasAccess(moderator, UserAccessData.Instance.MinimumAccessLevelForTimeoutCommand, true))
		{
			IRCConnection.Instance.SendMessage($"Sorry @{moderator}, you do not have sufficient priveleges for this command.");
			return;
		}
		if (timeout <= 0)
		{
			IRCConnection.Instance.SendMessage("Usage: !timeout <user nick name> <time in seconds> [reason].  Timeout must be for at least one second.");
			return;
		}
		if (HasAccess(userNickName, AccessLevel.Streamer))
		{
			IRCConnection.Instance.SendMessage($"Sorry @{moderator}, you cannot timeout the streamer.");
			return;
		}
		if (userNickName.ToLowerInvariant().Equals(moderator.ToLowerInvariant()))
		{
			IRCConnection.Instance.SendMessage($"Sorry @{moderator}, you cannot timeout yourself.");
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
		IRCConnection.Instance.SendMessage($"User {userNickName} was timed out from Twitch Plays for {timeout} seconds by {moderator}{(reason == null ? "." : $", for the following reason: {reason}")}");
	}

	public static void BanUser(string userNickName, string moderator, string reason)
	{
		if (!HasAccess(moderator, UserAccessData.Instance.MinimumAccessLevelForBanCommand, true))
		{
			IRCConnection.Instance.SendMessage($"Sorry @{moderator}, you do not have sufficient priveleges for this command.");
			return;
		}
		if (HasAccess(userNickName, AccessLevel.Streamer))
		{
			IRCConnection.Instance.SendMessage($"Sorry @{moderator}, you cannot ban the streamer.");
			return;
		}
		if (userNickName.ToLowerInvariant().Equals(moderator.ToLowerInvariant()))
		{
			IRCConnection.Instance.SendMessage($"Sorry @{moderator}, you cannot ban yourself.");
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
		IRCConnection.Instance.SendMessage($"User {userNickName} was banned permanently from Twitch Plays by {moderator}{(reason == null ? "." : $", for the following reason: {reason}")}");
	}

	private static void UnbanUser(string userNickName, bool rewrite=true)
	{
		RemoveUser(userNickName, AccessLevel.Banned);
		if (UserAccessData.Instance.Bans.ContainsKey(userNickName.ToLowerInvariant()))
			UserAccessData.Instance.Bans.Remove(userNickName.ToLowerInvariant());
		if(rewrite)
			WriteAccessList();
	}

	public static void UnbanUser(string userNickName, string moderator)
	{
		if (!HasAccess(moderator, UserAccessData.Instance.MinimumAccessLevelForUnbanCommand, true))
		{
			IRCConnection.Instance.SendMessage($"Sorry @{moderator}, you do not have sufficient priveleges for this command.");
			return;
		}
		UnbanUser(userNickName);
		IRCConnection.Instance.SendMessage($"User {userNickName} was unbanned from Twitch plays.");
	}

	public static BanData IsBanned(string usernickname)
	{
		if (!UserAccessData.Instance.Bans.TryGetValue(usernickname.ToLowerInvariant(), out BanData ban) || !HasAccess(usernickname, AccessLevel.Banned))
		{
			bool rewrite = ban != null;
			rewrite |= HasAccess(usernickname, AccessLevel.Banned);
			UnbanUser(usernickname, rewrite);
			return null;
		}

		bool unban = ban.BanExpiry < DateTime.Now.TotalSeconds();
		if (!string.IsNullOrEmpty(ban.BannedBy) && !UserAccessData.Instance.StickyBans)
		{
			if (double.IsInfinity(ban.BanExpiry) && !HasAccess(ban.BannedBy, UserAccessData.Instance.MinimumAccessLevelForBanCommand))
			{
				unban = true;
				IRCConnection.Instance.SendMessage($"User {usernickname} is no longer banned from twitch plays because {ban.BannedBy} no longer has the power to issue permanent bans.");
			}
			if (!double.IsInfinity(ban.BanExpiry) && !HasAccess(ban.BannedBy, UserAccessData.Instance.MinimumAccessLevelForTimeoutCommand))
			{
				unban = true;
				IRCConnection.Instance.SendMessage($"User {usernickname} is no longer timed out from twitch plays because {ban.BannedBy} no longer has the power to issue time outs.");
			}
		}
		else if (!UserAccessData.Instance.StickyBans && !unban)
		{
			IRCConnection.Instance.SendMessage($"User {usernickname} is no longer banned from twitch plays, as there is no one to hold accoutable for the ban.");
			unban = true;
		}
		else
		{
			ban.BannedBy = IRCConnection.Instance.ChannelName;
		}

		if (!unban && !HasAccess(usernickname, UserAccessData.Instance.MinimumAccessLevelForUnbanCommand) 
			&& !usernickname.ToLowerInvariant().Equals(ban.BannedBy.ToLowerInvariant())) return ban;

		UnbanUser(usernickname);
		return null;
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

	public static Dictionary<string, AccessLevel> GetUsers()
	{
		return UserAccessData.Instance.UserAccessLevel;
	}

	public static Dictionary<string, BanData> GetBans()
	{
		return UserAccessData.Instance.Bans;
	}

	public static string LevelToString(AccessLevel level)
	{
		switch (level)
		{
			case AccessLevel.Banned:
				return "Banned";
			case AccessLevel.User:
				return "User";
			case AccessLevel.NoPoints:
				return "No Points";
			case AccessLevel.Defuser:
				return "Defuser";
			case AccessLevel.Mod:
				return "Moderator";
			case AccessLevel.Admin:
				return "Admin";
			case AccessLevel.SuperUser:
				return "Super User";
			case AccessLevel.Streamer:
				return "Streamer";
			default:
				return null;
		}
	}
}
