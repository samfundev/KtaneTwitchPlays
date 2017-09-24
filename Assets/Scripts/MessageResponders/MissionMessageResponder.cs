using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

public class MissionMessageResponder : MessageResponder
{
    private BombBinderCommander _bombBinderCommander = null;
    private FreeplayCommander _freeplayCommander = null;

    #region Unity Lifecycle
    private void OnEnable()
    {
        // InputInterceptor.DisableInput();

        StartCoroutine(CheckForBombBinderAndFreeplayDevice());
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        _bombBinderCommander = null;
        _freeplayCommander = null;
    }
    #endregion

    #region Protected/Private Methods
    private IEnumerator CheckForBombBinderAndFreeplayDevice()
    {
        yield return null;

        while (true)
        {
            UnityEngine.Object[] bombBinders = FindObjectsOfType(CommonReflectedTypeInfo.BombBinderType);
            if (bombBinders != null && bombBinders.Length > 0)
            {
                _bombBinderCommander = new BombBinderCommander((MonoBehaviour)bombBinders[0]);
                break;
            }

            yield return null;
        }

        while (true)
        {
            UnityEngine.Object[] freeplayDevices = FindObjectsOfType(CommonReflectedTypeInfo.FreeplayDeviceType);
            if (freeplayDevices != null && freeplayDevices.Length > 0)
            {
                UnityEngine.Object[] objects = FindObjectsOfType(ReflectionHelper.FindType("MultipleBombsAssembly.MultipleBombs"));
                MonoBehaviour multipleBombs = (objects == null || objects.Length == 0) ? null : (MonoBehaviour)objects[0];

                _freeplayCommander = new FreeplayCommander((MonoBehaviour)freeplayDevices[0],multipleBombs);
                break;
            }

            yield return null;
        }
    }

    protected override void OnMessageReceived(string userNickName, string userColorCode, string text)
    {
        if (_bombBinderCommander == null)
        {
            return;
        }

        Match binderMatch = Regex.Match(text, "^!binder (.+)", RegexOptions.IgnoreCase);
        if (binderMatch.Success)
        {
            _coroutineQueue.AddToQueue(_bombBinderCommander.RespondToCommand(userNickName, binderMatch.Groups[1].Value, null));
        }

        Match freeplayMatch = Regex.Match(text, "^!freeplay (.+)", RegexOptions.IgnoreCase);
        if (freeplayMatch.Success)
        {
            _coroutineQueue.AddToQueue(_freeplayCommander.RespondToCommand(userNickName, freeplayMatch.Groups[1].Value, null));
        }
    }
    #endregion
}
