using DarkTonic.MasterAudio;
using UnityEngine;

public class Audio
{
	public static void PlaySound(KMSoundOverride.SoundEffect effectOverride, Transform transform)
	{
		if (ModSoundMap.GroupNameMap.TryGetValue(effectOverride, out string effect))
			MasterAudio.PlaySound3DAtTransformAndForget(effect, transform, 1f, null);
	}

	public static void PlaySound(string effect, Transform transform)
	{
		if (!string.IsNullOrEmpty(effect))
			MasterAudio.PlaySound3DAtTransformAndForget(effect, transform, 1f, null);
	}
}
