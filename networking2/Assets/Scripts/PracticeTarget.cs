using UnityEngine;
using System.Collections;

public class PracticeTarget : MonoBehaviour
{
    [Header("Respawn")]
    [SerializeField] private float minRespawnTime = 2f;
    [SerializeField] private float maxRespawnTime = 5f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip upSound;
    [SerializeField] private AudioClip downSound;

    [Header("Animation")]
    [SerializeField] private Animation targetAnimation;
    [SerializeField] private AnimationClip targetUp;
    [SerializeField] private AnimationClip targetDown;

    private bool isDown;

    public void Hit()
    {
        if (isDown)
            return;

        StartCoroutine(TargetRoutine());
    }

    private IEnumerator TargetRoutine()
    {
        isDown = true;

        targetAnimation.clip = targetDown;
        targetAnimation.Play();

        if (audioSource != null && downSound != null)
        {
            audioSource.PlayOneShot(downSound);
        }

        float delay =
            Random.Range(
                minRespawnTime,
                maxRespawnTime
            );

        yield return new WaitForSeconds(delay);

        targetAnimation.clip = targetUp;
        targetAnimation.Play();

        if (audioSource != null && upSound != null)
        {
            audioSource.PlayOneShot(upSound);
        }

        isDown = false;
    }
}