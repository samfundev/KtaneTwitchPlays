using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;

public class ModuleInformation
{
    public string moduleDisplayName = string.Empty;
    public string moduleID;

    public int moduleScore = 5;
    public int strikePenalty = -6;
    public bool moduleScoreIsDynamic;

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

	public bool ShouldSerializeunclaimedColor() { return unclaimedColor != new Color(); }
	public bool ShouldSerializebuiltIntoTwitchPlays() {return false;}
    public bool ShouldSerializevalidCommands() {return !builtIntoTwitchPlays;}
    public bool ShouldSerializeDoesTheRightThing() { return !builtIntoTwitchPlays; }
    public bool ShouldSerializehelpTextOverride() { return !builtIntoTwitchPlays; }
    public bool ShouldSerializemanualCodeOverride() { return !builtIntoTwitchPlays; }
    public bool ShouldSerializestatusLightOverride() { return !builtIntoTwitchPlays; }
    public bool ShouldSerializevalidCommandsOverride() { return !builtIntoTwitchPlays; }
}

public static class ModuleData
{
    public static bool DataHasChanged = true;
    public static void WriteDataToFile()
    {
        if (!DataHasChanged) return;
        string path = Path.Combine(Application.persistentDataPath, usersSavePath);
        DebugHelper.Log("ModuleData: Writing file {0}", path);

        try
        {
            List<ModuleInformation> infoList = ComponentSolverFactory.GetModuleInformation().ToList();
            infoList = infoList.OrderBy(info => info.moduleDisplayName).ThenBy(info => info.moduleID).ToList();

			File.WriteAllText(path, SettingsConverter.Serialize(infoList));//JsonConvert.SerializeObject(infoList, Formatting.Indented));
        }
        catch (FileNotFoundException)
        {
            DebugHelper.LogWarning("ModuleData: File {0} was not found.", path);
            return;
        }
        catch (Exception ex)
        {
            DebugHelper.LogException(ex);
            return;
        }

        DataHasChanged = false;
        DebugHelper.Log("ModuleData: Writing of file {0} completed successfully", path);
    }

    public static bool LoadDataFromFile()
    {
        ModuleInformation[] modInfo;
        string path = Path.Combine(Application.persistentDataPath, usersSavePath);
        try
        {
            DebugHelper.Log("ModuleData: Loading Module information data from file: {0}", path);
			modInfo = SettingsConverter.Deserialize<ModuleInformation[]>(File.ReadAllText(path));//JsonConvert.DeserializeObject<ModuleInformation[]>(File.ReadAllText(path));
        }
        catch (FileNotFoundException)
        {
            DebugHelper.LogWarning("ModuleData: File {0} was not found.", path);
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
