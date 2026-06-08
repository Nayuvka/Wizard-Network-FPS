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

    [Header("Movement & Ranges")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float castingRange = 12f;
    [SerializeField] private float tooCloseRange = 5f;
    [SerializeField] private float backAwayDistance = 6f;
    [SerializeField] private float castDuration = 2.5f;

    [Header("Target Tracking")]
    [SerializeField] private float rotationSpeed = 6f;
    [SerializeField] private float rotationOffsetAngle = 0f;

    private bool isCasting;
    private bool isBackingAway;

    [Header("Death VFX")]
    [SerializeField] private GameObject deathVfx;

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

    private NetworkVariable<float> currentHealth =
        new NetworkVariable<float>(1200f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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
            FindNearestPlayer();
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
        HandleCanvasBillboard();

        if (!IsServer || isDead) return;
        FindNearestPlayer();

        if (target == null) return;

        HandleSmoothLookRotation();
        HandleMovement();
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= UpdateBossUI;
    }

    private void HandleMovement()
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
            return;

        float distance = Vector3.Distance(transform.position, target.position);

        if (!isCasting && !isBackingAway)
        {
            if (distance <= tooCloseRange)
            {
                StartCoroutine(BackAwayRoutine());
            }
            else if (distance > castingRange)
            {
                agent.updateRotation = true;
                agent.isStopped = false;
                agent.SetDestination(target.position);
            }
            else
            {
                StartCoroutine(CastRoutine());
            }
        }
    }

    private void HandleSmoothLookRotation()
    {
        if ((isCasting || isBackingAway) && target != null)
        {
            Vector3 dir = (target.position - transform.position).normalized;
            dir.y = 0;

            if (dir != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(dir);
                Quaternion offsetRotation = lookRotation * Quaternion.Euler(0, rotationOffsetAngle, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, offsetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }

    private IEnumerator CastRoutine()
    {
        isCasting = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        yield return new WaitForSeconds(castDuration);

        if (target != null && !isDead)
        {
            SpawnBat();
        }

        yield return new WaitForSeconds(batSpawnRate);

        isCasting = false;
    }

    private IEnumerator BackAwayRoutine()
    {
        isBackingAway = true;
        agent.updateRotation = false;
        agent.isStopped = false;

        Vector3 awayDir = (transform.position - target.position).normalized;
        Vector3 targetPos = transform.position + awayDir * backAwayDistance;

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, backAwayDistance, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }

        yield return null;

        float safetyTimeout = 2.5f;
        while (agent.isActiveAndEnabled && agent.isOnNavMesh &&
              (agent.pathPending || agent.remainingDistance > agent.stoppingDistance) && safetyTimeout > 0f)
        {
            safetyTimeout -= Time.deltaTime;
            yield return null;
        }

        agent.updateRotation = true;
        isBackingAway = false;
    }

    void SpawnBat()
    {
        Transform spawnT = batSpawnPoint != null ? batSpawnPoint : transform;

        Vector3 directionToPlayer = (target.position - spawnT.position).normalized;
        directionToPlayer.y = 0;
        Quaternion launchRotation = Quaternion.LookRotation(directionToPlayer);

        GameObject bat = Instantiate(batPrefab, spawnT.position, launchRotation);
        bat.GetComponent<NetworkObject>().Spawn();
    }

    private void FindNearestPlayer()
    {
        float closestDistance = Mathf.Infinity;
        Transform nearest = null;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            PlayerHealth pHealth = client.PlayerObject.GetComponent<PlayerHealth>();
            RespawnScript respawn = client.PlayerObject.GetComponent<RespawnScript>();

            if (pHealth == null || respawn == null) continue;

            if (pHealth.currentHealth.Value <= 0 || respawn.isRespawning.Value) continue;

            float dist = Vector3.Distance(transform.position, client.PlayerObject.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                nearest = client.PlayerObject.transform;
            }
        }

        target = nearest;
    }

    private void HandleCanvasBillboard()
    {
        if (healthCanvas != null && Camera.main != null)
        {
            healthCanvas.transform.LookAt(healthCanvas.transform.position + Camera.main.transform.forward);
        }
    }

    public void TakeDamage(float amount, Vector3 sourcePosition = default, ulong attackerId = ulong.MaxValue, DamageType damageType = DamageType.Normal)
    {
        if (!IsServer || isDead) return;

        currentHealth.Value -= amount;

        switch (damageType)
        {
            case DamageType.Fire:
                PlayBurnVfxClientRpc(5f);
                break;

            case DamageType.Frost:
                if (frostRoutine != null) StopCoroutine(frostRoutine);
                frostRoutine = StartCoroutine(FrostRoutine(3f, 0.5f));
                PlayFrostVfxClientRpc(3f);
                break;

            case DamageType.Lightning:
                PlayLightningVfxClientRpc(1.5f);
                break;
        }

        if (currentHealth.Value <= 0)
            Die();
    }

    private void Die()
    {
        isDead = true;
        StopAllCoroutines();

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

        GetComponent<NetworkObject>().Despawn();
    }

    private IEnumerator FrostRoutine(float duration, float multiplier)
    {
        if (agent != null)
            agent.speed = originalSpeed * multiplier;

        yield return new WaitForSeconds(duration);

        if (!isDead && agent != null)
            agent.speed = originalSpeed;
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

    private void HideVisuals()
    {
        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = false;

        if (healthCanvas != null)
            healthCanvas.SetActive(false);
    }

    [ClientRpc]
    private void DeathFeedbackClientRpc()
    {
        if (activeBurnVfx)
        {
            Destroy(activeBurnVfx);
            activeBurnVfx = null;
        }

        if (activeFrostVfx)
        {
            Destroy(activeFrostVfx);
            activeFrostVfx = null;
        }

        if (activeLightningVfx)
        {
            Destroy(activeLightningVfx);
            activeLightningVfx = null;
        }

        HideVisuals();

        if (deathVfx != null)
        {
            Instantiate(deathVfx, transform.position, Quaternion.identity);
        }
    }


    
}