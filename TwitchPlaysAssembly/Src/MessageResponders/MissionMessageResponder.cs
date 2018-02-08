using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class MissionMessageResponder : MessageResponder
{
    private BombBinderCommander _bombBinderCommander = null;
    private FreeplayCommander _freeplayCommander = null;
	private List<KMHoldableCommander> _holdableCommanders = null;

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
	    _holdableCommanders = null;
    }
    #endregion

    #region Protected/Private Methods
    private IEnumerator CheckForBombBinderAndFreeplayDevice()
    {
        yield return null;

        while (true)
        {
            BombBinder[] bombBinders = FindObjectsOfType<BombBinder>();
            if (bombBinders != null && bombBinders.Length > 0)
            {
                _bombBinderCommander = new BombBinderCommander(bombBinders[0]);
                break;
            }

            yield return null;
        }

        while (true)
        {
            FreeplayDevice[] freeplayDevices = FindObjectsOfType<FreeplayDevice>();
            if (freeplayDevices != null && freeplayDevices.Length > 0)
            {
                _freeplayCommander = new FreeplayCommander(freeplayDevices[0]);
                break;
            }

            yield return null;
        }

	    while (true)
	    {
		    _holdableCommanders = new List<KMHoldableCommander>();
		    string[] blacklistedHoldables =
		    {
			    "FreeplayDevice", "BombBinder"
		    };

		    FloatingHoldable[] holdables = FindObjectsOfType<FloatingHoldable>();
		    if (holdables != null)
		    {
			    foreach (FloatingHoldable holdable in holdables)
			    {
				    if (blacklistedHoldables.Contains(holdable.name.Replace("(Clone)", ""))) continue;
				    try
				    {
						DebugHelper.Log($"Creating holdable handler for {holdable.name}");
					    KMHoldableCommander holdableCommander = new KMHoldableCommander(holdable);
					    _holdableCommanders.Add(holdableCommander);
				    }
				    catch (Exception ex)
				    {
						DebugHelper.LogException(ex, $"Could not create a handler for holdable {holdable.name} due to an exception:");
				    }
			    }
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
				foreach (KMHoldableCommander commander in _holdableCommanders)
				{
					if (string.IsNullOrEmpty(commander?.ID) || !commander.ID.Equals(split[0])) continue;
					if (textAfter.EqualsAny("help", "manual"))
					{
						commander.Handler.ShowHelp();
						break;
					}
					_coroutineQueue.AddToQueue(commander.RespondToCommand(userNickName, textAfter));
					break;
				}
				break;
		}
	}
    #endregion
}
