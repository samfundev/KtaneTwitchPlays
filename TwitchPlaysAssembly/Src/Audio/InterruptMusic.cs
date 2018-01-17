using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class InterruptMusic : MonoBehaviour
{
    public static InterruptMusic Instance => _instance;

	private static InterruptMusic _instance = null;
    private static FieldInfo _volumeLevelField = null;
    private Dictionary<int, float> _oldVolumes = new Dictionary<int, float>();

    static InterruptMusic()
    {
        _volumeLevelField = typeof(GameplayMusicController).GetField("volumeLevel", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private void Awake()
    {
        _instance = this;
    }

    public void SetMusicInterrupt(bool enableInterrupt)
    {
        GameplayMusicController[] gameplayMusicControllers = FindObjectsOfType<GameplayMusicController>();
        foreach (GameplayMusicController musicController in gameplayMusicControllers)
        {
            int musicControllerInstanceID = musicController.GetInstanceID();
            if (enableInterrupt)
            {
                _oldVolumes[musicControllerInstanceID] = (float)_volumeLevelField.GetValue(musicController);
                musicController.SetVolume(0.0f, true);
            }
            else
            {
                if (_oldVolumes.ContainsKey(musicControllerInstanceID))
                    musicController.SetVolume(_oldVolumes[musicControllerInstanceID], true);
            }
        }
    }
}
