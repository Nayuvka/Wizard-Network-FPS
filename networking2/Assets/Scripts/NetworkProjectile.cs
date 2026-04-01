using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class NetworkProjectile : NetworkBehaviour
{
    //[SerializeField] private float speed = 50f;
    //[SerializeField] private float lifetime = 5f;
    //[SerializeField] GameObject vfxPrefab;
    public ProjectileData projectileData;

    private NetworkVariable<Vector3> moveDir = new NetworkVariable<Vector3>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Rigidbody rb;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.useGravity = false;

        moveDir.OnValueChanged += OnMoveDirChanged;
    }

    public override void OnNetworkDespawn()
    {
        moveDir.OnValueChanged -= OnMoveDirChanged;
    }

    void OnMoveDirChanged(Vector3 previous, Vector3 current)
    {
        if (IsServer) return;

        if (rb != null)
            rb.linearVelocity = current * projectileData.speed;

        if (current != Vector3.zero)
            rb.rotation = Quaternion.LookRotation(current);
    }

    public void Initialize(Vector3 direction)
    {
        if (!IsServer) return;
        moveDir.Value = direction;

        if (rb != null)
            rb.linearVelocity = direction * projectileData.speed;

        //if (IsServer)
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
        if (projectileData.hitEffect != null)
        {
            Instantiate(projectileData.hitEffect, spawnPos, Quaternion.identity);
        }
    }
}