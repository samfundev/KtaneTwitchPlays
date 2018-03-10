using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Newtonsoft.Json;

public class IceCreamConfirm : ComponentSolver
{
	public IceCreamConfirm(BombCommander bombCommander, BombComponent bombComponent) :
	base(bombCommander, bombComponent)
	{
		_component = bombComponent.GetComponent(_componentType);
		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType());
		KMModSettings modSettings = bombComponent.GetComponent<KMModSettings>();
		try
		{
			settings = JsonConvert.DeserializeObject<Settings>(modSettings.Settings);
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
			yield return null;
			if (!TwitchPlaySettings.data.ShowHours)
			{
				yield return $"sendtochat Sorry, hours are currently unavailable. Enjoy your ice cream!";
				yield break;
			}
			string hours = settings.openingTimeEnabled ? "We are open every other hour today." : "We're open all day today!";
			yield return $"sendtochat {hours}";
		}
		else
		{
			IEnumerator command = (IEnumerator)_ProcessCommandMethod.Invoke(_component, new object[] { inputCommand });
			if (command == null) yield break;
			while (command.MoveNext())
			{
				yield return command.Current;
			}
		}
	}
	static IceCreamConfirm()
	{
		_componentType = ReflectionHelper.FindType("IceCreamModule");
		_ProcessCommandMethod = _componentType.GetMethod("ProcessTwitchCommand", BindingFlags.Public | BindingFlags.Instance);
	}
	class Settings
	{
		public bool openingTimeEnabled;
	}

	private static Type _componentType = null;
	private static MethodInfo _ProcessCommandMethod = null;

	private object _component = null;
	private Settings settings;
}