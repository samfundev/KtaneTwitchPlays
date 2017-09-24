using System;
using System.Collections.Generic;

[Flags()]
public enum AccessLevel
{
    SuperUser = 0x80,
    Admin = 0x40,
    Mod = 0x20,

    User = 0x00
}

public static class UserAccess
{ 
    private static readonly Dictionary<string, AccessLevel> AccessLevels = new Dictionary<string, AccessLevel>();

    static UserAccess()
    {
        /*
         * Enter here the list of special user roles, giving them bitwise enum flags to determine the level of access each user has.
         * 
         * The access level enum can be extended further per your requirements.
         * 
         * Use the helper method below to determine if the user has access for a particular access level or not.
         * TODO: Extend this to a JSON-serializable type, and/or inspect the decorated PRIVMSG lines from IRC to infer moderator status from the Twitch Chat moderator flag.
         */

        //AccessLevels["UserNickName"] = AccessLevel.SuperUser | AccessLevel.Admin | AccessLevel.Mod;
        //AccessLevels["UserNickName"] = AccessLevel.Mod;
    }

    public static bool HasAccess(string userNickName, AccessLevel accessLevel)
    {
        AccessLevel userAccessLevel = AccessLevel.User;
        return AccessLevels.TryGetValue(userNickName, out userAccessLevel) && (accessLevel & userAccessLevel) == accessLevel;
    }
}
