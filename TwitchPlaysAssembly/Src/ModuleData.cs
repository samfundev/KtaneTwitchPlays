using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ModuleInformation
{
	public string moduleDisplayName = string.Empty;
	public string moduleID;

	public bool moduleScoreOverride;
	public float moduleScore = 5;
	public bool strikePenaltyOverride;
	public int strikePenalty = -6;
	public bool moduleScoreIsDynamic;

	public ScoreMethod scoreMethod;

	public bool helpTextOverride;
	public string helpText;

	public bool manualCodeOverride;
	public string manualCode;

	public bool statusLightOverride;
	public bool statusLightLeft;
	public bool statusLightDown;

	public bool validCommandsOverride;
	public string[] validCommands;
	public bool DoesTheRightThing;

	public bool builtIntoTwitchPlays;

	public bool CameraPinningAlwaysAllowed;

	public Color unclaimedColor;

	public bool ShouldSerializescoreMethod() => scoreMethod != ScoreMethod.Default;
	public bool ShouldSerializeunclaimedColor() => unclaimedColor != new Color();
	public bool ShouldSerializebuiltIntoTwitchPlays() => false;
	public bool ShouldSerializevalidCommands() => !builtIntoTwitchPlays;
	public bool ShouldSerializeDoesTheRightThing() => !builtIntoTwitchPlays;
	public bool ShouldSerializevalidCommandsOverride() => !builtIntoTwitchPlays;

	public ModuleInformation Clone() => (ModuleInformation) MemberwiseClone();
}

public enum ScoreMethod
{
	Default,
	NeedyTime,
	NeedySolves,
}

public static class ModuleData
{
	public static bool DataHasChanged = true;
	public static void WriteDataToFile()
	{
		if (!DataHasChanged || ComponentSolverFactory.SilentMode) return;
		string path = Path.Combine(Application.persistentDataPath, usersSavePath);
		DebugHelper.Log($"ModuleData: Writing file {path}");

		try
		{
			List<ModuleInformation> infoList = ComponentSolverFactory.GetModuleInformation().ToList();
			infoList = infoList.OrderBy(info => info.moduleDisplayName).ThenBy(info => info.moduleID).ToList();

			File.WriteAllText(path, SettingsConverter.Serialize(infoList));//JsonConvert.SerializeObject(infoList, Formatting.Indented));
		}
		catch (FileNotFoundException)
		{
			DebugHelper.LogWarning($"ModuleData: File {path} was not found.");
			return;
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex);
			return;
		}

		DataHasChanged = false;
		DebugHelper.Log($"ModuleData: Writing of file {path} completed successfully.");
	}

	public static bool LoadDataFromFile()
	{
		ModuleInformation[] modInfo;
		string path = Path.Combine(Application.persistentDataPath, usersSavePath);
		try
		{
			DebugHelper.Log($"ModuleData: Loading Module information data from file: {path}");
			modInfo = SettingsConverter.Deserialize<ModuleInformation[]>(File.ReadAllText(path));//JsonConvert.DeserializeObject<ModuleInformation[]>(File.ReadAllText(path));
		}
		catch (FileNotFoundException)
		{
			DebugHelper.LogWarning($"ModuleData: File {path} was not found.");
			return false;
		}
		catch (Exception ex)
		{
			DebugHelper.LogException(ex);
			return false;
		}

		foreach (ModuleInformation info in modInfo)
		{
			ComponentSolverFactory.AddModuleInformation(info);
		}
		return true;
	}

	public static string usersSavePath = "ModuleInformation.json";
}
