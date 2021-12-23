static class TwitchPlaysAPI
{
	public static void Setup()
	{
		ModdedAPI.AddProperty("Mode", () => OtherModes.currentMode, value => OtherModes.Set((TwitchPlaysMode) value));
		ModdedAPI.AddProperty("TimeMode", () => OtherModes.TimeModeOn, value => OtherModes.Set(TwitchPlaysMode.Time, (bool) value));
		ModdedAPI.AddProperty("ZenMode", () => OtherModes.Unexplodable, value => OtherModes.Set(TwitchPlaysMode.Zen, (bool) value));
		ModdedAPI.AddProperty("TimeModeStartingTime", () => TwitchPlaySettings.data.TimeModeStartingTime, value => TwitchPlaySettings.data.TimeModeStartingTime = (int) value);
		ModdedAPI.AddProperty("ExplodeBomb", null, value =>
		{
			if (value is string stringValue)
				TwitchGame.Instance.Bombs[0].CauseStrikesToExplosion(stringValue);
		});
	}
}