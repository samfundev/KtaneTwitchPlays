using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class Factory
{
    public static Type FactoryType()
    {
        if (_factoryType != null) return _factoryType;

        _factoryType = ReflectionHelper.FindType("FactoryAssembly.FactoryRoom");
        if (_factoryType == null)
            return null;
        _currentBombField = _factoryType.GetField("_currentBomb", BindingFlags.NonPublic | BindingFlags.Instance);

        return _factoryType;
    }

    public static Factory SetupFactory(UnityEngine.Object[] factoryObject)
    {
        return (factoryObject == null || factoryObject.Length == 0) ? null : new Factory {_factory = factoryObject[0], BombID = -1};
    }

    public int BombID { get; private set; }

    private UnityEngine.Object GetBomb
    {
        get { return (UnityEngine.Object) _currentBombField.GetValue(_factory); }
    }

    public static bool IsCurrentBomb(Factory factory, int bombID)
    {
        if (factory == null || bombID == -1)
            return true;
        return factory.BombID== bombID;
    }

    public IEnumerator ReportBombStatus(List<TwitchBombHandle> bombHandles)
    {
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
            IEnumerator bombHold = bombHandles[BombID].OnMessageReceived("Bomb Factory", "red", string.Format("!bomb{0} hold",bombHandles.Count == 1 ? "" : (BombID + 1).ToString()));
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
    private static FieldInfo _currentBombField = null;

    private object _factory = null;
}
