using UnityEngine;

public class BattleMusicManager : MonoBehaviour
{
    public static BattleMusicManager Instance;

    [SerializeField] private AudioSource musicSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        if (musicSource == null)
        {
            GameObject sourceObj = GameObject.Find("MusicSource");

            if (sourceObj != null)
            {
                musicSource = sourceObj.GetComponent<AudioSource>();
            }
        }
    }

    public void StartBattleMusic()
    {
        if (musicSource == null)
            return;

        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    public void StopBattleMusic()
    {
        if (musicSource == null)
            return;

        musicSource.Stop();
    }
}