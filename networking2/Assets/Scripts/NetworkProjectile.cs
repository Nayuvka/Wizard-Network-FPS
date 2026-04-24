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

    public void Initialize(Vector3 hitPoint, ulong targetId, int extraDamage)
    {
        targetHitPoint = hitPoint;
        targetNetworkObjectId = targetId;
        additionalDamage = extraDamage;
        lastPosition = transform.position;

        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out targetNetObj);

        if (IsServer)
        {
            StartCoroutine(DestroyAfterDelay(lifetime));
        }
    }

    void Update()
    {
        if (targetReached) return;

        lastPosition = transform.position;
        Vector3 destination = (targetNetObj != null) ? targetNetObj.transform.position : targetHitPoint;
        
        float step = speed * Time.deltaTime;
        float distanceToTarget = Vector3.Distance(transform.position, destination);

        if (distanceToTarget <= step)
        {
            targetReached = true;
            if (IsServer) OnTargetReached();
        }
        else
        {
            Vector3 moveDir = (destination - transform.position).normalized;
            transform.position += moveDir * step;
            if (moveDir != Vector3.zero) transform.forward = moveDir;
        }
    }

    private void OnTargetReached()
    {
        Vector3 finalImpactPos = transform.position;

        if (targetNetObj != null)
        {
            Vector3 rayDir = (targetNetObj.transform.position - lastPosition).normalized;
            Ray ray = new Ray(lastPosition, rayDir);
            
            if (targetNetObj.TryGetComponent(out Collider col))
            {
                if (col.Raycast(ray, out RaycastHit hit, 10f))
                {
                    finalImpactPos = hit.point;
                }
                else
                {
                    finalImpactPos = col.ClosestPoint(lastPosition);
                }
            }

            if (targetNetObj.TryGetComponent(out NetworkEnemy enemy))
            {
                ApplyTypeEffect(enemy, finalImpactPos);
            }
            else if (targetNetObj.TryGetComponent(out NetworkBoss boss))
            {
                boss.TakeDamage(baseDamage + additionalDamage);
                SpawnImpactVisualsClientRpc(finalImpactPos);
                DespawnProjectile();
            }
        }
        else
        {
            SpawnImpactVisualsClientRpc(targetHitPoint);
            DespawnProjectile();
        }
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
                enemy.TakeDamage(finalDamage);
                StopProjectileVisuals();
                SpawnImpactVisualsClientRpc(impactPos);
                StartCoroutine(BurnEffect(enemy, 5));
                break;

            case ProjectileType.Frostball:
                enemy.TakeDamage(finalDamage);
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
                enemy.TakeDamage(finalDamage);
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
            if (col.TryGetComponent(out NetworkEnemy enemy)) enemy.TakeDamage(damage);
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