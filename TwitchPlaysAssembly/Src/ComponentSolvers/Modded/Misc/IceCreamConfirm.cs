using System;
using System.Collections;
using System.Reflection;
using Newtonsoft.Json;

public class IceCreamConfirm : ComponentSolver
{
	public IceCreamConfirm(BombCommander bombCommander, BombComponent bombComponent) :
	base(bombCommander, bombComponent)
	{
		_component = bombComponent.GetComponent(_componentType);
		string help = (string)_HelpMessageField.GetValue(_component);

		modInfo = ComponentSolverFactory.GetModuleInfo(GetModuleType(), help + " Check the opening hours with !{0} hours.");
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
			yield return !TwitchPlaySettings.data.ShowHours
				? "sendtochat Sorry, hours are currently unavailable. Enjoy your ice cream!"
				: $"sendtochat {(settings.openingTimeEnabled ? "We are open every other hour today." : "We're open all day today!")}";
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
		_HelpMessageField = _componentType.GetField("TwitchHelpMessage", BindingFlags.Public | BindingFlags.Instance);

	}
	class Settings
	{
#pragma warning disable 649
		public bool openingTimeEnabled;
#pragma warning restore 649
	}

	private static Type _componentType = null;
	private static MethodInfo _ProcessCommandMethod = null;
	private static FieldInfo _HelpMessageField = null;

	private object _component = null;
	private Settings settings;
}
