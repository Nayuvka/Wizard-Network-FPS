using Unity.Netcode;
using UnityEngine;

public class BossProjectile : NetworkBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private float damage = 25f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private GameObject impactEffect;

    private Vector3 moveDirection;
    private bool hasHit = false;

    public void Initialize(Vector3 direction)
    {
        moveDirection = direction;
        if (IsServer)
        {
            Invoke(nameof(DespawnProjectile), lifetime);
        }
    }

    void FixedUpdate()
    {
        if (hasHit || !IsServer) return;

        float moveDistance = speed * Time.fixedDeltaTime;
        Ray ray = new Ray(transform.position, moveDirection);

        if (Physics.Raycast(ray, out RaycastHit hit, moveDistance + 0.1f))
        {
            if (hit.collider.CompareTag("Player"))
            {
                HandlePlayerHit(hit);
                return;
            }
            else if (!hit.collider.CompareTag("Enemy"))
            {
                HandleEnvironmentHit(hit);
                return;
            }
        }

        transform.position += moveDirection * moveDistance;
    }

    private void HandlePlayerHit(RaycastHit hit)
    {
        hasHit = true;
        
        if (hit.collider.TryGetComponent(out PlayerHealth health))
        {
            health.TakeDamage(damage);
        }

        SpawnImpactVisualsClientRpc(hit.point);
        DespawnProjectile();
    }

    private void HandleEnvironmentHit(RaycastHit hit)
    {
        hasHit = true;
        SpawnImpactVisualsClientRpc(hit.point);
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
}