using System;

public class TwitchPlaysProperties : PropertiesBehaviour
{
	public TwitchPlaysProperties()
	{
		AddProperty("TimeMode", new Property(() => OtherModes.timedModeOn, x => { OtherModes.timedModeOn = (bool)x; }));
		AddProperty("TimeModeTimeLimit", new Property(() => TwitchPlaySettings.data.TimeModeStartingTime, null));
		AddProperty("ircConnectionSendMessage", new Property(null, x => IRCConnection.Instance.SendMessage((string)x)));
		AddProperty("Reward", new Property(() => TwitchPlaySettings.GetRewardBonus(), x => TwitchPlaySettings.SetRewardBonus((int) x)));
	}

	internal TwitchPlaysService TwitchPlaysService { get; set; }
}
