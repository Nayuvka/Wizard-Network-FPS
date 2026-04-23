using Unity.Netcode;
using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class NetworkProjectile : NetworkBehaviour
{
    public enum ProjectileType { Fireball, Frostball, Lightning, Normal }

    [Header("Base Settings")]
    [SerializeField] private ProjectileType type;
    [SerializeField] private float speed = 20f;
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private GameObject impactEffect;

    [Header("Special Effect Settings")]
    [SerializeField] private float effectRadius = 5f;
    [SerializeField] private LayerMask enemyLayer;

    private Vector3 moveDirection;
    private int additionalDamage = 0;
    private bool hasHit = false;

    public void Initialize(Vector3 direction, int extraDamage)
    {
        moveDirection = direction;
        additionalDamage = extraDamage;

        if (IsServer)
        {
            StartCoroutine(DestroyAfterDelay(lifetime));
        }
    }

    void Update()
    {
        if (hasHit) return;
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || hasHit) return;

        if (other.TryGetComponent(out NetworkEnemy enemy))
        {
            hasHit = true;
            Vector3 impactPoint = other.ClosestPoint(transform.position);

            ApplyTypeEffect(enemy);
            SpawnImpactVisualsClientRpc(impactPoint);
        }
        else if (other.TryGetComponent(out NetworkBoss boss))
        {
            hasHit = true;
            Vector3 impactPoint = other.ClosestPoint(transform.position);

            float finalDamage = baseDamage + additionalDamage;
            boss.TakeDamage(finalDamage);

            SpawnImpactVisualsClientRpc(impactPoint);
            DespawnProjectile();
        }
        else if (!other.CompareTag("Player"))
        {
            hasHit = true;
            Vector3 impactPoint = other.ClosestPoint(transform.position);

            SpawnImpactVisualsClientRpc(impactPoint);
            DespawnProjectile();
        }
    }

    private void StopProjectileVisuals()
    {
        if (TryGetComponent(out Collider col)) col.enabled = false;
        if (TryGetComponent(out Renderer ren)) ren.enabled = false;
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    private void ApplyTypeEffect(NetworkEnemy enemy)
    {
        float finalDamage = baseDamage + additionalDamage;

        switch (type)
        {
            case ProjectileType.Fireball:
                enemy.TakeDamage(finalDamage);
                StopProjectileVisuals();
                StartCoroutine(BurnEffect(enemy, 5));
                break;

            case ProjectileType.Frostball:
                enemy.TakeDamage(finalDamage);
                StopProjectileVisuals();
                StartCoroutine(FreezeEffect(enemy, 3f));
                break;

            case ProjectileType.Lightning:
                ChainLightning(transform.position, finalDamage);
                DespawnProjectile();
                break;

            case ProjectileType.Normal:
                enemy.TakeDamage(finalDamage);
                DespawnProjectile();
                break;
        }
    }

    private IEnumerator BurnEffect(NetworkEnemy enemy, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            yield return new WaitForSeconds(1f);
            if (enemy != null)
            {
                enemy.TakeDamage(5f + (additionalDamage * 0.2f));
            }
        }
        DespawnProjectile();
    }

    private IEnumerator FreezeEffect(NetworkEnemy enemy, float duration)
    {
        if (enemy != null && enemy.TryGetComponent(out NavMeshAgent agent))
        {
            float originalSpeed = agent.speed;
            agent.speed *= 0.2f;
            yield return new WaitForSeconds(duration);
            if (agent != null) agent.speed = originalSpeed;
        }
        DespawnProjectile();
    }

    private void ChainLightning(Vector3 pos, float damage)
    {
        Collider[] hitEnemies = Physics.OverlapSphere(pos, effectRadius, enemyLayer);
        foreach (var col in hitEnemies)
        {
            if (col.TryGetComponent(out NetworkEnemy enemy))
            {
                enemy.TakeDamage(damage);
            }
        }
    }

    [ClientRpc]
    private void SpawnImpactVisualsClientRpc(Vector3 pos)
    {
        if (impactEffect != null) Instantiate(impactEffect, pos, Quaternion.identity);
    }

    private void DespawnProjectile()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn();
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            DespawnProjectile();
        }
    }
}