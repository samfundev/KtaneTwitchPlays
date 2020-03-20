using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Assets.Scripts.Mods;
using UnityEngine;
using UnityEngine.Networking;

static class Updater
{
	public static bool UpdateAvailable;
	public static DateTime LastCheck;

	static readonly string BuildStorage = Application.temporaryCachePath;

	public static IEnumerator CheckForUpdates()
	{
		if (UpdateAvailable) yield break;

		LastCheck = DateTime.Now;

		// Download the build
		UnityWebRequest request = UnityWebRequest.Get("https://www.dropbox.com/s/30vsm4a1p9yps70/Twitch%20Plays.zip?dl=1");

		yield return request.SendWebRequest();

		// Delete any previously downloaded copy
		DirectoryInfo buildFolder = new DirectoryInfo(Path.Combine(BuildStorage, "Twitch Plays"));
		if (buildFolder.Exists) buildFolder.Delete(true);

		// Extract the zip file which should only contain one folder, "Twitch Plays".
		using (ZipStorer zip = ZipStorer.Open(new MemoryStream(request.downloadHandler.data), FileAccess.Read))
		{
			foreach (ZipStorer.ZipFileEntry entry in zip.ReadCentralDir())
			{
				zip.ExtractFile(entry, Path.Combine(BuildStorage, entry.FilenameInZip));
			}
		}

		string dllPath = new string[] { BuildStorage, "Twitch Plays", "TwitchPlaysAssembly.dll" }.Aggregate(Path.Combine);

		// Check to see if the build is actually newer (and if the dll actually exists).
		UpdateAvailable = File.Exists(dllPath) && GetBuildDateTime(dllPath) > GetCurrentBuildDateTime();

		// Delete the build if it's old, so we don't just leave stuff around in the temporary directory.
		if (!UpdateAvailable) buildFolder.Delete(true);
	}

	public static IEnumerator Update()
	{
		if (!UpdateAvailable) yield return CheckForUpdates();

		if (!UpdateAvailable)
		{
			IRCConnection.SendMessage("Twitch Plays is up-to-date.");
			yield break;
		}

		IRCConnection.SendMessage("Updating Twitch Plays and restarting.");

		// Make the current build the previous build
		var previousBuild = new DirectoryInfo(Path.Combine(TwitchPlaysService.DataFolder, "Previous Build"));
		if (previousBuild.Exists)
			previousBuild.Delete(true);

		DirectoryInfo modFolder = new DirectoryInfo(GetModFolder());
		modFolder.MoveToSafe(previousBuild);

		// Copy the build that CheckForUpdates() downloaded to where the current one was.
		DirectoryInfo buildFolder = new DirectoryInfo(Path.Combine(BuildStorage, "Twitch Plays"));
		buildFolder.MoveToSafe(modFolder);

		GlobalCommands.RestartGame();
	}

	public static IEnumerator Revert()
	{
		var previousBuild = new DirectoryInfo(Path.Combine(TwitchPlaysService.DataFolder, "Previous Build"));
		if (!previousBuild.Exists)
		{
			IRCConnection.SendMessage("There is no previous version of Twitch Plays to revert to.");
			yield break;
		}

		IRCConnection.SendMessage("Reverting to the previous Twitch Plays build and restarting.");

		DirectoryInfo modFolder = new DirectoryInfo(GetModFolder());
		modFolder.Delete(true);

		previousBuild.MoveToSafe(modFolder);

		GlobalCommands.RestartGame();
	}

	public static DateTime GetBuildDateTime(string dllPath)
	{
		const int c_PeHeaderOffset = 60;
		const int c_LinkerTimestampOffset = 8;

		byte[] buffer = new byte[2048];

		using (FileStream stream = new FileStream(dllPath, FileMode.Open, FileAccess.Read))
			stream.Read(buffer, 0, 2048);

		int offset = BitConverter.ToInt32(buffer, c_PeHeaderOffset);
		int secondsSince1970 = BitConverter.ToInt32(buffer, offset + c_LinkerTimestampOffset);
		DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		DateTime linkTimeUtc = epoch.AddSeconds(secondsSince1970);

		return linkTimeUtc;
	}

	public static DateTime GetCurrentBuildDateTime() => GetBuildDateTime(Path.Combine(GetModFolder(), "TwitchPlaysAssembly.dll"));

	public static string GetModFolder() => ModManager.Instance.GetEnabledModPaths(ModInfo.ModSourceEnum.Local)
						.FirstOrDefault(x => Directory.GetFiles(x, "TwitchPlaysAssembly.dll").Any()) ??
					ModManager.Instance.GetEnabledModPaths(ModInfo.ModSourceEnum.SteamWorkshop)
						.First(x => Directory.GetFiles(x, "TwitchPlaysAssembly.dll").Any());
}