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

    [Header("Knockback Settings")]
    public float knockbackForce = 10f;


    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }
        
    }

    public void TakeDamage(float damage, Vector3 hitDirection)
    {
        if (!IsServer || isDead) return;

        currentHealth.Value -= damage;
        ApplyKnockback(hitDirection);
        HitFlashClientRpc();

        if (currentHealth.Value <= 0)
        {
            isDead = true;
            Die();
        }
    }

    public void ApplyKnockback(Vector3 direction)
    {
        if (!IsServer) return;

        if (rb == null) rb = GetComponent<Rigidbody>();

        // Apply the force on the server
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
