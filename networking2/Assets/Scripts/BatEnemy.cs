using UnityEngine;
using Unity.Netcode;

public class BatEnemy : NetworkBehaviour, IDamageable
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float health = 20f;
    private Transform target;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            if (players.Length > 0)
                target = players[Random.Range(0, players.Length)].transform;
        }
    }

    void Update()
    {
        if (IsServer && target != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
            transform.LookAt(target);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsServer && other.CompareTag("Player"))
        {
            if (other.TryGetComponent(out PlayerHealth health))
            {
                health.TakeDamage(damage);
                GetComponent<NetworkObject>().Despawn();
            }
        }
    }

    public void TakeDamage(float amount, Vector3 source, ulong attackerId)
    {
        if (!IsServer) return;
        health -= amount;
        if (health <= 0) GetComponent<NetworkObject>().Despawn();
    }
}