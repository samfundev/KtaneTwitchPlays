using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class InterruptMusic : MonoBehaviour
{
    public static InterruptMusic Instance
    {
        get
        {
            return _instance;
        }
    }

    private static InterruptMusic _instance = null;

    private static Type _gameplayMusicControllerType = null;
    private static FieldInfo _volumeLevelField = null;
    private static MethodInfo _setVolumeMethod = null;
    private Dictionary<int, float> _oldVolumes = new Dictionary<int, float>();

    static InterruptMusic()
    {
        _gameplayMusicControllerType = ReflectionHelper.FindType("GameplayMusicController");
        if (_gameplayMusicControllerType != null)
        {
            _volumeLevelField = _gameplayMusicControllerType.GetField("volumeLevel", BindingFlags.NonPublic | BindingFlags.Instance);
            _setVolumeMethod = _gameplayMusicControllerType.GetMethod("SetVolume", BindingFlags.Public | BindingFlags.Instance);
        }
    }

    private void Awake()
    {
        _instance = this;
    }

    public void SetMusicInterrupt(bool enableInterrupt)
    {
        object[] gameplayMusicControllers = FindObjectsOfType(_gameplayMusicControllerType);
        foreach (object musicController in gameplayMusicControllers)
        {
            if (enableInterrupt)
            {
                _oldVolumes[((MonoBehaviour)musicController).GetInstanceID()] = (float)_volumeLevelField.GetValue(musicController);
                _setVolumeMethod.Invoke(musicController, new object[] { 0.0f, true });
            }
            else
            {
                _setVolumeMethod.Invoke(musicController, new object[] { _oldVolumes[((MonoBehaviour)musicController).GetInstanceID()], true });
            }
        }
    }
}
