using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class NetworkProjectile : NetworkBehaviour
{
    [SerializeField] private ProjectileData[] projectiles;

    public ProjectileData projectileData;

    private NetworkVariable<Vector3> moveDir = new NetworkVariable<Vector3>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> projectileIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Rigidbody rb;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null)
            rb.useGravity = false;

        projectileIndex.OnValueChanged += OnProjectileIndexChanged;
        moveDir.OnValueChanged += OnMoveDirChanged;


        SetProjectileData(projectileIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        projectileIndex.OnValueChanged -= OnProjectileIndexChanged;
        moveDir.OnValueChanged -= OnMoveDirChanged;
    }

    void OnProjectileIndexChanged(int previous, int current)
    {
        SetProjectileData(current);
    }

    void SetProjectileData(int index)
    {
        if (projectiles == null || projectiles.Length == 0) return;

        if (index < 0 || index >= projectiles.Length)
            index = 0;

        projectileData = projectiles[index];
    }

    void OnMoveDirChanged(Vector3 previous, Vector3 current)
    {
        if (IsServer) return;

        // Fix: If data isn't set yet, try to set it using the current networked index
        if (projectileData == null)
        {
            SetProjectileData(projectileIndex.Value);
        }

        // Safety check: if it's still null (e.g., array not assigned in inspector)
        if (projectileData == null)
        {
            Debug.LogWarning("ProjectileData still null on client!");
            return;
        }

        if (rb != null)
            rb.linearVelocity = current * projectileData.speed;

        if (current != Vector3.zero)
            rb.rotation = Quaternion.LookRotation(current);
    }


    public void Initialize(Vector3 direction, int index)
    {
        if (!IsServer) return;

        projectileIndex.Value = index;
        moveDir.Value = direction;

        SetProjectileData(index);

        if (rb != null)
            rb.linearVelocity = direction * projectileData.speed;

        StartCoroutine(LifetimeTimer());
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || !IsSpawned) return;

        var health = collision.collider.GetComponent<EnemyHealth>();

        if (health != null)
        {
            Vector3 hitDir = (collision.transform.position - transform.position).normalized;
            health.TakeDamage(projectileData.damage, hitDir);
        }

        SpawnVFXClientRpc(transform.position);
        GetComponent<NetworkObject>().Despawn();
    }

    IEnumerator LifetimeTimer()
    {
        yield return new WaitForSeconds(projectileData.lifetime);

        if (IsServer && IsSpawned)
            GetComponent<NetworkObject>().Despawn();
    }

    [ClientRpc]
    private void SpawnVFXClientRpc(Vector3 spawnPos)
    {
        if (projectileData != null && projectileData.hitEffect != null)
        {
            Instantiate(projectileData.hitEffect, spawnPos, Quaternion.identity);
        }
    }
}