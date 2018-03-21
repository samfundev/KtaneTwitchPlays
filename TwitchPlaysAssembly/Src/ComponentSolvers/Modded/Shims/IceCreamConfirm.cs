using System;
using System.Collections;
using Newtonsoft.Json;
using TwitchPlaysAssembly.ComponentSolvers.Modded.Shims;

public class IceCreamConfirm : ComponentSolverShim
{
	public IceCreamConfirm(BombCommander bombCommander, BombComponent bombComponent) :
	base(bombCommander, bombComponent)
	{
		_settings = new Settings();
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), ShimData.HelpMessage + " Check the opening hours with !{0} hours.");
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

	protected override IEnumerator RespondToCommandInternal(string inputCommand)
	{
		if (inputCommand.ToLowerInvariant().Equals("hours"))
		{
			yield return !TwitchPlaySettings.data.ShowHours
				? "sendtochat Sorry, hours are currently unavailable. Enjoy your ice cream!"
				: $"sendtochat {(_settings.OpeningTimeEnabled ? "We are open every other hour today." : "We're open all day today!")}";
		}
		else
		{
			IEnumerator command = base.RespondToCommandInternal(inputCommand);
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
