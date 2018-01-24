using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

public class Factory : GameRoom
{
    public static Type FactoryType()
    {
        if (_factoryType != null) return _factoryType;

        _factoryType = ReflectionHelper.FindType("FactoryAssembly.FactoryRoom");
        if (_factoryType == null)
            return null;

		_factoryStaticModeType = ReflectionHelper.FindType("FactoryAssembly.StaticMode");
		_factoryFiniteModeType = ReflectionHelper.FindType("FactoryAssembly.FiniteSequenceMode");
		_currentBombField = _factoryFiniteModeType.GetField("_currentBomb", BindingFlags.NonPublic | BindingFlags.Instance);

	    _gameModeField = _factoryType.GetField("_gameMode", BindingFlags.NonPublic | BindingFlags.Instance);

        return _factoryType;
    }

    public static bool TrySetupFactory(UnityEngine.Object[] factoryObject, out GameRoom room)
    {
        if (factoryObject == null || factoryObject.Length == 0)
        {
            room = null;
            return false;
        }

	    room = new Factory(factoryObject[0]);
        return true;
    }

    private Factory(UnityEngine.Object roomObject)
    {
        DebugHelper.Log("Found gameplay room of type Factory Room");
        _factory = roomObject;
	    _gameroom = _gameModeField.GetValue(_factory);
	    if (_gameroom.GetType() == _factoryStaticModeType) return;
        BombID = -1;
        HoldBomb = false;
    }

    public override void RefreshBombID(ref int bombID)
    {
	    if (_gameroom.GetType() == _factoryStaticModeType)
	    {
		    base.RefreshBombID(ref bombID);
		    return;
	    }
		if (bombID == -1) return;
        bombID = BombID;
    }

	private UnityEngine.Object GetBomb => _gameroom.GetType() == _factoryFiniteModeType ? (UnityEngine.Object) _currentBombField.GetValue(_gameroom) : null;

	public override bool IsCurrentBomb(int bombID)
    {
	    if (_gameroom.GetType() == _factoryStaticModeType)
	    {
		    return base.IsCurrentBomb(bombID);
	    }

        if (bombID == -1)
            return true;
        return BombID== bombID;
    }

    public override void InitializeBombNames(List<TwitchBombHandle> bombHandles)
    {
	    if (_gameroom.GetType() == _factoryStaticModeType)
	    {
		    base.InitializeBombNames(bombHandles);
		    return;
	    }

		for (int i = 0; i < bombHandles.Count; i++)
        {
            bombHandles[i].nameText.text = $"Bomb {i + 1} of {bombHandles.Count}";
        }
    }

    public override IEnumerator ReportBombStatus(List<TwitchBombHandle> bombHandles)
    {
		if (_gameroom.GetType() == _factoryStaticModeType)
		{
			IEnumerator reportBombStatus = base.ReportBombStatus(bombHandles);
			while (reportBombStatus.MoveNext())
			{
				yield return reportBombStatus.Current;
			}
			yield break;
		}

		yield return new WaitUntil(() => GetBomb != null);
        BombID = 0;
        while (GetBomb != null)
        {
            UnityEngine.Object currentBomb = GetBomb;
            IEnumerator showWindow = bombHandles[BombID].ShowMainUIWindow();
            while (showWindow.MoveNext())
            {
                yield return showWindow.Current;
            }

            yield return new WaitForSeconds(3.0f);
            bombHandles[BombID].ircConnection.SendMessage("Bomb {0} of {1} is now live.", BombID + 1, bombHandles.Count);
            if (bombHandles[BombID].edgeworkText.text != TwitchPlaySettings.data.BlankBombEdgework)
                bombHandles[BombID].ircConnection.SendMessage(TwitchPlaySettings.data.BombEdgework, bombHandles[BombID].edgeworkText.text);
            IEnumerator bombHold = bombHandles[BombID].OnMessageReceived("Bomb Factory", "red", string.Format("bomb{0} hold",bombHandles.Count == 1 ? "" : (BombID + 1).ToString()));
            while (bombHold.MoveNext())
            {
                yield return bombHold.Current;
            }

            yield return new WaitUntil(() => currentBomb != GetBomb);

            IEnumerator hideWindow = bombHandles[BombID++].HideMainUIWindow();
            while (hideWindow.MoveNext())
            {
                yield return hideWindow.Current;
            }
        }
    }

    private static Type _factoryType = null;
	private static Type _factoryGameModeType = null;
	private static Type _factoryStaticModeType = null;
	private static Type _factoryFiniteModeType = null;

	private static FieldInfo _gameModeField = null;
	private static FieldInfo _currentBombField = null;

    private object _factory = null;
	private object _gameroom = null;
}
