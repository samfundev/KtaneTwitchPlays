using System;
using System.Linq;
using UnityEngine;

public static class DebugHelper
{
	public static void Log(params object[] args)
	{
		Log(string.Join(" ", args.Select(x => x.ToString()).ToArray()));
	}

	public static void Log(string format, params object[] args)
	{
		Debug.LogFormat("[TwitchPlays] " + format, args);
	}

	public static void LogWarning(params object[] args)
	{
		LogWarning(string.Join(" ", args.Select(x => x.ToString()).ToArray()));
	}

	public static void LogWarning(string format, params object[] args)
	{
		Debug.LogWarningFormat("[TwitchPlays] " + format, args);
	}

	public static void LogError(params object[] args)
	{
		LogError(string.Join(" ", args.Select(x => x.ToString()).ToArray()));
	}

	public static void LogError(string format, params object[] args)
	{
		Debug.LogErrorFormat("[TwitchPlays] " + format, args);
	}

	public static void LogException(Exception ex, string message = "An exception has occurred:")
	{
		Log(message);
		Debug.LogException(ex);
	}
}
