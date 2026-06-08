using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ExplosiveBarrel : NetworkBehaviour, IDamageable
{
    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float explosionDamage = 50f;

    [Header("Respawn")]
    [SerializeField] private float respawnTime = 5f;
    [SerializeField] private float randomRespawnOffset = 2f;

    [Header("VFX")]
    [SerializeField] private GameObject explosionVFX;

    [Header("Explosion Audio")]
    [SerializeField] private AudioClip[] explosionSFX;

    [Header("Light Flash")]
    [SerializeField] private float lightIntensity = 8f;
    [SerializeField] private float lightRange = 12f;
    [SerializeField] private float lightDuration = 0.08f;
    [SerializeField] private Color lightColor = new Color(1f, 0.8f, 0.5f);

    private bool exploded;
    private ulong lastAttackerId = ulong.MaxValue;

    private Collider[] barrelColliders;
    private Renderer[] barrelRenderers;

    private void Awake()
    {
        barrelColliders = GetComponentsInChildren<Collider>();
        barrelRenderers = GetComponentsInChildren<Renderer>();
    }

    public void TakeDamage(float amount, Vector3 sourcePosition = default, ulong attackerId = ulong.MaxValue, DamageType damageType = DamageType.Normal)
    {
        Explode(attackerId);
    }

    public void Explode(ulong attackerId = ulong.MaxValue)
    {
        if (!IsServer || exploded)
            return;

        if (attackerId != ulong.MaxValue)
        {
            lastAttackerId = attackerId;
        }

        exploded = true;

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in hits)
        {
            float distance = Vector3.Distance(transform.position, hit.transform.position);
            float damageMultiplier = 1f - Mathf.Clamp01(distance / explosionRadius);
            float damage = explosionDamage * damageMultiplier;

            if (damage < 1f)
                continue;

            if (hit.TryGetComponent(out ExplosiveBarrel barrel))
            {
                if (barrel != this)
                {
                    barrel.Explode(lastAttackerId);
                }
                continue;
            }

            if (hit.TryGetComponent(out NetworkEnemy enemy))
            {
                enemy.TakeDamage(damage, transform.position, lastAttackerId);
                continue;
            }

            if (hit.TryGetComponent(out WizardBoss wizardBoss))
            {
                wizardBoss.TakeDamage(damage, transform.position, lastAttackerId);
                continue;
            }

            if (hit.TryGetComponent(out NetworkBoss networkBoss))
            {
                networkBoss.TakeDamage(damage, transform.position, lastAttackerId);
                continue;
            }

            if (hit.TryGetComponent(out PlayerHealth playerHealth))
            {
                playerHealth.TakeDamage(damage);
                continue;
            }

            if (hit.TryGetComponent(out PracticeTarget target))
            {
                target.ExplodeHit();
                continue;
            }

            if (hit.TryGetComponent(out IDamageable damageable) && hit.gameObject != gameObject)
            {
                damageable.TakeDamage(damage, transform.position, lastAttackerId);
            }
        }

        PlayExplosionClientRpc();
        SetBarrelVisibleClientRpc(false);
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(
            respawnTime + Random.Range(0f, randomRespawnOffset)
        );

        exploded = false;
        SetBarrelVisibleClientRpc(true);
    }

    [ClientRpc]
    private void PlayExplosionClientRpc()
    {
        if (explosionVFX != null)
        {
            Instantiate(explosionVFX, transform.position, Quaternion.identity);
        }

        if (explosionSFX != null && explosionSFX.Length > 0)
        {
            AudioClip randomClip =
                explosionSFX[Random.Range(0, explosionSFX.Length)];

            AudioSource.PlayClipAtPoint(randomClip, transform.position);
        }

        StartCoroutine(LightFlashRoutine());
    }

    private IEnumerator LightFlashRoutine()
    {
        GameObject lightObj = new GameObject("Explosion Flash");
        lightObj.transform.position = transform.position + Vector3.up;

        Light flashLight = lightObj.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.intensity = lightIntensity;
        flashLight.range = lightRange;
        flashLight.color = lightColor;

        yield return new WaitForSeconds(lightDuration);

        Destroy(lightObj);
    }

    [ClientRpc]
    private void SetBarrelVisibleClientRpc(bool visible)
    {
        foreach (Renderer renderer in barrelRenderers)
            renderer.enabled = visible;

        foreach (Collider collider in barrelColliders)
            collider.enabled = visible;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}