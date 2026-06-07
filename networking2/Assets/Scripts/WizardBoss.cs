using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections;

public class WizardBoss : NetworkBehaviour, IDamageable
{
    [Header("Bat Settings")]
    [SerializeField] private GameObject batPrefab;
    [SerializeField] private Transform batSpawnPoint;
    [SerializeField] private float batSpawnRate = 3.0f;
    private float batSpawnTimer;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;

    [Header("VFX & Status")]
    [SerializeField] private GameObject burnVfxPrefab;
    [SerializeField] private GameObject frostVfxPrefab;
    [SerializeField] private GameObject lightningVfxPrefab;
    [SerializeField] private Transform statusVfxPoint;
    [SerializeField] private Vector3 vfxOffset = new Vector3(0, 2f, 0);

    private GameObject activeBurnVfx;
    private GameObject activeFrostVfx;
    private GameObject activeLightningVfx;

    [Header("Stats")]
    [SerializeField] private float maxHealth = 1200f;
    [SerializeField] private Slider healthBar;
    [SerializeField] private GameObject healthCanvas;
    
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(1200f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
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
            if (agent != null) 
            {
                agent.speed = moveSpeed;
                originalSpeed = agent.speed;
            }
            currentHealth.Value = maxHealth;
            FindTarget();
        }

        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth.Value;
        }

        currentHealth.OnValueChanged += UpdateBossUI;
    }

    private void UpdateBossUI(float oldVal, float newVal)
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

        if (!IsServer || isDead) return;

        if (target == null)
        {
            FindTarget();
            return;
        }

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
    }

    void SpawnBat()
    {
        Transform spawnT = batSpawnPoint != null ? batSpawnPoint : transform;
        GameObject bat = Instantiate(batPrefab, spawnT.position, spawnT.rotation);
        bat.GetComponent<NetworkObject>().Spawn();
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

        currentHealth.Value -= amount;

        switch (damageType)
        {
            case DamageType.Fire: PlayBurnVfxClientRpc(5f); break;
            case DamageType.Frost:
                if (frostRoutine != null) StopCoroutine(frostRoutine);
                frostRoutine = StartCoroutine(FrostRoutine(3f, 0.5f));
                PlayFrostVfxClientRpc(3f);
                break;
            case DamageType.Lightning: PlayLightningVfxClientRpc(1.5f); break;
        }

        if (currentHealth.Value <= 0) Die();
    }

    private void Die()
    {
        isDead = true;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        DeathFeedbackClientRpc();
        StartCoroutine(DespawnDelay());
    }

    private IEnumerator DespawnDelay()
    {
        yield return new WaitForSeconds(1f);
        if (GameOverManager.Instance != null) GameOverManager.Instance.WinGame();
        GetComponent<NetworkObject>().Despawn();
    }

    private IEnumerator FrostRoutine(float duration, float multiplier)
    {
        if (agent != null) agent.speed = originalSpeed * multiplier;
        yield return new WaitForSeconds(duration);
        if (!isDead && agent != null) agent.speed = originalSpeed;
    }

    [ClientRpc] private void PlayBurnVfxClientRpc(float duration) => SpawnStatusVfx(burnVfxPrefab, ref activeBurnVfx, duration);
    [ClientRpc] private void PlayFrostVfxClientRpc(float duration) => SpawnStatusVfx(frostVfxPrefab, ref activeFrostVfx, duration);
    [ClientRpc] private void PlayLightningVfxClientRpc(float duration) => SpawnStatusVfx(lightningVfxPrefab, ref activeLightningVfx, duration);

    private void SpawnStatusVfx(GameObject prefab, ref GameObject activeVfx, float duration)
    {
        if (isDead || prefab == null) return;
        if (activeVfx != null) Destroy(activeVfx);
        Transform attach = statusVfxPoint != null ? statusVfxPoint : transform;
        activeVfx = Instantiate(prefab, attach.position + vfxOffset, attach.rotation, attach);
        Destroy(activeVfx, duration);
    }

    [ClientRpc]
    private void DeathFeedbackClientRpc()
    {
        if (activeBurnVfx) Destroy(activeBurnVfx);
        if (activeFrostVfx) Destroy(activeFrostVfx);
        if (activeLightningVfx) Destroy(activeLightningVfx);
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= UpdateBossUI;
    }
}