using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class BatEnemy : NetworkBehaviour, IDamageable
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float health = 20f;
    private Transform target;

    private float originalSpeed;
    private Coroutine frostRoutine;

    public override void OnNetworkSpawn()
    {
        originalSpeed = speed;
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

    public void TakeDamage(float amount, Vector3 sourcePosition = default, ulong attackerId = ulong.MaxValue, DamageType damageType = DamageType.Normal)
    {
        if (!IsServer) return;

        if (damageType == DamageType.Frost)
        {
            if (frostRoutine != null) StopCoroutine(frostRoutine);
            frostRoutine = StartCoroutine(FrostRoutine(3f, 0.5f));
        }

        health -= amount;
        if (health <= 0) GetComponent<NetworkObject>().Despawn();
    }

    private IEnumerator FrostRoutine(float duration, float multiplier)
    {
        speed = originalSpeed * multiplier;
        yield return new WaitForSeconds(duration);
        speed = originalSpeed;
    }
}