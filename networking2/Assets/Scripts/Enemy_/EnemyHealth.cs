using Unity.Netcode;
using UnityEngine;

public class EnemyHealth : NetworkBehaviour
{
    [Header("Enemy Health Settings")]
    [Space(5)]
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>();
    public float maxHealth;
    private bool isDead;

    [Header("VFX")]
    [Space(5)]
    public GameObject enemyDeathParticle;


    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }
        
    }

    public void TakeDamage(float damage)
    {
        if (!IsServer || isDead) return;
        currentHealth.Value -= damage;

        if(currentHealth.Value <= 0)
        {
            isDead = true;
            Die();
        }

        
    }

    private void Die()
    { 
        SpawnDeathPartcleClientRpc();
        GetComponent<NetworkObject>().Despawn();
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
