using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class NetworkProjectile : NetworkBehaviour
{
    public enum ProjectileType { Fireball, Frostball, Lightning, Slime, Normal }

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
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.TryGetComponent(out NetworkEnemy enemy))
        {
            ApplyTypeEffect(enemy);
            SpawnImpactVisualsClientRpc(transform.position);

            if (type != ProjectileType.Slime)
            {
                DespawnProjectile();
            }
        }
    }

    private void ApplyTypeEffect(NetworkEnemy enemy)
    {
        float finalDamage = baseDamage + additionalDamage;

        switch (type)
        {
            case ProjectileType.Fireball:
                enemy.TakeDamage(finalDamage);
                StartCoroutine(BurnEffect(enemy, 5));
                break;

            case ProjectileType.Frostball:
                enemy.TakeDamage(finalDamage);
                StartCoroutine(FreezeEffect(enemy, 3f));
                break;

            case ProjectileType.Lightning:
                ChainLightning(transform.position, finalDamage);
                break;

            case ProjectileType.Slime:
                AttachSticky(enemy.transform, finalDamage);
                break;

            case ProjectileType.Normal:
                enemy.TakeDamage(finalDamage);
                break;
        }
    }

    private IEnumerator BurnEffect(NetworkEnemy enemy, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            yield return new WaitForSeconds(1f);
            if (enemy != null) enemy.TakeDamage(2f + (additionalDamage * 0.1f));
        }
    }

    private IEnumerator FreezeEffect(NetworkEnemy enemy, float duration)
    {
        if (enemy.TryGetComponent(out UnityEngine.AI.NavMeshAgent agent))
        {
            float originalSpeed = agent.speed;
            agent.speed *= 0.5f;
            yield return new WaitForSeconds(duration);
            if (agent != null) agent.speed = originalSpeed;
        }
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

    private void AttachSticky(Transform target, float damage)
    {
        transform.SetParent(target);
        moveDirection = Vector3.zero;
        speed = 0;
        StartCoroutine(StickyExplosion(damage));
    }

    private IEnumerator StickyExplosion(float damage)
    {
        yield return new WaitForSeconds(2f);
        ChainLightning(transform.position, damage);
        DespawnProjectile();
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
        DespawnProjectile();
    }
}