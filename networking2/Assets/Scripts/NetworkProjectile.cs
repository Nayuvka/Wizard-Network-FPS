using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkProjectile : NetworkBehaviour
{
    public enum ProjectileType { Fireball, Frostball, Lightning, Normal }

    [SerializeField] private ProjectileType type;
    [SerializeField] private float speed = 40f;
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private float effectRadius = 5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private AudioClip hitSfx;
    [SerializeField] private float hitSfxVolume = 1f;

    private Vector3 targetHitPoint;
    private ulong targetNetworkObjectId;
    private int additionalDamage = 0;
    private bool targetReached = false;
    private NetworkObject targetNetObj;
    private Vector3 lastPosition;
    private Vector3 hitOffset;
    private bool hasTargetOffset = false;
    private ulong ownerClientId;

    public override void OnNetworkSpawn()
    {
        if (TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    public void Initialize(Vector3 hitPoint, ulong targetId, int extraDamage, ulong ownerId)
    {
        if (!IsServer) return;

        targetHitPoint = hitPoint;
        targetNetworkObjectId = targetId;
        additionalDamage = extraDamage;
        ownerClientId = ownerId;
        lastPosition = transform.position;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out targetNetObj))
        {
            hitOffset = hitPoint - targetNetObj.transform.position;
            hasTargetOffset = true;
        }

        StartCoroutine(DestroyAfterDelay(lifetime));
    }

    void Update()
    {
        if (!IsServer || targetReached) return;

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
            OnTargetReached();
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

        if (targetNetObj != null && targetNetObj.TryGetComponent(out IDamageable damageable))
        {
            ApplyTypeEffect(damageable, impactPos);
            return;
        }

        PlayImpactFeedback(impactPos);
        DespawnProjectile();
    }

    private void StopProjectileVisuals()
    {
        StopProjectileVisualsClientRpc();
    }

    private void PlayImpactFeedback(Vector3 impactPos)
    {
        PlayImpactFeedbackClientRpc(impactPos);
    }

    [ClientRpc]
    private void PlayImpactFeedbackClientRpc(Vector3 impactPos)
    {
        if (impactEffect != null)
        {
            Instantiate(impactEffect, impactPos, Quaternion.identity);
        }

        if (hitSfx != null)
        {
            AudioSource.PlayClipAtPoint(
                hitSfx,
                impactPos,
                hitSfxVolume
            );
        }
    }

    [ClientRpc]
    private void StopProjectileVisualsClientRpc()
    {
        if (TryGetComponent(out Collider col))
        {
            col.enabled = false;
        }

        if (TryGetComponent(out Renderer ren))
        {
            ren.enabled = false;
        }

        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    private void ApplyTypeEffect(IDamageable target, Vector3 impactPos)
    {
        float finalDamage = baseDamage + additionalDamage;

        switch (type)
        {
            case ProjectileType.Fireball:
                target.TakeDamage(finalDamage, lastPosition, ownerClientId);
                StopProjectileVisuals();
                PlayImpactFeedback(impactPos);
                StartCoroutine(BurnEffect(target, 5));
                break;

            case ProjectileType.Frostball:
                target.TakeDamage(finalDamage, lastPosition, ownerClientId);
                StopProjectileVisuals();
                PlayImpactFeedback(impactPos);
                StartCoroutine(FreezeEffect(target, 3f));
                break;

            case ProjectileType.Lightning:
                ChainLightning(impactPos, finalDamage);
                PlayImpactFeedback(impactPos);
                DespawnProjectile();
                break;

            case ProjectileType.Normal:
                target.TakeDamage(finalDamage, lastPosition, ownerClientId);
                PlayImpactFeedback(impactPos);
                DespawnProjectile();
                break;
        }
    }

    private IEnumerator BurnEffect(IDamageable target, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            yield return new WaitForSeconds(1f);

            if (target != null)
            {
                target.TakeDamage(5f + (additionalDamage * 0.2f), transform.position, ownerClientId);
            }
        }
        DespawnProjectile();
    }

    private IEnumerator FreezeEffect(IDamageable target, float duration)
    {
        if (target != null && target is MonoBehaviour mb && mb.TryGetComponent(out UnityEngine.AI.NavMeshAgent agent))
        {
            float originalSpeed = agent.speed;
            agent.speed *= 0.2f;

            yield return new WaitForSeconds(duration);

            if (agent != null)
            {
                agent.speed = originalSpeed;
            }
        }
        DespawnProjectile();
    }

    private void ChainLightning(Vector3 pos, float damage)
    {
        Collider[] hitEnemies = Physics.OverlapSphere(pos, effectRadius, enemyLayer);

        foreach (var col in hitEnemies)
        {
            if (col.TryGetComponent(out IDamageable target))
            {
                target.TakeDamage(damage, pos, ownerClientId);
            }
        }
    }

    private void DespawnProjectile()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
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