using UnityEngine;
using System.Collections.Generic;

public enum MusicGenre
{
    Jazz,
    Forties
}

public class MusicManager : MonoBehaviour
{
    [Header("MUSIC")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private List<AudioClip> jazzClips = new();
    [SerializeField] private List<AudioClip> fortiesClips = new();

    [Header("AMBIENT")]
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioClip jazzAmbient;
    [SerializeField] private AudioClip fortiesAmbient;

    private List<AudioClip> currentMusicList;
    private int lastIndex = -1;

    private void Start()
    {
        SetGenre(MusicGenre.Jazz);
    }

    private void Update()
    {
        if (!musicSource.isPlaying && musicSource.clip != null)
            PlayNextMusic();
    }

    public void SetGenre(MusicGenre genre)
    {
        // Música
        currentMusicList = (genre == MusicGenre.Jazz) ? jazzClips : fortiesClips;
        lastIndex = -1;

        if (currentMusicList != null && currentMusicList.Count > 0)
        {
            musicSource.Stop();
            musicSource.clip = null;
            PlayNextMusic();
        }

        // Ambiente
        AudioClip targetAmbient = (genre == MusicGenre.Jazz) ? jazzAmbient : fortiesAmbient;
        if (ambientSource && ambientSource.clip != targetAmbient)
        {
            ambientSource.clip = targetAmbient;
            ambientSource.loop = true;
            ambientSource.Play();
        }
    }

    private void PlayNextMusic()
    {
        if (currentMusicList == null || currentMusicList.Count == 0)
            return;

        int index = Random.Range(0, currentMusicList.Count);
        if (index == lastIndex && currentMusicList.Count > 1)
            index = (index + 1) % currentMusicList.Count;

        lastIndex = index;

        musicSource.clip = currentMusicList[index];
        musicSource.Play();
    }
}
