using System;
using System.Linq;
using System.Collections;
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
                _freeplayCommander = new FreeplayCommander((MonoBehaviour)freeplayDevices[0]);
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

		if (!text.StartsWith("!")) return;
		text = text.Substring(1);

		string[] split = text.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		string textAfter = split.Skip(1).Join();
		switch (split[0])
		{
			case "binder":
				if ((TwitchPlaySettings.data.EnableMissionBinder && TwitchPlaySettings.data.EnableTwitchPlaysMode) || UserAccess.HasAccess(userNickName, AccessLevel.Admin, true))
				{
					_coroutineQueue.AddToQueue(_bombBinderCommander.RespondToCommand(userNickName, textAfter, null, _ircConnection));
				}
				else
				{
					_ircConnection.SendMessage(TwitchPlaySettings.data.MissionBinderDisabled, userNickName);
				}
				break;
			case "freeplay":
				if((TwitchPlaySettings.data.EnableFreeplayBriefcase && TwitchPlaySettings.data.EnableTwitchPlaysMode) || UserAccess.HasAccess(userNickName, AccessLevel.Admin, true))
				{
					_coroutineQueue.AddToQueue(_freeplayCommander.RespondToCommand(userNickName, textAfter, null, _ircConnection));
				}
				else
				{
					_ircConnection.SendMessage(TwitchPlaySettings.data.FreePlayDisabled, userNickName);
				}
				break;
		}
	}
    #endregion
}
