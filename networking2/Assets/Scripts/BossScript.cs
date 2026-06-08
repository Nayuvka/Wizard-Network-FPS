using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections;

public class NetworkBoss : NetworkBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private float maxHealth = 1000f;
    [SerializeField] private float knockbackIntensity = 8f;
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

    [Header("SFX")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private float shootVolume = 0.7f;

    [Header("VFX")]
    [SerializeField] private GameObject deathVfx;

    [Header("Movement")]
    private NavMeshAgent agent;
    private Transform target;
    private bool isBeingKnockedBack = false;
    private bool isDead = false;
    private float originalSpeed;

    [Header("Animations")]
    [SerializeField] private Animator bossAnimator;
    [SerializeField] private float deathDelay = 3.0f;

    private Coroutine frostRoutine;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent != null) originalSpeed = agent.speed;
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

        if (!IsServer || isBeingKnockedBack || isDead) return;

        if (target == null)
        {
            FindTarget();
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.SetDestination(target.position);
        }

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
        fireball.GetComponent<NetworkObject>().Spawn();

        if (fireball.TryGetComponent(out BossProjectile proj))
        {
            proj.Initialize(targetDir);
        }

        PlayShootSoundClientRpc(activePoint.position);
    }

    [ClientRpc]
    private void PlayShootSoundClientRpc(Vector3 position)
    {
        if (shootSound != null)
        {
            AudioSource.PlayClipAtPoint(shootSound, position, shootVolume);
        }
    }

    public void TakeDamage(float amount, Vector3 knockbackSource = default, ulong attackerId = ulong.MaxValue, DamageType damageType = DamageType.Normal)
    {
        if (!IsServer || isDead) return;

        currentHealth.Value -= amount;

        if (damageType == DamageType.Frost)
        {
            if (frostRoutine != null) StopCoroutine(frostRoutine);
            frostRoutine = StartCoroutine(FrostRoutine(3f, 0.2f));
        }

        if (knockbackSource != default)
        {
            ApplyKnockback(knockbackSource);
        }

        if (currentHealth.Value <= 0)
        {
            if (attackerId != ulong.MaxValue && NetworkManager.Singleton.ConnectedClients.TryGetValue(attackerId, out NetworkClient client))
            {
                if (client.PlayerObject != null && client.PlayerObject.TryGetComponent(out PlayerCombatStats stats))
                {
                    stats.AddKill();
                    stats.AddScore(500);
                }
            }

            StartCoroutine(HandleDeath());
        }
    }

    private IEnumerator FrostRoutine(float duration, float multiplier)
    {
        if (agent != null) agent.speed = originalSpeed * multiplier;
        yield return new WaitForSeconds(duration);
        if (!isDead && agent != null) agent.speed = originalSpeed;
    }

    private void ApplyKnockback(Vector3 source)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            isBeingKnockedBack = true;
            Vector3 direction = (transform.position - source).normalized;
            direction.y = 0;

            agent.isStopped = true;
            agent.velocity = direction * knockbackIntensity;

            StartCoroutine(BossKnockbackRecovery());
        }
    }

    private IEnumerator BossKnockbackRecovery()
    {
        yield return new WaitForSeconds(0.15f);
        if (agent != null && agent.isOnNavMesh)
        {
            agent.velocity = Vector3.zero;
            agent.isStopped = false;
        }
        isBeingKnockedBack = false;
    }

    private IEnumerator HandleDeath()
    {
        isDead = true;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        DeathVfxClientRpc(transform.position);
        TriggerDeathAnimationClientRpc();

        yield return new WaitForSeconds(deathDelay);

        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.EnemyDeath(NetworkObject);
        }

        NetworkObject.Despawn();
    }

    [ClientRpc]
    private void TriggerDeathAnimationClientRpc()
    {
        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger("Death");
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

    [ClientRpc]
    private void DeathVfxClientRpc(Vector3 position)
    {
        if (deathVfx != null)
        {
            Instantiate(deathVfx, position, Quaternion.identity);
        }
    }
}