using UnityEngine;
using System.Collections;

public class PracticeTarget : MonoBehaviour
{
    [Header("Respawn")]
    [SerializeField] private float minRespawnTime = 2f;
    [SerializeField] private float maxRespawnTime = 5f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pushSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip reviveSound;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private int hitCount;
    private bool isDead;

    public void Hit()
    {
        if (isDead)
            return;

        hitCount++;

        // First hit
        if (hitCount == 1)
        {
            animator.SetTrigger("Push");

            if (audioSource != null && pushSound != null)
            {
                audioSource.PlayOneShot(pushSound);
            }
        }
        // Second hit
        else if (hitCount >= 2)
        {
            isDead = true;

            animator.SetTrigger("Die");

            if (audioSource != null && deathSound != null)
            {
                audioSource.PlayOneShot(deathSound);
            }

            StartCoroutine(RespawnRoutine());
        }
    }

    public void ExplodeHit()
    {
        if (isDead)
            return;

        hitCount = 2;
        isDead = true;

        animator.SetTrigger("Die");
        Debug.Log("Death Animation");

        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        float delay = Random.Range(
            minRespawnTime,
            maxRespawnTime
        );

        yield return new WaitForSeconds(delay);

        animator.SetTrigger("Revive");

        if (audioSource != null && reviveSound != null)
        {
            audioSource.PlayOneShot(reviveSound);
        }

        hitCount = 0;
        isDead = false;
    }
}