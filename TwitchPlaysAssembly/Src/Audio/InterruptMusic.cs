using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class InterruptMusic : MonoBehaviour
{
	public static InterruptMusic Instance { get; private set; }

	private static readonly FieldInfo VolumeLevelGameplayField = typeof(GameplayMusicController).GetField("volumeLevel", BindingFlags.NonPublic | BindingFlags.Instance);
	private readonly Dictionary<int, float> _oldVolumesGameplay = new Dictionary<int, float>();
	private static readonly FieldInfo VolumeLevelOtherField = typeof(MusicController).GetField("volumeLevel", BindingFlags.NonPublic | BindingFlags.Instance);
	private readonly Dictionary<int, float> _oldVolumesOther = new Dictionary<int, float>();

	private void Awake() => Instance = this;

	public void SetMusicInterrupt(bool enableInterrupt)
	{
		var gameplayMusicController = MusicManager.Instance.GameplayMusicController;
		int gameplayMusicControllerInstanceID = gameplayMusicController.GetInstanceID();
		if (enableInterrupt)
		{
			if (!_oldVolumesGameplay.ContainsKey(gameplayMusicControllerInstanceID))
				_oldVolumesGameplay[gameplayMusicControllerInstanceID] = (float) VolumeLevelGameplayField.GetValue(gameplayMusicController);
			gameplayMusicController.SetVolume(0.0f, true);
		}
		else
		{
			if (_oldVolumesGameplay.ContainsKey(gameplayMusicControllerInstanceID))
			{
				gameplayMusicController.SetVolume(_oldVolumesGameplay[gameplayMusicControllerInstanceID], true);
				_oldVolumesGameplay.Remove(gameplayMusicControllerInstanceID);
			}
		}

		var musicController = MusicManager.Instance.MusicController;
		int musicControllerInstanceID = musicController.GetInstanceID();
		if (enableInterrupt)
		{
			if (!_oldVolumesOther.ContainsKey(musicControllerInstanceID))
				_oldVolumesOther[musicControllerInstanceID] = (float) VolumeLevelOtherField.GetValue(musicController);
			musicController.SetVolume(0.0f, true);
		}
		else
		{
			if (_oldVolumesOther.ContainsKey(musicControllerInstanceID))
			{
				musicController.SetVolume(_oldVolumesOther[musicControllerInstanceID], true);
				_oldVolumesOther.Remove(musicControllerInstanceID);
			}
		}
	}
}
