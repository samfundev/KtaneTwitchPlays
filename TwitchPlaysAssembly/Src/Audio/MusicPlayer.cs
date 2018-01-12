using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class MusicPlayer : MonoBehaviour
{
    private static Dictionary<string, MusicPlayer> _musicPlayers = new Dictionary<string, MusicPlayer>();

    public AudioSource startInterruptSound = null;
    public AudioSource musicLoopSound = null;
    public AudioSource endInterruptSound = null;

    private Coroutine _currentCoroutine;
    
    private void Awake()
    {
        _musicPlayers[name] = this;
    }

    public static MusicPlayer GetMusicPlayer(string name)
    {
        return _musicPlayers[name];
    }

    public static MusicPlayer StartRandomMusic()
    {
        MusicPlayer player = _musicPlayers.Values.ElementAt(UnityEngine.Random.Range(0, _musicPlayers.Values.Count));
        player.StartMusic();
        return player;
    }

    public static void StopAllMusic()
    {
        foreach (MusicPlayer player in _musicPlayers.Values)
        {
            player.StopMusic();
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
        if (musicLoopSound == null || musicLoopSound.isPlaying)
        {
            yield break;
        }

        InterruptMusic.Instance.SetMusicInterrupt(true);

        if (startInterruptSound != null)
        {
            startInterruptSound.time = 0.0f;
            startInterruptSound.Play();
            yield return new WaitForSeconds(startInterruptSound.clip.length);
        }

        musicLoopSound.time = 0.0f;
        musicLoopSound.Play();
        _currentCoroutine = null;
    }

    private IEnumerator EndMusicCoroutine()
    {
        if (musicLoopSound == null)
        {
            yield break;
        }

        if (musicLoopSound.isPlaying)
        {
            musicLoopSound.Stop();
            if (endInterruptSound != null)
            {
                endInterruptSound.time = 0.0f;
                endInterruptSound.Play();
                yield return new WaitForSeconds(endInterruptSound.clip.length);
            }
        }

        InterruptMusic.Instance.SetMusicInterrupt(false);
        _currentCoroutine = null;
    }
}
