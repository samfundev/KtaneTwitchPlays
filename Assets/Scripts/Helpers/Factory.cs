using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class Factory
{
    static Factory()
    {
        _factoryType = ReflectionHelper.FindType("FactoryRoom");
        if (_factoryType == null)
        {
            return;
        }
        _currentBombID = _factoryType.GetField("_currentBombIndex", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    public static Type FactoryType()
    {
        if (_factoryType == null)
        {
            _factoryType = ReflectionHelper.FindType("FactoryRoom");
            if (_factoryType == null)
                return null;
            _currentBombID = _factoryType.GetField("_currentBombIndex", BindingFlags.NonPublic | BindingFlags.Instance);
        }
        return _factoryType;
    }

    public static Factory SetupFactory(UnityEngine.Object[] factoryObject)
    {
        return (factoryObject == null || factoryObject.Length == 0) ? null : new Factory {_factory = factoryObject[0]};
    }

    public int GetBombID()
    {
        return (int) _currentBombID.GetValue(_factory);
    }

    public static bool IsCurrentBomb(Factory factory, int bombID)
    {
        if (factory == null || bombID == -1)
            return true;
        return factory.GetBombID() == bombID;
    }

    public IEnumerator ReportBombStatus(List<TwitchBombHandle> bombHandles)
    {
        yield return new WaitUntil(() => GetBombID() > -1);
        while (GetBombID() < bombHandles.Count)
        {
            
            int currentBomb = GetBombID();
            IEnumerator showWindow = bombHandles[currentBomb].ShowMainUIWindow();
            while (showWindow.MoveNext())
            {
                yield return showWindow.Current;
            }

            yield return new WaitForSeconds(2.5f);
            bombHandles[currentBomb].ircConnection.SendMessage("Bomb {0} of {1} is now live.", currentBomb + 1, bombHandles.Count);
            if (bombHandles[currentBomb].edgeworkText.text != TwitchPlaySettings.data.BlankBombEdgework)
                bombHandles[currentBomb].ircConnection.SendMessage(TwitchPlaySettings.data.BombEdgework, bombHandles[currentBomb].edgeworkText.text);
            IEnumerator bombHold = bombHandles[currentBomb].OnMessageReceived("Bomb Factory", "red", string.Format("!bomb{0} hold",bombHandles.Count == 1 ? "" : (currentBomb + 1).ToString()));
            while (bombHold.MoveNext())
            {
                yield return bombHold.Current;
            }

            yield return new WaitUntil(() => GetBombID() != currentBomb);

            IEnumerator hideWindow = bombHandles[currentBomb].HideMainUIWindow();
            while (hideWindow.MoveNext())
            {
                yield return hideWindow.Current;
            }

        }
    }

    private static Type _factoryType = null;
    private static FieldInfo _currentBombID = null;

    private object _factory = null;
}
