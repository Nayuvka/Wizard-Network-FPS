using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;
using System.Collections;

public class WizardBoss : NetworkBehaviour, IDamageable
{
    [Header("Bat Settings")]
    [SerializeField] private GameObject batPrefab;
    [SerializeField] private float batSpawnRate = 3.0f;
    private float batSpawnTimer;

    [Header("Projectile Settings")]
    [SerializeField] private GameObject spellPrefab;
    [SerializeField] private float spellFireRate = 2.0f;
    [SerializeField] private Transform firePoint;
    private float spellTimer;

    [Header("Stats")]
    [SerializeField] private float health = 1200f;
    private NavMeshAgent agent;
    private Transform target;
    private bool isDead = false;

    private float originalSpeed;
    private Coroutine frostRoutine;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent != null) originalSpeed = agent.speed;
            FindTarget();
        }
    }

    void Update()
    {
        if (!IsServer || isDead) return;

        if (target == null) 
        {
            FindTarget();
        }
        else 
        {
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.SetDestination(target.position);
            }
            
            batSpawnTimer += Time.deltaTime;
            if (batSpawnTimer >= batSpawnRate)
            {
                SpawnBat();
                batSpawnTimer = 0;
            }

            spellTimer += Time.deltaTime;
            if (spellTimer >= spellFireRate)
            {
                CastSpell();
                spellTimer = 0;
            }
        }
    }

    void SpawnBat()
    {
        GameObject bat = Instantiate(batPrefab, transform.position + Vector3.up, Quaternion.identity);
        bat.GetComponent<NetworkObject>().Spawn();
    }

    void CastSpell()
    {
        GameObject spell = Instantiate(spellPrefab, firePoint.position, Quaternion.LookRotation(target.position - firePoint.position));
        spell.GetComponent<NetworkObject>().Spawn();
    }

    void FindTarget()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length > 0)
            target = players[Random.Range(0, players.Length)].transform;
    }

    public void TakeDamage(float amount, Vector3 sourcePosition = default, ulong attackerId = ulong.MaxValue, DamageType damageType = DamageType.Normal)
    {
        if (!IsServer || isDead) return;

        if (damageType == DamageType.Frost)
        {
            if (frostRoutine != null) StopCoroutine(frostRoutine);
            frostRoutine = StartCoroutine(FrostRoutine(3f, 0.5f));
        }

        health -= amount;
        if (health <= 0)
        {
            isDead = true;
            if (GameOverManager.Instance != null) GameOverManager.Instance.WinGame();
            GetComponent<NetworkObject>().Despawn();
        }
    }

    private IEnumerator FrostRoutine(float duration, float multiplier)
    {
        if (agent != null) agent.speed = originalSpeed * multiplier;
        yield return new WaitForSeconds(duration);
        if (!isDead && agent != null) agent.speed = originalSpeed;
    }
}