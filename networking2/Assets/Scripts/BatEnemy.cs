using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class BatEnemy : NetworkBehaviour, IDamageable
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float maxHealth = 20f;
    
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(20f);

    [Header("Flying Logic")]
    [SerializeField] private float flightHeight = 5.0f;
    [SerializeField] private float hoverAmplitude = 0.5f;
    [SerializeField] private float hoverFrequency = 2.0f;

    [Header("VFX")]
    [SerializeField] private GameObject deathVfx;
    [SerializeField] private GameObject burnVfxPrefab;
    [SerializeField] private GameObject frostVfxPrefab;
    [SerializeField] private GameObject lightningVfxPrefab;
    [SerializeField] private Transform statusVfxPoint;

    private GameObject activeBurnVfx;
    private GameObject activeFrostVfx;
    private GameObject activeLightningVfx;
    
    private Transform target;
    private Rigidbody rb;
    private bool isDead = false;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;

        if (IsServer) 
        {
            currentHealth.Value = maxHealth;
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            if (players.Length > 0)
                target = players[Random.Range(0, players.Length)].transform;
        }
    }

    void FixedUpdate()
    {
        if (IsServer && !isDead && target != null)
        {
            Vector3 targetPos = target.position + (Vector3.up * flightHeight);
            targetPos.y += Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;

            Vector3 newPos = Vector3.MoveTowards(transform.position, targetPos, speed * Time.fixedDeltaTime);
            rb.MovePosition(newPos);

            Vector3 lookPos = new Vector3(target.position.x, transform.position.y, target.position.z);
            transform.LookAt(lookPos);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsServer && !isDead && other.CompareTag("Player"))
        {
            if (other.TryGetComponent(out PlayerHealth pHealth))
            {
                pHealth.TakeDamage(damage);
                Die();
            }
        }
    }

    public void TakeDamage(float amount, Vector3 sourcePosition = default, ulong attackerId = ulong.MaxValue, DamageType damageType = DamageType.Normal)
    {
        if (!IsServer || isDead) return;

        currentHealth.Value -= amount;

        if (currentHealth.Value <= 0) 
        {
            Die();
        }
        else
        {
            switch (damageType)
            {
                case DamageType.Fire: PlayBurnVfxClientRpc(5f); break;
                case DamageType.Frost: PlayFrostVfxClientRpc(3f); break;
                case DamageType.Lightning: PlayLightningVfxClientRpc(1.5f); break;
            }
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        DeathFeedbackClientRpc(transform.position);
        StartCoroutine(DespawnDelay());
    }

    private IEnumerator DespawnDelay()
    {
        yield return new WaitForSeconds(0.2f);
        if (IsServer && NetworkObject.IsSpawned) GetComponent<NetworkObject>().Despawn();
    }

    [ClientRpc] private void PlayBurnVfxClientRpc(float duration) => SpawnStatusVfx(burnVfxPrefab, ref activeBurnVfx, duration);
    [ClientRpc] private void PlayFrostVfxClientRpc(float duration) => SpawnStatusVfx(frostVfxPrefab, ref activeFrostVfx, duration);
    [ClientRpc] private void PlayLightningVfxClientRpc(float duration) => SpawnStatusVfx(lightningVfxPrefab, ref activeLightningVfx, duration);

    private void SpawnStatusVfx(GameObject prefab, ref GameObject activeVfx, float duration)
    {
        if (isDead || prefab == null) return;
        if (activeVfx != null) Destroy(activeVfx);
        Transform attach = statusVfxPoint != null ? statusVfxPoint : transform;
        activeVfx = Instantiate(prefab, attach.position, attach.rotation, attach);
        Destroy(activeVfx, duration);
    }

    [ClientRpc]
    private void DeathFeedbackClientRpc(Vector3 pos)
    {
        if (activeBurnVfx) Destroy(activeBurnVfx);
        if (activeFrostVfx) Destroy(activeFrostVfx);
        if (activeLightningVfx) Destroy(activeLightningVfx);
        if (deathVfx != null) Instantiate(deathVfx, pos, Quaternion.identity);
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
    }
}