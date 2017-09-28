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
    SuperUser = 0x8000,
    Admin = 0x4000,
    Mod = 0x2000,
    
    Defuser = 0x0002,
    NoPoints = 0x0001,
    User = 0x0000
}

public static class UserAccess
{ 
    private static Dictionary<string, AccessLevel> AccessLevels = new Dictionary<string, AccessLevel>();

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
        AccessLevels["_UserNickName1".ToLowerInvariant()] = AccessLevel.SuperUser | AccessLevel.Admin | AccessLevel.Mod;
        AccessLevels["_UserNickName2".ToLowerInvariant()] = AccessLevel.Mod;

        LoadAccessList();
    }

    public static void WriteAccessList()
    {
        string path = Path.Combine(Application.persistentDataPath, usersSavePath);
        try
        {
            DebugHelper.Log("UserAccess: Writing User Access information data to file: {0}", path);
            File.WriteAllText(path, JsonConvert.SerializeObject(AccessLevels,Formatting.Indented,new StringEnumConverter()));
        }
        catch (Exception ex)
        {
            DebugHelper.LogException(ex);
        }
    }

    public static void LoadAccessList()
    {
        string path = Path.Combine(Application.persistentDataPath, usersSavePath);
        try
        {
            DebugHelper.Log("UserAccess: Loading User Access information data from file: {0}", path);
            AccessLevels = JsonConvert.DeserializeObject<Dictionary<string, AccessLevel>>(File.ReadAllText(path), new StringEnumConverter());
        }
        catch (FileNotFoundException)
        {
            DebugHelper.LogWarning("UserAccess: File {0} was not found.", path);
            WriteAccessList();
        }
        catch (Exception ex)
        {
            DebugHelper.LogException(ex);
        }

		AccessLevels = AccessLevels.ToDictionary(pair => pair.Key.ToLowerInvariant(), pair => pair.Value);
    }
    public static string usersSavePath = "AccessLevels.json";

    public static bool HasAccess(string userNickName, AccessLevel accessLevel, bool orHigher = false)
    {
        AccessLevel userAccessLevel = AccessLevel.User;
        if (!AccessLevels.TryGetValue(userNickName.ToLowerInvariant(), out userAccessLevel))
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

    public static void AddUser(string userNickName, AccessLevel level)
    {
        AccessLevel userAccessLevel = AccessLevel.User;
        AccessLevels.TryGetValue(userNickName.ToLowerInvariant(), out userAccessLevel);
        userAccessLevel |= level;
        AccessLevels[userNickName.ToLowerInvariant()] = userAccessLevel;
    }

    public static void RemoveUser(string userNickName, AccessLevel level)
    {
        AccessLevel userAccessLevel = AccessLevel.User;
        AccessLevels.TryGetValue(userNickName.ToLowerInvariant(), out userAccessLevel);
        userAccessLevel &= ~level;
        AccessLevels[userNickName.ToLowerInvariant()] = userAccessLevel;
    }

}
