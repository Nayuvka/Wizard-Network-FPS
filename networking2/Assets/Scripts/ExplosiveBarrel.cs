using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ExplosiveBarrel : NetworkBehaviour
{
    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float explosionDamage = 50f;

    [Header("Respawn")]
    [SerializeField] private float respawnTime = 5f;

    [Header("Effects")]
    [SerializeField] private GameObject explosionVFX;
    [SerializeField] private AudioClip explosionSFX;

    private bool exploded;

    private Collider[] barrelColliders;
    private Renderer[] barrelRenderers;

    private void Awake()
    {
        barrelColliders = GetComponentsInChildren<Collider>();
        barrelRenderers = GetComponentsInChildren<Renderer>();
    }

    public void Explode()
    {
        if (!IsServer || exploded)
            return;

        exploded = true;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            explosionRadius
        );

        foreach (Collider hit in hits)
        {
            if (hit.TryGetComponent(out ExplosiveBarrel barrel))
            {
                if (barrel != this)
                {
                    barrel.Explode();
                }
            }
        }

        PlayExplosionClientRpc();
        SetBarrelVisibleClientRpc(false);

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);

        exploded = false;

        SetBarrelVisibleClientRpc(true);
    }

    [ClientRpc]
    private void PlayExplosionClientRpc()
    {
        if (explosionVFX != null)
        {
            Instantiate(
                explosionVFX,
                transform.position,
                Quaternion.identity
            );
        }

        if (explosionSFX != null)
        {
            AudioSource.PlayClipAtPoint(
                explosionSFX,
                transform.position
            );
        }
    }

    [ClientRpc]
    private void SetBarrelVisibleClientRpc(bool visible)
    {
        foreach (Renderer renderer in barrelRenderers)
        {
            renderer.enabled = visible;
        }

        foreach (Collider collider in barrelColliders)
        {
            collider.enabled = visible;
        }
    }
}