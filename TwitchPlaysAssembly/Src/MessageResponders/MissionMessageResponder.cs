using System;
using System.Linq;
using System.Collections;

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

        _bombBinderCommander = BombBinderCommander.Instance = null;
        _freeplayCommander = FreeplayCommander.Instance = null;
    }
    #endregion

    #region Protected/Private Methods
    private IEnumerator CheckForBombBinderAndFreeplayDevice()
    {
        yield return null;

	    SetupRoom setupRoom = (SetupRoom)SceneManager.Instance.CurrentRoom;
	    _bombBinderCommander = new BombBinderCommander(setupRoom.BombBinder);
	    _freeplayCommander = new FreeplayCommander(setupRoom.FreeplayDevice);
    }

	

	protected override void OnMessageReceived(string userNickName, string userColorCode, string text)
	{
		if (_bombBinderCommander == null)
		{
			return;
		}

		if (!text.StartsWith("!") || text.Equals("!")) return;
		text = text.Substring(1);

		string[] split = text.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		string textAfter = split.Skip(1).Join();
		switch (split[0])
		{
			case "binder":
				if ((TwitchPlaySettings.data.EnableMissionBinder && TwitchPlaySettings.data.EnableTwitchPlaysMode) || UserAccess.HasAccess(userNickName, AccessLevel.Admin, true))
				{
					_coroutineQueue.AddToQueue(_bombBinderCommander.RespondToCommand(userNickName, textAfter, null));
				}
				else
				{
					IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.MissionBinderDisabled, userNickName);
				}
				break;
			case "freeplay":
				if((TwitchPlaySettings.data.EnableFreeplayBriefcase && TwitchPlaySettings.data.EnableTwitchPlaysMode) || UserAccess.HasAccess(userNickName, AccessLevel.Admin, true))
				{
					_coroutineQueue.AddToQueue(_freeplayCommander.RespondToCommand(userNickName, textAfter, null));
				}
				else
				{
					IRCConnection.Instance.SendMessage(TwitchPlaySettings.data.FreePlayDisabled, userNickName);
				}
				break;
			default:
				_coroutineQueue.AddToQueue(TPElevatorSwitch.Instance.ProcessElevatorCommand(split));
				break;
		}
	}
    #endregion
}
