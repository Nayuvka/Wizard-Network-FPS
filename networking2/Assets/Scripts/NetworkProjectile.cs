using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class NetworkProjectile : NetworkBehaviour
{
    [SerializeField] private float speed = 50f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] GameObject vfxPrefab;

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
        if (rb != null)
            rb.linearVelocity = current * speed;
    }

    public void Initialize(Vector3 direction)
    {
        moveDir.Value = direction;

        if (rb != null)
            rb.linearVelocity = direction * speed;

        StartCoroutine(LifetimeTimer());
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || !IsSpawned) return;

        SpawnVFXClientRpc(transform.position);
        if (IsSpawned) GetComponent<NetworkObject>().Despawn();
        //GetComponent<NetworkObject>().Despawn();
        Destroy(gameObject);
    }

    IEnumerator LifetimeTimer()
    {
        yield return new WaitForSeconds(lifetime);
        if (IsServer && IsSpawned)
            GetComponent<NetworkObject>().Despawn();
    }

    [ClientRpc]

    private void SpawnVFXClientRpc(Vector3 spawnPos)
    {
        if (vfxPrefab != null)
        {
            Instantiate(vfxPrefab, spawnPos, Quaternion.identity);
        }
    }
}