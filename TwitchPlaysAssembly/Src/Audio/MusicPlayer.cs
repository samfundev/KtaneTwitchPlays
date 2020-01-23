using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
	private static readonly Dictionary<string, MusicPlayer> MusicPlayers = new Dictionary<string, MusicPlayer>();

	public AudioSource StartInterruptSound;
	public AudioSource MusicLoopSound;
	public AudioSource EndInterruptSound;

	private Coroutine _currentCoroutine;

	private void Awake() => MusicPlayers[name] = this;

	public static MusicPlayer GetMusicPlayer(string name) => MusicPlayers[name];

	public static MusicPlayer StartRandomMusic()
	{
		MusicPlayer player = MusicPlayers.Values.ElementAt(Random.Range(0, MusicPlayers.Values.Count));
		player.StartMusic();
		return player;
	}

	public static void StopAllMusic()
	{
		foreach (MusicPlayer player in MusicPlayers.Values)
		{
			player.StopMusic();
		}
	}

	public static void LoadMusic()
	{
		var interruptSource = MusicPlayers.Values.First().StartInterruptSound;

		var musicDirectory = Path.Combine(Application.persistentDataPath, "ElevatorMusic");
		if (!Directory.Exists(musicDirectory))
			Directory.CreateDirectory(musicDirectory);

		foreach (var file in Directory.GetFiles(musicDirectory))
		{
			if (Path.GetExtension(file).EqualsAny(".wav", ".ogg"))
			{
				try
				{
					AudioClip clip = new WWW("file:///" + file).GetAudioClip();
					while (clip.loadState != AudioDataLoadState.Loaded)
					{
					}

					var name = Path.GetFileName(file);
					var musicObject = new GameObject(name);
					musicObject.transform.parent = TwitchPlaysService.Instance.transform;
					var musicPlayer = musicObject.AddComponent<MusicPlayer>();
					var source = musicObject.AddComponent<AudioSource>();
					source.loop = true;
					source.clip = clip;

					musicPlayer.StartInterruptSound = interruptSource;
					musicPlayer.MusicLoopSound = source;
					musicPlayer.EndInterruptSound = interruptSource;
				}
				catch (System.Exception ex)
				{
					DebugHelper.LogException(ex, $"Failed to load \"{file}\" because:");
				}
			}
		}
	}

	public void StartMusic()
	{
		if (_currentCoroutine != null)
		{
			StopCoroutine(_currentCoroutine);
		}
		_currentCoroutine = StartCoroutine(StartMusicCoroutine());
	}

	public void StopMusic()
	{
		if (_currentCoroutine != null)
		{
			StopCoroutine(_currentCoroutine);
		}
		_currentCoroutine = StartCoroutine(EndMusicCoroutine());
	}

	private IEnumerator StartMusicCoroutine()
	{
		if (MusicLoopSound == null || MusicLoopSound.isPlaying)
		{
			yield break;
		}

		InterruptMusic.Instance.SetMusicInterrupt(true);

		if (StartInterruptSound != null)
		{
			StartInterruptSound.time = 0.0f;
			StartInterruptSound.Play();
			yield return new WaitForSeconds(StartInterruptSound.clip.length);
		}

		MusicLoopSound.time = 0.0f;
		MusicLoopSound.Play();
		_currentCoroutine = null;
	}

	private IEnumerator EndMusicCoroutine()
	{
		if (MusicLoopSound == null)
		{
			yield break;
		}

		if (MusicLoopSound.isPlaying)
		{
			MusicLoopSound.Stop();
			if (EndInterruptSound != null)
			{
				EndInterruptSound.time = 0.0f;
				EndInterruptSound.Play();
				yield return new WaitForSeconds(EndInterruptSound.clip.length);
			}
		}

		InterruptMusic.Instance.SetMusicInterrupt(false);
		_currentCoroutine = null;
	}
}
