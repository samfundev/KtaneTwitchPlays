using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultipleBombs
{
	private static GameObject _gameObject;

	private static IDictionary<string, object> Properties
	{
		get
		{
			return _gameObject == null
				? null
				: _gameObject.GetComponent<IDictionary<string, object>>();
		}
	}

	public static IEnumerator Refresh()
	{
		_gameObject = GameObject.Find("MultipleBombs_Info");
		for (var i = 0; i < 120 && _gameObject == null; i++)
		{
			yield return null;
			_gameObject = GameObject.Find("MultipleBombs_Info");
		}
	}

	public static bool Installed()
	{
		return _gameObject != null;
	}

	public static int GetMaximumBombCount()
	{
		return (Properties != null && Properties.TryGetValue(MaxBombCount, out object count))
			? (int) count
			: 1;
	}

	public static int GetFreePlayBombCount()
	{
		return (Properties != null && Properties.TryGetValue(BombCount, out object count))
			? (int) count
			: 2;
	}

	private const string MaxBombCount = "CurrentMaximumBombCount";
	private const string BombCount = "CurrentFreePlayBombCount";
}
