using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class EnemyHealth : NetworkBehaviour
{
    [Header("Enemy Health Settings")]
    [Space(5)]
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>();
    public float maxHealth;
    private bool isDead;

    private Rigidbody rb;

    [Header("SFX")]
    [Space(5)]

    [SerializeField] private AudioClip deathClip;
    [SerializeField]private AudioSource deathSFX;
    [SerializeField] private AudioSource hitSFX;
    [SerializeField] private AudioSource spawnSFX;

    [Header("VFX")]
    [Space(5)]
    public GameObject enemyDeathParticle;

    [Header("Hit Pop Settings")]
    public float popScale = 1.2f;
    public float popDuration = 0.12f;
    private Vector3 originalScale;



    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        rb = GetComponent<Rigidbody>();
        originalScale = transform.localScale;
    }

    public void TakeDamage(float damage, Vector3 hitDirection, float knockBackForce)
    {
        if (!IsServer || isDead) return;

        currentHealth.Value -= damage;
        ApplyKnockback(hitDirection, knockBackForce);


        HitFlashClientRpc();
        HitPopClientRpc();

        if (currentHealth.Value <= 0)
        {
            isDead = true;
            Die();
        }
    }

    public void ApplyKnockback(Vector3 direction, float knockbackForce)
    {
        if (!IsServer) return;

        //if (rb == null) rb = GetComponent<Rigidbody>();
        rb.AddForce(direction * knockbackForce, ForceMode.Impulse);
    }

    private IEnumerator HitPop()
    {
        Vector3 targetScale = originalScale * popScale;


        float t = 0;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t / popDuration);
            yield return null;
        }

        t = 0;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t / popDuration);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    private void Die()
    { 
        SpawnDeathPartcleClientRpc();
        DeathSFXClientRpc();

        StartCoroutine(DespawnDelay());
    }

    private IEnumerator DespawnDelay()
    {
        yield return new WaitForSeconds(1f);
        GetComponent<NetworkObject>().Despawn();
    }


    [ClientRpc]
    void HitFlashClientRpc()
    {
        GetComponent<EnemyHitFlash>()?.PlayFlash();
    }

    [ClientRpc]

    public void SpawnDeathPartcleClientRpc()
    {
        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        foreach (MeshRenderer mr in GetComponentsInChildren<MeshRenderer>())
        {
            mr.enabled = false;
        }

        foreach (SkinnedMeshRenderer smr in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            smr.enabled = false;
        }

      
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        rb.isKinematic = true;
        if(enemyDeathParticle != null)
        {
            Instantiate(enemyDeathParticle, transform.position, Quaternion.identity);
            
        }
        
    }

    [ClientRpc]
    void DeathSFXClientRpc()
    {
        AudioSource.PlayClipAtPoint(deathClip, transform.position);
    }

    [ClientRpc]
    void HitPopClientRpc()
    {
        StopAllCoroutines();
        StartCoroutine(HitPop());
    }
}
