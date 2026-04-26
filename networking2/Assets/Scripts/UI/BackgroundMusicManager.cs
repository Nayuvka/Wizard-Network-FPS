using UnityEngine;

public class BackgroundMusicManager : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] backgroundTracks;

    [Header("Playback Settings")]
    [SerializeField] private bool playRandomly = false;
    [SerializeField] private bool loopAll = true;

    private int currentTrackIndex = 0;

    private void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            Debug.LogWarning("No AudioSource assigned or found on Music Manager.");
            return;
        }

        if (backgroundTracks == null || backgroundTracks.Length == 0)
        {
            Debug.LogWarning("No background tracks assigned to the Music Manager.");
            return;
        }

        currentTrackIndex = playRandomly ? Random.Range(0, backgroundTracks.Length) : 0;
        PlayTrack(currentTrackIndex);
    }

    private void Update()
    {
        if (audioSource == null) return;

        if (!audioSource.isPlaying && loopAll)
        {
            PlayNextTrack();
        }
    }

    private void PlayTrack(int index)
    {
        if (index < 0 || index >= backgroundTracks.Length)
            return;

        audioSource.clip = backgroundTracks[index];
        audioSource.Play();
    }

    public void PlayNextTrack()
    {
        if (backgroundTracks == null || backgroundTracks.Length == 0)
            return;

        if (playRandomly)
        {
            int randomIndex = Random.Range(0, backgroundTracks.Length);

            while (randomIndex == currentTrackIndex && backgroundTracks.Length > 1)
            {
                randomIndex = Random.Range(0, backgroundTracks.Length);
            }

            currentTrackIndex = randomIndex;
        }
        else
        {
            currentTrackIndex = (currentTrackIndex + 1) % backgroundTracks.Length;
        }

        PlayTrack(currentTrackIndex);
    }

    public void StopMusic()
    {
        if (audioSource != null)
            audioSource.Stop();
    }
}