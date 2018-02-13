using System;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;

public static class DebugHelper
{
	public static void Log(params object[] args)
	{
		Log(string.Join(" ", args.Select(x => x.ToString()).ToArray()));
	}

	[StringFormatMethod("format")]
	public static void Log(string format, params object[] args)
	{
		Debug.LogFormat("[TwitchPlays] " + format, args);
	}

	public static void LogWarning(params object[] args)
	{
		LogWarning(string.Join(" ", args.Select(x => x.ToString()).ToArray()));
	}

	[StringFormatMethod("format")]
	public static void LogWarning(string format, params object[] args)
	{
		Debug.LogWarningFormat("[TwitchPlays] " + format, args);
	}

	public static void LogError(params object[] args)
	{
		LogError(string.Join(" ", args.Select(x => x.ToString()).ToArray()));
	}

	[StringFormatMethod("format")]
	public static void LogError(string format, params object[] args)
	{
		Debug.LogErrorFormat("[TwitchPlays] " + format, args);
	}

	public static void LogException(Exception ex, string message = "An exception has occurred:")
	{
		Log(message);
		Debug.LogException(ex);
	}

	

	private static StringBuilder _treeBuilder;

	public static void PrintTree(Transform t, Type[] forbiddenTypes = null, bool printComponents = false, bool fromTop = false) => PrintTree(t, forbiddenTypes, printComponents, fromTop, 0);
	private static void PrintTree(Transform t, Type[] forbiddenTypes, bool printComponents, bool fromTop, int level)
	{
		if (level == 0)
		{
			_treeBuilder = new StringBuilder();
			if (fromTop)
			{
				while (t.parent != null)
					t = t.parent;
			}
		}

		string prefix = "";
		for (int i = 0; i < level; i++)
			prefix += "    ";

		_treeBuilder.Append($"{prefix}name = \"{t.name}\" - Active = {t.gameObject.activeInHierarchy}:{t.gameObject.activeSelf}\n");
		bool moveDown = forbiddenTypes == null || forbiddenTypes.ToList().TrueForAll(x => t.GetComponent(x) == null);
		if (moveDown)
		{
			_treeBuilder.Append($"{prefix} position = {Math.Round(t.localPosition.x, 7)},{Math.Round(t.localPosition.y, 7)},{Math.Round(t.localPosition.z, 7)}\n");
			_treeBuilder.Append($"{prefix} rotation = {Math.Round(t.localEulerAngles.x, 7)},{Math.Round(t.localEulerAngles.y, 7)},{Math.Round(t.localEulerAngles.z, 7)}\n");
			_treeBuilder.Append($"{prefix} scale = {Math.Round(t.localScale.x, 7)},{Math.Round(t.localScale.y, 7)},{Math.Round(t.localScale.z, 7)}\n");

			if (printComponents)
			{
				foreach (Component component in t.GetComponents<Component>())
				{
					if (component is Transform) continue;
					_treeBuilder.Append($"{prefix} Component: {component.GetType().FullName}\n");
				}
			}

		
			for (int i = 0; i < t.childCount; i++)
			{
				PrintTree(t.GetChild(i), forbiddenTypes, printComponents, false, level + 1);
			}
		}

		if (level == 0)
			Log(_treeBuilder.ToString());
	}

	public static void PrintParents(Transform t, bool printComponents = false) => PrintParents(t, 0, printComponents);
	private static void PrintParents(Transform t, int level, bool printComponents)
	{
		if (level == 0)
			_treeBuilder = new StringBuilder();

		string prefix = "";
		for (int i = 0; i < level; i++)
			prefix += "    ";

		_treeBuilder.Append($"{prefix}name = {t.name}\n");
		_treeBuilder.Append($"{prefix} position = {Math.Round(t.localPosition.x, 7)},{Math.Round(t.localPosition.y, 7)},{Math.Round(t.localPosition.z, 7)}\n");
		_treeBuilder.Append($"{prefix} rotation = {Math.Round(t.localEulerAngles.x, 7)},{Math.Round(t.localEulerAngles.y, 7)},{Math.Round(t.localEulerAngles.z, 7)}\n");
		_treeBuilder.Append($"{prefix} scale = {Math.Round(t.localScale.x, 7)},{Math.Round(t.localScale.y, 7)},{Math.Round(t.localScale.z, 7)}\n");

		if (printComponents)
		{
			foreach (Component component in t.GetComponents<Component>())
			{
				if (component is Transform) continue;
				_treeBuilder.Append($"{prefix} Component: {component.GetType().FullName}\n");
			}
		}

		if (t.parent != null)
		{
			PrintParents(t.parent, level + 1, printComponents);
		}

		if (level == 0)
			Log(_treeBuilder.ToString());
	}
}
