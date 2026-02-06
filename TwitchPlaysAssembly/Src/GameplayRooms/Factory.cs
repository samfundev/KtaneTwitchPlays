using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class Factory : GameRoom
{
	private readonly bool _finiteMode;
	private readonly bool _infiniteMode;
	private readonly bool _zenMode = false; //For future use.

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
		_factoryDataType = ReflectionHelper.FindType("FactoryAssembly.FactoryRoomData");
		_destroyBombMethod = _factoryModeType.GetMethod("DestroyBomb", BindingFlags.NonPublic | BindingFlags.Instance);

		_factoryStaticModeType = ReflectionHelper.FindType("FactoryAssembly.StaticMode");
		_factoryFiniteModeType = ReflectionHelper.FindType("FactoryAssembly.FiniteSequenceMode");
		_factoryInfiniteModeType = ReflectionHelper.FindType("FactoryAssembly.InfiniteSequenceMode");
		_currentBombField = _factoryFiniteModeType.GetField("_currentBomb", BindingFlags.NonPublic | BindingFlags.Instance);
		_warningTimeField = _factoryDataType.GetField("WarningTime", BindingFlags.Public | BindingFlags.Instance);

		_gameModeProperty = _factoryType.GetProperty("GameMode", BindingFlags.NonPublic | BindingFlags.Instance);

		return _factoryType;
	}

	public static bool TrySetupFactory(Object[] factoryObject, out GameRoom room)
	{
		if (factoryObject == null || factoryObject.Length == 0)
		{
			room = null;
			return false;
		}

		room = new Factory(factoryObject[0]);
		return true;
	}

	private Factory(Object roomObject)
	{
		_gameroom = _gameModeProperty.GetValue(roomObject, new object[] { });
		_factoryRoom = Object.FindObjectOfType(_factoryType);
		if (_gameroom.GetType() == _factoryStaticModeType) return;

		_infiniteMode = _gameroom.GetType() == _factoryInfiniteModeType;
		_finiteMode = _gameroom.GetType() == _factoryFiniteModeType;
		BombID = -1;
		HoldBomb = false;
	}

	private Object GetBomb => _finiteMode || _infiniteMode ? (Object) _currentBombField.GetValue(_gameroom) : null;

	public override void InitializeBombs(List<Bomb> bombs)
	{
		if (_gameroom.GetType() == _factoryStaticModeType)
		{
			base.InitializeBombs(bombs);
			return;
		}

		TwitchGame.Instance.SetBombs(new List<Bomb> { bombs[0] });
		TwitchGame.Instance.InitializeModuleCodes();
		BombCount = bombs.Count;
	}

	public override IEnumerator InterruptLights()
	{
		if (!_factoryRoom) yield break;
		if (_factoryDataType == null || _warningTimeField == null) yield break;
		Object roomData = Object.FindObjectOfType(_factoryDataType);
		_warningTimeField.SetValue(roomData, OtherModes.Unexplodable ? 0 : 60);
	}

	public IEnumerator DestroyBomb(Object bomb)
	{
		yield return new WaitUntil(() => _infiniteMode || bomb == null || _internalBombProperty.GetValue(bomb, null) == null || (bool) _bombEndedProperty.GetValue(bomb, null));
		yield return new WaitForSeconds(0.1f);
		if (_infiniteMode || bomb == null || _internalBombProperty.GetValue(bomb, null) == null) yield break;
		_destroyBombMethod.Invoke(_gameroom, new object[] { bomb });
	}

	public override IEnumerator ReportBombStatus()
	{
		if (_gameroom.GetType() == _factoryStaticModeType)
		{
			IEnumerator baseIEnumerator = base.ReportBombStatus();
			while (baseIEnumerator.MoveNext()) yield return baseIEnumerator.Current;
			yield break;
		}
		InitializeOnLightsOn = false;

		TwitchBomb bombHandle = TwitchGame.Instance.Bombs[0];

		bombHandle.BombName = _infiniteMode ? "Infinite bombs incoming" : $"{BombCount} bombs incoming";

		yield return new WaitUntil(() => GetBomb != null || bombHandle.Bomb.HasDetonated);
		if (bombHandle.Bomb.HasDetonated && !_zenMode) yield break;

		float currentBombTimer = bombHandle.CurrentTimer + 5;
		int currentBombID = 1;
		while (GetBomb != null)
		{
			Object currentBomb = GetBomb;

			TimerComponent timerComponent = bombHandle.Bomb.GetTimer();
			yield return new WaitUntil(() => timerComponent.IsActive);

			if (Math.Abs(currentBombTimer - bombHandle.CurrentTimer) > 1f)
			{
				yield return null;
				InitializeGameModes(true);
			}

			bool enableCameraWall = OtherModes.TrainingModeOn && IRCConnection.Instance.State == IRCConnectionState.Connected && TwitchPlaySettings.data.EnableFactoryTrainingModeCameraWall;
			if (enableCameraWall != TwitchGame.ModuleCameras.CameraWallEnabled)
			{
				if (enableCameraWall)
					TwitchGame.ModuleCameras.EnableCameraWall();
				else
					TwitchGame.ModuleCameras.DisableCameraWall();
			}
			bombHandle.BombName = $"Bomb {currentBombID} of {(_infiniteMode ? "∞" : BombCount.ToString())}";
			IRCConnection.SendMessage($"Bomb {currentBombID++} of {(_infiniteMode ? "∞" : BombCount.ToString())} is now live.");

			if (TwitchPlaySettings.data.EnableAutomaticEdgework)
			{
				bombHandle.FillEdgework();
			}
			else
			{
				bombHandle.EdgeworkText.text = TwitchPlaySettings.data.BlankBombEdgework;
			}
			if (OtherModes.Unexplodable)
				bombHandle.StrikeLimit += bombHandle.StrikeCount;

			IEnumerator bombHold = bombHandle.HoldBomb();
			while (bombHold.MoveNext())
				yield return bombHold.Current;

			Bomb bomb1 = (Bomb) _internalBombProperty.GetValue(currentBomb, null);
			yield return new WaitUntil(() =>
			{
				bool result = bomb1.HasDetonated || bomb1.IsSolved() || !TwitchGame.BombActive;
				if (!result || OtherModes.TimeModeOn) currentBombTimer = bomb1.GetTimer().TimeRemaining;
				return result;
			});
			if (!TwitchGame.BombActive) yield break;

			// In between sequence bombs, award players who maintained modules on the previous bomb.
			TwitchGame.Instance.AwardMaintainedModules();

			IRCConnection.SendMessage(TwitchGame.Instance.GetBombResult(false));
			TwitchPlaySettings.SetRetryReward();

			foreach (TwitchModule handle in TwitchGame.Instance.Modules)
			{
				//If the camera is still attached to the bomb component when the bomb gets destroyed, then THAT camera is destroyed as well.
				TwitchGame.ModuleCameras.UnviewModule(handle);
			}

			if (TwitchPlaySettings.data.EnableFactoryAutomaticNextBomb)
			{
				bombHold = bombHandle.LetGoBomb();
				while (bombHold.MoveNext())
					yield return bombHold.Current;
				yield return new WaitForSeconds(1.0f);

				//If for some reason we are somehow still holding the bomb, then the Let go did not register.
				//Try again exactly one more time.
				if (currentBomb == GetBomb)
				{
					bombHold = bombHandle.HoldBomb();
					while (bombHold.MoveNext()) yield return bombHold.Current;
					yield return new WaitForSeconds(0.10f);

					bombHold = bombHandle.LetGoBomb();
					while (bombHold.MoveNext()) yield return bombHold.Current;
				}
			}

			//If we are still holding the bomb, wait for it to actually be put down manually, or by a Twitch plays Drop bomb command.
			while (currentBomb == GetBomb)
				yield return new WaitForSeconds(0.1f);

			bombHandle.StartCoroutine(DestroyBomb(currentBomb));

			if (GetBomb == null) continue;
			Bomb bomb = (Bomb) _internalBombProperty.GetValue(GetBomb, null);
			Object.Destroy(bombHandle);
			TwitchGame.Instance.Bombs.Clear();
			TwitchGame.Instance.DestroyComponentHandles();
			TwitchGame.Instance.SetBombs(new List<Bomb> { bomb });
			TwitchGame.Instance.InitializeModuleCodes();
			bombHandle = TwitchGame.Instance.Bombs[0];
		}
	}

	private static Type _factoryBombType;
	private static PropertyInfo _internalBombProperty;
	private static PropertyInfo _bombEndedProperty;

	private static Type _factoryType;
	private static Type _factoryModeType;
	private static Type _factoryDataType;
	private static MethodInfo _destroyBombMethod;

	private static Type _factoryStaticModeType;
	private static Type _factoryFiniteModeType;
	private static Type _factoryInfiniteModeType;

	private static PropertyInfo _gameModeProperty;
	private static FieldInfo _currentBombField;
	private static FieldInfo _warningTimeField;

	private readonly object _gameroom;
	private readonly UnityEngine.Object _factoryRoom;
}
