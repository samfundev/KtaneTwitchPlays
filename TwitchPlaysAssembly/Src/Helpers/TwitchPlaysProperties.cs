public class TwitchPlaysProperties : PropertiesBehaviour
{
	public TwitchPlaysProperties()
	{
		AddProperty("TimeMode", new Property(() => OtherModes.TimeModeOn, x => OtherModes.TimeModeOn = (bool) x));
		AddProperty("ZenMode", new Property(() => OtherModes.ZenModeOn, x => OtherModes.ZenModeOn = (bool) x));
		AddProperty("TimeModeTimeLimit", new Property(() => TwitchPlaySettings.data.TimeModeStartingTime, SetTimeModeTimeLimit));
		AddProperty("ircConnectionSendMessage", new Property(null, x => IRCConnection.SendMessage((string) x)));
		AddProperty("Reward", new Property(() => TwitchPlaySettings.GetRewardBonus(), x => TwitchPlaySettings.SetRewardBonus((int) x)));
		AddProperty("CauseFakeStrike", new Property(null, CauseFakeStrike));
	}

	private static void SetTimeModeTimeLimit(object timeLimit)
	{
		if (!(timeLimit is int intTimeLimit)) return;
		TwitchPlaySettings.data.TimeModeStartingTime = intTimeLimit;
		TwitchPlaySettings.WriteDataToFile();
	}

	private static void CauseFakeStrike(object kmBombModule)
	{
		BombComponent component;
		switch (kmBombModule)
		{
			case KMBombModule module:
				component = module.GetComponentInChildren<BombComponent>();
				break;
			case KMNeedyModule needyModule:
				component = needyModule.GetComponentInChildren<BombComponent>();
				break;
			default:
				return;
		}
		if (component == null) return;

		foreach (TwitchComponentHandle handle in BombMessageResponder.Instance.ComponentHandles)
		{
			if (handle.bombComponent != component) continue;
			handle.Solver.OnFakeStrike();
			break;
		}
	}

	internal TwitchPlaysService TwitchPlaysService { get; set; }
}
