using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class NetworkBoss : NetworkBehaviour
{
    [Header("Stats")]
    [SerializeField] private float maxHealth = 1000f;
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(1000f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("UI")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private GameObject healthCanvas;

    [Header("Shooting")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private Transform[] firePoints;
    [SerializeField] private float fireRate = 1.5f;
    [SerializeField] private float attackRange = 25f;
    private int nextFirePointIndex = 0;
    private float fireTimer;

    [Header("Movement")]
    private NavMeshAgent agent;
    private Transform target;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            agent = GetComponent<NavMeshAgent>();
            currentHealth.Value = maxHealth;
            FindTarget();
        }

        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth.Value;
        }

        currentHealth.OnValueChanged += UpdateUI;
    }

    private void UpdateUI(float oldVal, float newVal)
    {
        if (healthBar != null) healthBar.value = newVal;
    }

    void Update()
    {
        if (healthCanvas != null && Camera.main != null)
        {
            healthCanvas.transform.LookAt(healthCanvas.transform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up);
        }

        if (!IsServer) return;

        if (target == null)
        {
            FindTarget();
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);
        agent.SetDestination(target.position);

        fireTimer += Time.deltaTime;
        if (distance <= attackRange && fireTimer >= fireRate)
        {
            Shoot();
            fireTimer = 0;
        }
    }

    void Shoot()
    {
        Transform activePoint = firePoints[nextFirePointIndex];
        nextFirePointIndex = (nextFirePointIndex + 1) % firePoints.Length;

        Vector3 targetDir = (target.position + Vector3.up - activePoint.position).normalized;

        GameObject fireball = Instantiate(fireballPrefab, activePoint.position, Quaternion.LookRotation(targetDir));
        NetworkObject netObj = fireball.GetComponent<NetworkObject>();
        netObj.Spawn();

        if (fireball.TryGetComponent(out BossProjectile proj))
        {
            proj.Initialize(targetDir);
        }
    }

    public void TakeDamage(float amount)
    {
        if (!IsServer) return;

        currentHealth.Value -= amount;

        if (currentHealth.Value <= 0)
        {
            SpawnManager.Instance.EnemyDeath(NetworkObject);
            NetworkObject.Despawn();
        }
    }

    void FindTarget()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length > 0)
        {
            target = players[Random.Range(0, players.Length)].transform;
        }
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= UpdateUI;
    }
}