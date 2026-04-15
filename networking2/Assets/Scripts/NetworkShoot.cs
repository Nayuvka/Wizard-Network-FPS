using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class NetworkShoot : NetworkBehaviour
{
    [Header("References")]
    [Space(5)]

    private NetworkPlayerController playerController;

    [Header("Shoot Settings")]
    [Space(5)]
    [SerializeField] private ParticleSystem wandSmoke;
    [SerializeField] private Animator wandAnimator;
    [SerializeField] private float shootCooldown = 0.5f;
    [SerializeField] private float wandRange = 100f;
    [SerializeField] private Transform wandFirePoint;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private ProjectileData[] projectiles;
    private int currentProjectileIndex = 0;
    private ProjectileData CurrentProjectile => projectiles[currentProjectileIndex];
    private bool canShoot = true;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        playerController = GetComponent<NetworkPlayerController>();
    }

    public void ProcessLocalShoot()
    {
        if (!canShoot) return;


        wandSmoke.Play();
        wandAnimator.SetTrigger("Shoot");
        StartCoroutine(ShootTimer());

        Vector3 cameraOrigin = playerController.playerCamera.transform.position;
        Vector3 cameraForward = playerController.playerCamera.transform.forward;
        Vector3 targetPoint = cameraOrigin + (cameraForward * wandRange);

        if (Physics.Raycast(cameraOrigin, cameraForward, out RaycastHit hit, wandRange, enemyLayer))
        {
            targetPoint = hit.point;
        }

        ShootServerRpc(wandFirePoint.position, targetPoint, cameraOrigin, cameraForward, currentProjectileIndex);
    }

    [ServerRpc]
    void ShootServerRpc(Vector3 spawnPos, Vector3 targetPoint, Vector3 cameraOrigin, Vector3 cameraForward, int projectileIndex)
    {
        if (projectileIndex < 0 || projectileIndex >= projectiles.Length)
            projectileIndex = 0;

        ProjectileData selectedProjectile = projectiles[projectileIndex];

        //Hitscan method
        /*if (Physics.Raycast(cameraOrigin, cameraForward, out RaycastHit hit, wandRange, enemyLayer))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                EnemyHealth health = hit.collider.GetComponent<EnemyHealth>();

                if(health != null)
                {
                    Vector3 hitDir = cameraForward;
                    health.TakeDamage(selectedProjectile.damage, hitDir);
                }
                
            }
        }*/

        Vector3 moveDir = (targetPoint - spawnPos).normalized;

        GameObject bullet = Instantiate(
            selectedProjectile.projectilePrefab,
            spawnPos,
            Quaternion.LookRotation(moveDir)
        );


        NetworkProjectile projectile = bullet.GetComponent<NetworkProjectile>();
        if (projectile != null)
        {
            projectile.projectileData = selectedProjectile;
        }

        NetworkObject bulletNetObj = bullet.GetComponent<NetworkObject>();
        if (bulletNetObj != null)
            bulletNetObj.Spawn();


        if (projectile != null)
            projectile.Initialize(moveDir, projectileIndex);

        ShootObserversClientRpc();
    }

    public void CycleProjectileForward()
    {
        currentProjectileIndex++;

        if (currentProjectileIndex >= projectiles.Length)
            currentProjectileIndex = 0;

        Debug.Log("Switched to: " + CurrentProjectile.name);
    }

    public void CycleProjectileBackward()
    {
        currentProjectileIndex--;

        if (currentProjectileIndex < 0)
            currentProjectileIndex = projectiles.Length - 1;

        Debug.Log("Switched to: " + CurrentProjectile.name);
    }

    [ClientRpc]
    void ShootObserversClientRpc()
    {
        if (IsOwner) return;

        wandSmoke.Play();
        wandAnimator.SetTrigger("Shoot");
    }

    IEnumerator ShootTimer()
    {
        canShoot = false;
        yield return new WaitForSeconds(shootCooldown);
        canShoot = true;
    }

    
}