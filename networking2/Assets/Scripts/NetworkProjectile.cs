using Unity.Netcode;
using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class NetworkProjectile : NetworkBehaviour
{
    public enum ProjectileType { Fireball, Frostball, Lightning, Normal }

    [Header("Base Settings")]
    [SerializeField] private ProjectileType type;
    [SerializeField] private float speed = 40f;
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private GameObject impactEffect;

    [Header("Special Effect Settings")]
    [SerializeField] private float effectRadius = 5f;
    [SerializeField] private LayerMask enemyLayer;

    private Vector3 targetHitPoint;
    private ulong targetNetworkObjectId;
    private int additionalDamage = 0;
    private bool targetReached = false;
    private NetworkObject targetNetObj;
    private Vector3 lastPosition;
    private Vector3 hitOffset;
    private bool hasTargetOffset = false;

    public void Initialize(Vector3 hitPoint, ulong targetId, int extraDamage)
    {
        targetHitPoint = hitPoint;
        targetNetworkObjectId = targetId;
        additionalDamage = extraDamage;
        lastPosition = transform.position;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out targetNetObj))
        {
            hitOffset = hitPoint - targetNetObj.transform.position;
            hasTargetOffset = true;
        }

        if (IsServer)
        {
            StartCoroutine(DestroyAfterDelay(lifetime));
        }
    }

    void Update()
    {
        if (targetReached) return;

        lastPosition = transform.position;
        
        Vector3 destination = (targetNetObj != null && hasTargetOffset) 
            ? targetNetObj.transform.position + hitOffset 
            : targetHitPoint;

        float distanceToTarget = Vector3.Distance(transform.position, destination);
        float step = speed * Time.deltaTime;

        if (distanceToTarget <= step || distanceToTarget < 0.1f)
        {
            transform.position = destination;
            targetReached = true;
            if (IsServer) OnTargetReached();
        }
        else
        {
            Vector3 moveDir = (destination - transform.position).normalized;
            if (moveDir != Vector3.zero)
            {
                transform.position += moveDir * step;
                transform.forward = moveDir;
            }
        }
    }

    private void OnTargetReached()
    {
        Vector3 impactPos = transform.position;

        if (targetNetObj != null)
        {
            if (targetNetObj.TryGetComponent(out NetworkEnemy enemy))
            {
                ApplyTypeEffect(enemy, impactPos);
                return;
            }
            
            if (targetNetObj.TryGetComponent(out NetworkBoss boss))
            {
                ApplyBossTypeEffect(boss, impactPos);
                return;
            }
            
            // If we reached here, it's a NetworkObject but NOT an enemy/boss (The Statue)
            SpawnImpactVisualsClientRpc(impactPos);
            DespawnProjectile();
            return;
        }

        // Catch-all for non-NetworkObjects (The Ground)
        SpawnImpactVisualsClientRpc(impactPos);
        DespawnProjectile();
    }

    private void StopProjectileVisuals()
    {
        if (TryGetComponent(out Collider col)) col.enabled = false;
        if (TryGetComponent(out Renderer ren)) ren.enabled = false;
        foreach (Transform child in transform) child.gameObject.SetActive(false);
    }

    private void ApplyTypeEffect(NetworkEnemy enemy, Vector3 impactPos)
    {
        float finalDamage = baseDamage + additionalDamage;

        switch (type)
        {
            case ProjectileType.Fireball:
                enemy.TakeDamage(finalDamage, lastPosition);
                StopProjectileVisuals();
                SpawnImpactVisualsClientRpc(impactPos);
                StartCoroutine(BurnEffect(enemy, 5));
                break;
            case ProjectileType.Frostball:
                enemy.TakeDamage(finalDamage, lastPosition);
                StopProjectileVisuals();
                SpawnImpactVisualsClientRpc(impactPos);
                StartCoroutine(FreezeEffect(enemy, 3f));
                break;
            case ProjectileType.Lightning:
                ChainLightning(impactPos, finalDamage);
                SpawnImpactVisualsClientRpc(impactPos);
                DespawnProjectile();
                break;
            case ProjectileType.Normal:
                enemy.TakeDamage(finalDamage, lastPosition);
                SpawnImpactVisualsClientRpc(impactPos);
                DespawnProjectile();
                break;
        }
    }

    private void ApplyBossTypeEffect(NetworkBoss boss, Vector3 impactPos)
    {
        float finalDamage = baseDamage + additionalDamage;

        switch (type)
        {
            case ProjectileType.Fireball:
                boss.TakeDamage(finalDamage, lastPosition);
                StopProjectileVisuals();
                SpawnImpactVisualsClientRpc(impactPos);
                StartCoroutine(BurnEffectBoss(boss, 5));
                break;
            case ProjectileType.Frostball:
                boss.TakeDamage(finalDamage, lastPosition);
                StopProjectileVisuals();
                SpawnImpactVisualsClientRpc(impactPos);
                boss.ApplyFrostEffect(3f, 0.2f);
                StartCoroutine(FreezeEffectBoss(3f));
                break;
            case ProjectileType.Lightning:
                ChainLightning(impactPos, finalDamage);
                SpawnImpactVisualsClientRpc(impactPos);
                DespawnProjectile();
                break;
            case ProjectileType.Normal:
                boss.TakeDamage(finalDamage, lastPosition);
                SpawnImpactVisualsClientRpc(impactPos);
                DespawnProjectile();
                break;
        }
    }

    private IEnumerator BurnEffect(NetworkEnemy enemy, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            yield return new WaitForSeconds(1f);
            if (enemy != null) enemy.TakeDamage(5f + (additionalDamage * 0.2f));
        }
        DespawnProjectile();
    }

    private IEnumerator BurnEffectBoss(NetworkBoss boss, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            yield return new WaitForSeconds(1f);
            if (boss != null) boss.TakeDamage(5f + (additionalDamage * 0.2f));
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

    private IEnumerator FreezeEffectBoss(float duration)
    {
        yield return new WaitForSeconds(duration);
        DespawnProjectile();
    }

    private void ChainLightning(Vector3 pos, float damage)
    {
        Collider[] hitEnemies = Physics.OverlapSphere(pos, effectRadius, enemyLayer);
        foreach (var col in hitEnemies)
        {
            if (col.TryGetComponent(out NetworkEnemy enemy)) enemy.TakeDamage(damage, pos);
            else if (col.TryGetComponent(out NetworkBoss boss)) boss.TakeDamage(damage, pos);
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
        if (NetworkObject != null && NetworkObject.IsSpawned) DespawnProjectile();
    }
}