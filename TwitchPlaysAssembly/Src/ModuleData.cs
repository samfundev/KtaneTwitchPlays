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
	public bool moduleScoreIsDynamic;
	public bool announceModule;
	public bool unclaimable;

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

public class FileModuleInformation : ModuleInformation
{
	new public float? moduleScore;
	new public bool? moduleScoreIsDynamic;
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
	private static FileModuleInformation[] lastRead = new FileModuleInformation[0]; // Used to prevent overriding settings that are only controlled by the file.
	public static void WriteDataToFile()
	{
		if (!DataHasChanged || ComponentSolverFactory.SilentMode) return;
		string path = Path.Combine(Application.persistentDataPath, usersSavePath);
		DebugHelper.Log($"ModuleData: Writing file {path}");

		try
		{
			var infoList = ComponentSolverFactory
				.GetModuleInformation()
				.OrderBy(info => info.moduleDisplayName)
				.ThenBy(info => info.moduleID)
				.Select(info =>
				{
					var fileInfo = lastRead.FirstOrDefault(file => file.moduleID == info.moduleID);
					var dictionary = new Dictionary<string, object>();
					var type = info.GetType();
					foreach (var field in type.GetFields())
					{
						if (!(info.CallMethod<bool?>($"ShouldSerialize{field.Name}") ?? true))
							continue;

						var value = field.GetValue(info);
						if (field.Name == "moduleScore")
							value = fileInfo?.moduleScore;
						else if (field.Name == "moduleScoreIsDynamic")
							value = fileInfo?.moduleScoreIsDynamic;

						dictionary[field.Name] = value;
					}

					return dictionary;
				})
				.ToList();

			File.WriteAllText(path, SettingsConverter.Serialize(infoList));
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
		FileModuleInformation[] modInfo;
		string path = Path.Combine(Application.persistentDataPath, usersSavePath);
		try
		{
			DebugHelper.Log($"ModuleData: Loading Module information data from file: {path}");
			modInfo = SettingsConverter.Deserialize<FileModuleInformation[]>(File.ReadAllText(path));
			lastRead = modInfo;
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

		foreach (var fileInfo in modInfo)
		{
			var info = (ModuleInformation) fileInfo;
			if (fileInfo.moduleID != null)
			{
				var defaultInfo = ComponentSolverFactory.GetDefaultInformation(fileInfo.moduleID);
				if (fileInfo.moduleScore == null)
					info.moduleScore = defaultInfo.moduleScore;
				if (fileInfo.moduleScoreIsDynamic == null)
					info.moduleScoreIsDynamic = defaultInfo.moduleScoreIsDynamic;
			}

			ComponentSolverFactory.AddModuleInformation(info);
		}
		return true;
	}

	public static string usersSavePath = "ModuleInformation.json";
}
