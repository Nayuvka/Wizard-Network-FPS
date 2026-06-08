using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class PracticeTarget : NetworkBehaviour
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

    private Collider targetCollider;

    private void Start()
    {
        targetCollider = GetComponent<Collider>();
    }

    public void Hit()
    {
        if (!IsServer || isDead)
            return;

        hitCount++;

        if (hitCount == 1)
        {
            PushClientRpc();
        }
        else if (hitCount >= 2)
        {
            isDead = true;
            DieClientRpc();

            StartCoroutine(RespawnRoutine());
        }
    }

    public void ExplodeHit()
    {
        if (!IsServer || isDead)
            return;

        hitCount = 2;
        isDead = true;

        DieClientRpc();
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        float delay = Random.Range(minRespawnTime, maxRespawnTime);
        yield return new WaitForSeconds(delay);

        hitCount = 0;
        isDead = false;

        ReviveClientRpc();
    }

    [ClientRpc]
    private void PushClientRpc()
    {
        animator.SetTrigger("Push");

        if (audioSource != null && pushSound != null)
        {
            audioSource.PlayOneShot(pushSound);
        }
    }

    [ClientRpc]
    private void DieClientRpc()
    {
        animator.SetTrigger("Die");
        targetCollider.enabled = false;

        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
    }

    [ClientRpc]
    private void ReviveClientRpc()
    {
        targetCollider.enabled = true;
        animator.SetTrigger("Revive");

        if (audioSource != null && reviveSound != null)
        {
            audioSource.PlayOneShot(reviveSound);
        }
    }
}