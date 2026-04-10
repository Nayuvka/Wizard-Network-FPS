using Unity.Netcode;
using UnityEngine;

public class EnemyHealth : NetworkBehaviour
{
    [Header("Enemy Health Settings")]
    [Space(5)]
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>();
    public float maxHealth;
    private bool isDead;

    private Rigidbody rb;

    [Header("VFX")]
    [Space(5)]
    public GameObject enemyDeathParticle;


    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        rb = GetComponent<Rigidbody>();
        
    }

    public void TakeDamage(float damage, Vector3 hitDirection, float knockBackForce)
    {
        if (!IsServer || isDead) return;

        currentHealth.Value -= damage;
        ApplyKnockback(hitDirection, knockBackForce);
        HitFlashClientRpc();

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

    private void Die()
    { 
        SpawnDeathPartcleClientRpc();
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
        if(enemyDeathParticle != null)
        {
            Instantiate(enemyDeathParticle, transform.position, Quaternion.identity);
        }
        
    }
}
