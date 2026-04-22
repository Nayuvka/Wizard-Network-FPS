using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class NetworkProjectile : NetworkBehaviour
{
    [SerializeField] private float speed = 50f;
    [SerializeField] private float damage = 25f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private GameObject vfxPrefab;
    [SerializeField] private LayerMask layersToHit;

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
        {
            rb.linearVelocity = moveDir.Value * speed;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }
        moveDir.OnValueChanged += OnMoveDirChanged;
    }

    public override void OnNetworkDespawn()
    {
        moveDir.OnValueChanged -= OnMoveDirChanged;
    }

    private void OnMoveDirChanged(Vector3 previous, Vector3 current)
    {
        if (rb != null) rb.linearVelocity = current * speed;
    }

    public void Initialize(Vector3 direction, int index)
    {
        if (!IsServer) return;
        moveDir.Value = direction;
        if (rb != null) rb.linearVelocity = direction * speed;
        StartCoroutine(LifetimeTimer());
    }

    private void FixedUpdate()
    {
        if (!IsServer || !IsSpawned) return;

        float stepDistance = speed * Time.fixedDeltaTime;
        Vector3 direction = rb.linearVelocity.normalized;

        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, stepDistance, layersToHit))
        {
            HandleHit(hit.collider, hit.point);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || !IsSpawned) return;

        if (((1 << other.gameObject.layer) & layersToHit) != 0)
        {
            HandleHit(other, other.ClosestPoint(transform.position));
        }
    }

    private void HandleHit(Collider other, Vector3 hitPoint)
    {
        if (other.CompareTag("Enemy"))
        {
            NetworkEnemy enemy = other.GetComponent<NetworkEnemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }

        ExplodeClientRpc(hitPoint);
        GetComponent<NetworkObject>().Despawn();
    }

    [ClientRpc]
    private void ExplodeClientRpc(Vector3 pos)
    {
        if (vfxPrefab != null) Instantiate(vfxPrefab, pos, Quaternion.identity);
    }

    private IEnumerator LifetimeTimer()
    {
        yield return new WaitForSeconds(lifetime);
        if (IsServer && IsSpawned) GetComponent<NetworkObject>().Despawn();
    }
}