using System;
using System.Collections;
using Newtonsoft.Json;
using TwitchPlaysAssembly.ComponentSolvers.Modded.Shims;

public class IceCreamConfirm : ComponentSolverShim
{
	public IceCreamConfirm(BombCommander bombCommander, BombComponent bombComponent) : base(bombCommander, bombComponent, "iceCreamModule")
	{
		_settings = new Settings();
		if (!modInfo.helpTextOverride)
		{
			modInfo.helpText += " Check the opening hours with !{0} hours.";
			modInfo.helpTextOverride = true;
		}
		KMModSettings modSettings = bombComponent.GetComponent<KMModSettings>();
		try
		{
			_settings = JsonConvert.DeserializeObject<Settings>(modSettings.Settings);
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex, "Could not deserialize ice cream settings:");
			TwitchPlaySettings.data.ShowHours = false;
			TwitchPlaySettings.WriteDataToFile();
		}
	}

	protected override IEnumerator RespondToCommandShimmed(string inputCommand)
	{
		if (inputCommand.ToLowerInvariant().Trim().Equals("hours"))
		{
			yield return !TwitchPlaySettings.data.ShowHours
				? "sendtochat Sorry, hours are currently unavailable. Enjoy your ice cream!"
				: $"sendtochat {(_settings.OpeningTimeEnabled ? "We are open every other hour today." : "We're open all day today!")}";
		}
		else
		{
			IEnumerator command = RespondToCommandUnshimmed(inputCommand);
			while (command.MoveNext())
			{
				yield return command.Current;
			}
		}
	}

	private class Settings
	{
		public bool OpeningTimeEnabled = false;
	}

	private readonly Settings _settings;
}
