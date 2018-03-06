using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class Factory : GameRoom
{
	private bool _finiteMode = false;
	private bool _infiniteMode = false;
	private bool _zenMode = false;	//For future use.

    public static Type FactoryType()
    {
        if (_factoryType != null) return _factoryType;

        _factoryType = ReflectionHelper.FindType("FactoryAssembly.FactoryRoom");
        if (_factoryType == null)
            return null;

	    _factoryBombType = ReflectionHelper.FindType("FactoryAssembly.FactoryBomb");
	    _internalBombProperty = _factoryBombType.GetProperty("InternalBomb", BindingFlags.NonPublic | BindingFlags.Instance);
	    _bombEndedProperty = _factoryBombType.GetProperty("Ended", BindingFlags.NonPublic | BindingFlags.Instance);

	    _factoryModeType = ReflectionHelper.FindType("FactoryAssembly.FactoryGameMode");
	    _destroyBombMethod = _factoryModeType.GetMethod("DestroyBomb", BindingFlags.NonPublic | BindingFlags.Instance);
		
		_factoryStaticModeType = ReflectionHelper.FindType("FactoryAssembly.StaticMode");
		_factoryFiniteModeType = ReflectionHelper.FindType("FactoryAssembly.FiniteSequenceMode");
		_factoryInfiniteModeType = ReflectionHelper.FindType("FactoryAssembly.InfiniteSequenceMode");
		_currentBombField = _factoryFiniteModeType.GetField("_currentBomb", BindingFlags.NonPublic | BindingFlags.Instance);

	    _gameModeProperty = _factoryType.GetProperty("GameMode", BindingFlags.NonPublic | BindingFlags.Instance);

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
		_gameroom = _gameModeProperty.GetValue(_factory, new object[] {});
		if (_gameroom.GetType() == _factoryStaticModeType) return;

		_infiniteMode = _gameroom.GetType() == _factoryInfiniteModeType;
		_finiteMode = _gameroom.GetType() == _factoryFiniteModeType;
		BombID = -1;
		HoldBomb = false;
	}

	private UnityEngine.Object GetBomb => (_finiteMode || _infiniteMode)  ? (UnityEngine.Object) _currentBombField.GetValue(_gameroom) : null;

	public override void InitializeBombs(List<Bomb> bombs)
	{
		if (_gameroom.GetType() == _factoryStaticModeType)
		{
			base.InitializeBombs(bombs);
			return;
		}

		ReuseBombCommander = true;
		BombMessageResponder.Instance.SetBomb(bombs[0], -1);
		BombCount = bombs.Count;
	}

	public IEnumerator DestroyBomb(UnityEngine.Object bomb)
	{
		yield return new WaitUntil(() => _infiniteMode || bomb == null || _internalBombProperty.GetValue(bomb, null) == null || (bool) _bombEndedProperty.GetValue(bomb, null));
		yield return new WaitForSeconds(0.1f);
		if (_infiniteMode || bomb == null || _internalBombProperty.GetValue(bomb, null) == null) yield break;
		_destroyBombMethod.Invoke(_gameroom, new object[] {bomb});
	}

    public override IEnumerator ReportBombStatus()
    {
	    IEnumerator baseIEnumerator;
	    if (_gameroom.GetType() == _factoryStaticModeType)
	    {
		    baseIEnumerator = base.ReportBombStatus();
			while (baseIEnumerator.MoveNext()) yield return baseIEnumerator.Current;
		    yield break;
	    }

		TwitchBombHandle bombHandle = BombMessageResponder.Instance.BombHandles[0];
		
	    bombHandle.nameText.text = _infiniteMode ? "Infinite bombs incoming" : $"{BombCount} bombs incoming";

		yield return new WaitUntil(() => GetBomb != null || bombHandle.bombCommander.Bomb.HasDetonated);
	    if (bombHandle.bombCommander.Bomb.HasDetonated && !_zenMode) yield break;

	    float currentBombTimer = bombHandle.bombCommander.timerComponent.TimeRemaining + 5;
	    int currentBombID = 1;
	    while (GetBomb != null)
        {
	        int reward = TwitchPlaySettings.GetRewardBonus();
			UnityEngine.Object currentBomb = GetBomb;

			yield return new WaitForSeconds(3.0f);
	        bombHandle.nameText.text = $"Bomb {currentBombID}  of {(_infiniteMode ? "∞" : BombCount.ToString())}";
	        IRCConnection.Instance.SendMessage("Bomb {0} of {1} is now live.", currentBombID++ , _infiniteMode ? "∞" : BombCount.ToString());
	        if (Math.Abs(currentBombTimer - bombHandle.bombCommander.timerComponent.TimeRemaining) > 1f)
	        {
		        baseIEnumerator = base.ReportBombStatus();
		        while (baseIEnumerator.MoveNext()) yield return baseIEnumerator.Current;
			}
	        if (TwitchPlaySettings.data.EnableAutomaticEdgework)
	        {
		        bombHandle.bombCommander.FillEdgework();
	        }
	        else
	        {
		        bombHandle.edgeworkText.text = TwitchPlaySettings.data.BlankBombEdgework;
	        }
	        
			IEnumerator bombHold = bombHandle.OnMessageReceived("Bomb Factory", "red", "bomb hold");
            while (bombHold.MoveNext())
            {
                yield return bombHold.Current;
            }

	        Bomb bomb1 = (Bomb)_internalBombProperty.GetValue(currentBomb, null);
	        yield return new WaitUntil(() =>
	        {
		        bool result = bomb1.HasDetonated || bomb1.IsSolved() || !BombMessageResponder.BombActive;
		        if (!result) currentBombTimer = bomb1.GetTimer().TimeRemaining;
				return result;
	        });
	        if (!BombMessageResponder.BombActive) yield break;

	        IRCConnection.Instance.SendMessage(BombMessageResponder.Instance.GetBombResult(false));
			TwitchPlaySettings.SetRewardBonus(reward);

	        bombHold = bombHandle.OnMessageReceived("Bomb Factory", "red", "bomb drop");
	        while (bombHold.MoveNext())
	        {
		        yield return bombHold.Current;
	        }

			yield return new WaitUntil(() => currentBomb != GetBomb);
	        foreach (TwitchComponentHandle handle in BombMessageResponder.Instance.ComponentHandles)
	        {
				//If the camera is still attached to the bomb component when the bomb gets destroyed, then THAT camera is destroyed as wel.
		        BombMessageResponder.moduleCameras.DetachFromModule(handle.bombComponent);
	        }

			bombHandle.StartCoroutine(DestroyBomb(currentBomb));

	        if (GetBomb == null) continue;
	        Bomb bomb = (Bomb)_internalBombProperty.GetValue(GetBomb, null);
	        InitializeBomb(bomb);
        }
    }

	private static Type _factoryBombType = null;
	private static PropertyInfo _internalBombProperty = null;
	private static PropertyInfo _bombEndedProperty = null;

    private static Type _factoryType = null;
	private static Type _factoryModeType = null;
	private static MethodInfo _destroyBombMethod = null;

	private static Type _factoryStaticModeType = null;
	private static Type _factoryFiniteModeType = null;
	private static Type _factoryInfiniteModeType = null;

	private static PropertyInfo _gameModeProperty = null;
	private static FieldInfo _currentBombField = null;

    private object _factory = null;
	private object _gameroom = null;
}
